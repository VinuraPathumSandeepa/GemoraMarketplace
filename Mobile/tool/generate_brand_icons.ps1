# Run from any directory on Windows: powershell -File tool/generate_brand_icons.ps1
# Keep the drawing coordinates in sync with lib/widgets/gemora_mark.dart.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$mobileRoot = Split-Path -Parent $PSScriptRoot
$green = [System.Drawing.ColorTranslator]::FromHtml('#08251E')
$gold = [System.Drawing.ColorTranslator]::FromHtml('#E4BC74')

function New-IconBitmap {
    param([int]$Size, [bool]$Transparent = $false, [single]$MarkScale = 1)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.Clear($(if ($Transparent) { [System.Drawing.Color]::Transparent } else { $green }))
        $graphics.ScaleTransform([single]($Size / 100), [single]($Size / 100))
        $graphics.TranslateTransform([single](50 * (1 - $MarkScale)), [single](50 * (1 - $MarkScale)))
        $graphics.ScaleTransform($MarkScale, $MarkScale)

        $points = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(27, 24),
            [System.Drawing.PointF]::new(73, 24),
            [System.Drawing.PointF]::new(85, 40),
            [System.Drawing.PointF]::new(50, 82),
            [System.Drawing.PointF]::new(15, 40)
        )
        $gemFill = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(32, $gold))
        $outline = [System.Drawing.Pen]::new($gold, 3.5)
        $facets = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(195, $gold), 2)
        $shine = [System.Drawing.Pen]::new($gold, 2.4)
        try {
            $outline.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
            $facets.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
            $shine.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
            $shine.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
            $graphics.FillPolygon($gemFill, $points)
            $graphics.DrawPolygon($outline, $points)
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(15, 40), [System.Drawing.PointF]::new(85, 40))
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(27, 24), [System.Drawing.PointF]::new(37, 40))
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(37, 40), [System.Drawing.PointF]::new(50, 82))
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(73, 24), [System.Drawing.PointF]::new(63, 40))
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(63, 40), [System.Drawing.PointF]::new(50, 82))
            $graphics.DrawLine($facets, [System.Drawing.PointF]::new(37, 40), [System.Drawing.PointF]::new(63, 40))
            $graphics.DrawLine($shine, [System.Drawing.PointF]::new(79, 12), [System.Drawing.PointF]::new(79, 21))
            $graphics.DrawLine($shine, [System.Drawing.PointF]::new(74.5, 16.5), [System.Drawing.PointF]::new(83.5, 16.5))
        } finally {
            $gemFill.Dispose()
            $outline.Dispose()
            $facets.Dispose()
            $shine.Dispose()
        }
    } finally {
        $graphics.Dispose()
    }
    return $bitmap
}

function Save-IconPng {
    param([string]$RelativePath, [int]$Size, [bool]$Transparent = $false, [single]$MarkScale = 1)
    $path = Join-Path $mobileRoot $RelativePath
    $directory = Split-Path -Parent $path
    if (!(Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    $bitmap = New-IconBitmap -Size $Size -Transparent $Transparent -MarkScale $MarkScale
    try { $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $bitmap.Dispose() }
}

Save-IconPng 'assets/brand/gemora_launcher_1024.png' 1024

$androidSizes = @{
    'mipmap-mdpi' = 48
    'mipmap-hdpi' = 72
    'mipmap-xhdpi' = 96
    'mipmap-xxhdpi' = 144
    'mipmap-xxxhdpi' = 192
}
foreach ($entry in $androidSizes.GetEnumerator()) {
    Save-IconPng "android/app/src/main/res/$($entry.Key)/ic_launcher.png" $entry.Value
}
Save-IconPng 'android/app/src/main/res/drawable-nodpi/ic_launcher_foreground.png' 432 $true 0.8

foreach ($platform in @('ios/Runner/Assets.xcassets/AppIcon.appiconset', 'macos/Runner/Assets.xcassets/AppIcon.appiconset')) {
    $manifest = Get-Content -LiteralPath (Join-Path $mobileRoot "$platform/Contents.json") -Raw | ConvertFrom-Json
    foreach ($item in $manifest.images) {
        if (!$item.filename) { continue }
        $baseSize = [double](($item.size -split 'x')[0])
        $scale = [double]($item.scale.TrimEnd('x'))
        Save-IconPng "$platform/$($item.filename)" ([int]($baseSize * $scale))
    }
}

Save-IconPng 'web/favicon.png' 32
Save-IconPng 'web/icons/Icon-192.png' 192
Save-IconPng 'web/icons/Icon-512.png' 512
Save-IconPng 'web/icons/Icon-maskable-192.png' 192 $false 0.8
Save-IconPng 'web/icons/Icon-maskable-512.png' 512 $false 0.8
Save-IconPng 'ios/Runner/Assets.xcassets/LaunchImage.imageset/LaunchImage.png' 160 $true
Save-IconPng 'ios/Runner/Assets.xcassets/LaunchImage.imageset/LaunchImage@2x.png' 320 $true
Save-IconPng 'ios/Runner/Assets.xcassets/LaunchImage.imageset/LaunchImage@3x.png' 480 $true

# Windows ICO: icon directory entries followed by PNG payloads.
$icoPath = Join-Path $mobileRoot 'windows/runner/resources/app_icon.ico'
$sizes = @(16, 32, 48, 256)
$payloads = @()
foreach ($size in $sizes) {
    $bitmap = New-IconBitmap -Size $size
    $stream = [System.IO.MemoryStream]::new()
    try { $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png); $payloads += ,$stream.ToArray() }
    finally { $stream.Dispose(); $bitmap.Dispose() }
}
$file = [System.IO.File]::Create($icoPath)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($index = 0; $index -lt $sizes.Count; $index++) {
        $dimension = $(if ($sizes[$index] -eq 256) { 0 } else { $sizes[$index] })
        $writer.Write([byte]$dimension)
        $writer.Write([byte]$dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$payloads[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $payloads[$index].Length
    }
    foreach ($payload in $payloads) { $writer.Write([byte[]]$payload) }
} finally { $writer.Dispose(); $file.Dispose() }

Write-Output 'Gemora launcher icons generated.'
