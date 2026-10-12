"""Move an MP4's moov atom before mdat so streaming can begin promptly.

This script preserves the encoded media and updates its chunk offsets. It is
intentionally small and strict: unsupported layouts fail without changing the
source file.
"""

from pathlib import Path
import os
import shutil
import struct
import sys


CONTAINERS = {b"moov", b"trak", b"mdia", b"minf", b"stbl", b"edts", b"dinf"}


def boxes(data, start=0, end=None):
    end = len(data) if end is None else end
    position = start
    while position < end:
        if end - position < 8:
            raise ValueError("Incomplete MP4 box header")
        size = struct.unpack_from(">I", data, position)[0]
        kind = bytes(data[position + 4 : position + 8])
        header = 8
        if size == 1:
            if end - position < 16:
                raise ValueError("Incomplete extended MP4 box header")
            size = struct.unpack_from(">Q", data, position + 8)[0]
            header = 16
        elif size == 0:
            size = end - position
        if size < header or position + size > end:
            raise ValueError(f"Invalid {kind!r} box size")
        yield position, size, header, kind
        position += size


def shift_chunk_offsets(moov, delta, old_mdat):
    adjusted = 0

    def visit(start, end):
        nonlocal adjusted
        for position, size, header, kind in boxes(moov, start, end):
            content = position + header
            box_end = position + size
            if kind in CONTAINERS:
                visit(content, box_end)
            elif kind in (b"stco", b"co64"):
                width = 4 if kind == b"stco" else 8
                if box_end - content < 8:
                    raise ValueError("Incomplete chunk offset table")
                count = struct.unpack_from(">I", moov, content + 4)[0]
                entries = content + 8
                if entries + count * width != box_end:
                    raise ValueError("Invalid chunk offset table length")
                for index in range(count):
                    offset_position = entries + index * width
                    offset = int.from_bytes(
                        moov[offset_position : offset_position + width], "big"
                    )
                    if not old_mdat[0] <= offset < old_mdat[1]:
                        raise ValueError("Chunk offset points outside mdat")
                    updated = offset + delta
                    if updated >= 1 << (width * 8):
                        raise ValueError("Chunk offset exceeds table width")
                    moov[offset_position : offset_position + width] = updated.to_bytes(
                        width, "big"
                    )
                    adjusted += 1

    visit(0, len(moov))
    if not adjusted:
        raise ValueError("No chunk offsets found")
    return adjusted


def main(path):
    source = Path(path)
    data = source.read_bytes()
    top_level = list(boxes(data))
    moovs = [box for box in top_level if box[3] == b"moov"]
    mdats = [box for box in top_level if box[3] == b"mdat"]
    if len(moovs) != 1 or len(mdats) != 1:
        raise ValueError("Expected one moov and one mdat box")
    moov_at, moov_size, _, _ = moovs[0]
    mdat_at, mdat_size, mdat_header, _ = mdats[0]
    if moov_at < mdat_at:
        print(f"Already streamable: {source}")
        return
    if moov_at + moov_size != len(data):
        raise ValueError("Only an MP4 with a final moov box is supported")

    moov = bytearray(data[moov_at:])
    count = shift_chunk_offsets(
        moov,
        moov_size,
        (mdat_at + mdat_header, mdat_at + mdat_size),
    )
    converted = data[:mdat_at] + moov + data[mdat_at:moov_at]
    temporary = source.with_name(source.name + ".faststart.tmp")
    try:
        temporary.write_bytes(converted)
        try:
            os.replace(temporary, source)
        except PermissionError:
            # Some Windows workspaces permit writing a file but deny replacing
            # its directory entry while another process has the file open.
            with temporary.open("rb") as ready, source.open("r+b") as target:
                shutil.copyfileobj(ready, target)
                target.truncate()
                target.flush()
                os.fsync(target.fileno())
    finally:
        temporary.unlink(missing_ok=True)
    print(f"Moved moov before mdat and updated {count} chunk offsets: {source}")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        raise SystemExit("Usage: python faststart_mp4.py path/to/video.mp4")
    main(sys.argv[1])
