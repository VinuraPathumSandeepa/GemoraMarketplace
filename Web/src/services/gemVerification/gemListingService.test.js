import { afterEach, expect, it, vi } from "vitest";
import api from "../api";
import gemListingService from "./gemListingService";

const originalAdapter = api.defaults.adapter;

afterEach(() => {
  api.defaults.adapter = originalAdapter;
  sessionStorage.clear();
  localStorage.clear();
  vi.clearAllMocks();
});

it("submits through the authenticated API client and exact backend route", async () => {
  sessionStorage.setItem("gemora_token", "seller-jwt");
  const submitted = { id: 69, status: "PendingVerification" };
  const adapter = vi.fn(async (config) => ({
    data: submitted,
    status: 200,
    statusText: "OK",
    headers: {},
    config,
  }));
  api.defaults.adapter = adapter;

  expect(await gemListingService.submitListing(69)).toEqual(submitted);
  expect(adapter).toHaveBeenCalledTimes(1);
  const request = adapter.mock.calls[0][0];
  expect(request.method).toBe("post");
  expect(request.url).toBe("/GemListings/69/submit-verification");
  expect(request.headers.Authorization).toBe("Bearer seller-jwt");
  expect(request.data).toBeUndefined();
});
