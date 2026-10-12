import { afterEach, expect, it, vi } from "vitest";

afterEach(() => {
  vi.unstubAllEnvs();
  vi.resetModules();
});

it("resolves legacy relative image URLs against the configured API origin", async () => {
  vi.stubEnv("VITE_API_URL", "https://gemora-api.onrender.com/api/");
  const { API_ORIGIN, resolveApiAssetUrl } = await import("./api.js");

  expect(API_ORIGIN).toBe("https://gemora-api.onrender.com");
  expect(resolveApiAssetUrl("/uploads/gem-images/gem.jpg"))
    .toBe("https://gemora-api.onrender.com/uploads/gem-images/gem.jpg");
  expect(resolveApiAssetUrl("uploads/gem-images/gem.jpg"))
    .toBe("https://gemora-api.onrender.com/uploads/gem-images/gem.jpg");
});

it("preserves absolute Supabase URLs and returns null for missing images", async () => {
  vi.stubEnv("VITE_API_URL", "https://gemora-api.onrender.com/api");
  const { resolveApiAssetUrl } = await import("./api.js");
  const supabaseUrl = "https://example.supabase.co/storage/v1/object/public/gem-images/gem.jpg";

  expect(resolveApiAssetUrl(supabaseUrl)).toBe(supabaseUrl);
  expect(resolveApiAssetUrl("http://localhost:5198/uploads/gem-images/gem.jpg"))
    .toBe("http://localhost:5198/uploads/gem-images/gem.jpg");
  expect(resolveApiAssetUrl(null)).toBeNull();
  expect(resolveApiAssetUrl(undefined)).toBeNull();
  expect(resolveApiAssetUrl("  ")).toBeNull();
});
