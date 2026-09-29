import axios from "axios";

const API_BASE_URL =
  import.meta.env.VITE_API_URL ||
  "http://localhost:5198/api";

export const API_ORIGIN =
  API_BASE_URL.replace(
    /\/api\/?$/i,
    ""
  );

export function resolveApiAssetUrl(
  value
) {
  if (!value) {
    return null;
  }

  if (
    value.startsWith("http://") ||
    value.startsWith("https://")
  ) {
    return value;
  }

  return `${API_ORIGIN}${
    value.startsWith("/")
      ? value
      : `/${value}`
  }`;
}

const api = axios.create({
  baseURL: API_BASE_URL,
});

/*
 * Do not permanently force:
 *
 * Content-Type: application/json
 *
 * Axios will automatically use:
 *
 * application/json
 * for normal objects
 *
 * multipart/form-data
 * for FormData uploads.
 */

api.interceptors.request.use(
  (config) => {
    const token =
      localStorage.getItem(
        "gemora_token"
      );

    if (token) {
      config.headers.Authorization =
        `Bearer ${token}`;
    }

    return config;
  },

  (error) =>
    Promise.reject(error)
);

export default api;