const TOKEN_KEY = "gemora_token";

function normalizeToken(value) {
  return (value || "").replace(/^Bearer\s+/i, "").replace(/^"(.*)"$/, "$1").trim();
}

// Pin the session to this tab. Signing into a different role in another tab
// must not change the credentials used by the already-open buyer/seller UI.
export function getAuthToken() {
  const tabToken = sessionStorage.getItem(TOKEN_KEY);
  if (tabToken !== null) return normalizeToken(tabToken);
  const token = normalizeToken(localStorage.getItem(TOKEN_KEY));
  if (token) sessionStorage.setItem(TOKEN_KEY, token);
  return token;
}

export function setAuthToken(token) {
  const normalized = normalizeToken(token);
  sessionStorage.setItem(TOKEN_KEY, normalized);
  localStorage.setItem(TOKEN_KEY, normalized);
}

export function clearAuthToken() {
  const token = getAuthToken();
  if (normalizeToken(localStorage.getItem(TOKEN_KEY)) === token)
    localStorage.removeItem(TOKEN_KEY);
  // An empty value represents an explicit sign-out and prevents silently
  // adopting another tab's account on the next request or page reload.
  sessionStorage.setItem(TOKEN_KEY, "");
}
