import {
  describe,
  expect,
  it,
} from "vitest";

import {
  API_ORIGIN,
  resolveApiAssetUrl,
} from "./api";

describe(
  "Component 1 - Asset URL Regression",
  () => {
    it(
      "C1_BUG_REG_01_AbsoluteHttpsAssetUrl_RemainsUnchanged",
      () => {
        const url =
          "https://example.supabase.co/storage/v1/object/public/gem-images/sapphire.png";

        expect(
          resolveApiAssetUrl(url)
        ).toBe(url);
      }
    );

    it(
      "C1_BUG_REG_02_RelativeAssetPath_UsesConfiguredApiOrigin",
      () => {
        const path =
          "/uploads/gem-images/sapphire.png";

        expect(
          resolveApiAssetUrl(path)
        ).toBe(
          `${API_ORIGIN}${path}`
        );
      }
    );

    it(
      "C1_BUG_REG_03_RelativeAssetPathWithoutLeadingSlash_IsResolvedSafely",
      () => {
        const path =
          "uploads/gem-images/sapphire.png";

        expect(
          resolveApiAssetUrl(path)
        ).toBe(
          `${API_ORIGIN}/${path}`
        );
      }
    );

    it(
      "C1_BUG_REG_04_MissingAssetReference_ReturnsNull",
      () => {
        expect(
          resolveApiAssetUrl(null)
        ).toBeNull();

        expect(
          resolveApiAssetUrl("")
        ).toBeNull();
      }
    );
  }
);