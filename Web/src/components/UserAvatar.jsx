import {
  useEffect,
  useState,
} from "react";

import {
  resolveApiAssetUrl,
} from "../services/api";

function getInitials(name) {
  if (!name) {
    return "U";
  }

  const parts =
    name
      .trim()
      .split(/\s+/)
      .filter(Boolean);

  if (parts.length === 0) {
    return "U";
  }

  if (parts.length === 1) {
    return parts[0]
      .slice(0, 2)
      .toUpperCase();
  }

  return (
    parts[0][0] +
    parts[parts.length - 1][0]
  ).toUpperCase();
}

export default function UserAvatar({
  user,
  size = 44,
  className = "",
}) {
  const [imageFailed, setImageFailed] =
    useState(false);

  const imageUrl =
    resolveApiAssetUrl(
      user?.profileImageUrl
    );

  useEffect(() => {
    setImageFailed(false);
  }, [imageUrl]);

  const initials =
    getInitials(
      user?.fullName
    );

  const style = {
    width: `${size}px`,
    height: `${size}px`,
    minWidth: `${size}px`,
  };

  if (
    imageUrl &&
    !imageFailed
  ) {
    return (
      <img
        src={imageUrl}
        alt={
          user?.fullName
            ? `${user.fullName} profile`
            : "User profile"
        }
        className={`gemora-user-avatar gemora-user-avatar-image ${className}`}
        style={style}
        onError={() =>
          setImageFailed(true)
        }
      />
    );
  }

  return (
    <div
      className={`gemora-user-avatar gemora-user-avatar-fallback ${className}`}
      style={style}
      aria-label={
        user?.fullName ||
        "User"
      }
    >
      {initials}
    </div>
  );
}