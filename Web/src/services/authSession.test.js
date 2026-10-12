// @vitest-environment node
import { assert, beforeEach, test } from "vitest";
import { clearAuthToken, getAuthToken, setAuthToken } from "./authSession.js";

const storage = () => {
  const values = new Map();
  return {
    getItem: key => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, String(value)),
    removeItem: key => values.delete(key),
  };
};
beforeEach(() => {
  globalThis.localStorage = storage();
  globalThis.sessionStorage = storage();
});

test("buyer and seller tabs keep their own credentials after another login", () => {
  const buyerTab = sessionStorage;
  setAuthToken("buyer-token");
  const sellerTab = storage();
  globalThis.sessionStorage = sellerTab;
  setAuthToken("seller-token");
  assert.equal(getAuthToken(), "seller-token");
  globalThis.sessionStorage = buyerTab;
  assert.equal(getAuthToken(), "buyer-token");
  globalThis.sessionStorage = sellerTab;
  assert.equal(getAuthToken(), "seller-token");
});

test("signing out one tab does not log out or adopt another tab's account", () => {
  const buyerTab = sessionStorage;
  setAuthToken("buyer-token");
  const sellerTab = storage();
  globalThis.sessionStorage = sellerTab;
  setAuthToken("seller-token");
  globalThis.sessionStorage = buyerTab;
  clearAuthToken();
  assert.equal(getAuthToken(), "");
  assert.equal(localStorage.getItem("gemora_token"), "seller-token");
  globalThis.sessionStorage = sellerTab;
  assert.equal(getAuthToken(), "seller-token");
});

test("an existing saved login is normalized and pinned before shared storage changes", () => {
  localStorage.setItem("gemora_token", 'Bearer "existing-token"');
  assert.equal(getAuthToken(), "existing-token");
  localStorage.setItem("gemora_token", "another-account-token");
  assert.equal(getAuthToken(), "existing-token");
});

test("explicit logout remains signed out across reload and a later login works", () => {
  setAuthToken("old-token");
  clearAuthToken();
  localStorage.setItem("gemora_token", "other-tab-token");
  assert.equal(getAuthToken(), "");
  setAuthToken("new-token");
  assert.equal(getAuthToken(), "new-token");
});
