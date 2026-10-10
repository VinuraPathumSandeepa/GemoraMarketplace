import "@testing-library/jest-dom/vitest";

import {
  afterEach,
  beforeEach,
  describe,
  expect,
  it,
  vi,
} from "vitest";

import {
  cleanup,
  render,
  screen,
  waitFor,
} from "@testing-library/react";

import userEvent from "@testing-library/user-event";

import {
  MemoryRouter,
} from "react-router-dom";

import VerificationQueue from "./VerificationQueue";

import api from "../../services/api";

vi.mock(
  "../../layouts/DashboardLayout",
  () => ({
    default: ({ children }) => (
      <div data-testid="dashboard-layout">
        {children}
      </div>
    ),
  })
);

vi.mock(
  "../../services/api",
  () => ({
    default: {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
    },

    resolveApiAssetUrl: vi.fn(
      (value) => value || null
    ),
  })
);

function createVerification(
  overrides = {}
) {
  return {
    verificationId: 101,
    gemListingId: 501,

    title: "Royal Blue Sapphire",
    gemType: "Sapphire",

    sellerName: "Test Seller",

    caratWeight: 2.5,
    color: "Royal Blue",
    clarity: "Eye Clean",
    cut: "Oval Mixed Cut",

    price: 250000,
    currency: "LKR",

    primaryImageUrl: null,

    certificateNumber:
      "CERT-C1-101",

    certificateAuthority:
      "Gemora Test Laboratory",

    certificateUrl:
      "/certificates/test.pdf",

    listingStatus:
      "PendingVerification",

    decision:
      "Pending",

    aiStatus:
      "NotStarted",

    createdAt:
      "2026-10-10T09:00:00Z",

    ...overrides,
  };
}

function renderQueue() {
  return render(
    <MemoryRouter>
      <VerificationQueue />
    </MemoryRouter>
  );
}

describe(
  "Component 1 - Verification Queue",
  () => {
    beforeEach(() => {
      vi.clearAllMocks();

      vi.spyOn(
        console,
        "error"
      ).mockImplementation(
        () => {}
      );
    });

    afterEach(() => {
      cleanup();

      vi.restoreAllMocks();
    });

    it(
      "C1_WEB_01_EmptyQueue_ShowsExpectedEmptyState",
      async () => {
        api.get.mockResolvedValue({
          data: [],
        });

        renderQueue();

        expect(
          await screen.findByText(
            "There are currently no pending gemstone submissions requiring review."
          )
        ).toBeInTheDocument();

        expect(
          api.get
        ).toHaveBeenCalledWith(
          "/GemVerifications/pending"
        );
      }
    );

    it(
      "C1_WEB_02_PendingVerification_IsRenderedWithReviewLink",
      async () => {
        api.get.mockResolvedValue({
          data: [
            createVerification(),
          ],
        });

        renderQueue();

        expect(
          await screen.findByText(
            "Royal Blue Sapphire"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByText(
            "Test Seller"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByText(
            "Open Verification"
          )
        ).toBeInTheDocument();

        const reviewLink =
          screen.getByRole(
            "link",
            {
              name: /Open Verification/i,
            }
          );

        expect(
          reviewLink
        ).toHaveAttribute(
          "href",
          "/gemologist/verifications/101"
        );
      }
    );

    it(
      "C1_WEB_03_Search_FiltersPendingVerifications",
      async () => {
        const user =
          userEvent.setup();

        api.get.mockResolvedValue({
          data: [
            createVerification(),

            createVerification({
              verificationId: 102,

              gemListingId: 502,

              title:
                "Natural Green Emerald",

              gemType:
                "Emerald",

              sellerName:
                "Second Seller",

              certificateNumber:
                "CERT-C1-102",
            }),
          ],
        });

        renderQueue();

        expect(
          await screen.findByText(
            "Royal Blue Sapphire"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByText(
            "Natural Green Emerald"
          )
        ).toBeInTheDocument();

        const searchInput =
          screen.getByPlaceholderText(
            "Search gem, seller, certificate..."
          );

        await user.type(
          searchInput,
          "Emerald"
        );

        expect(
          screen.getByText(
            "Natural Green Emerald"
          )
        ).toBeInTheDocument();

        expect(
          screen.queryByText(
            "Royal Blue Sapphire"
          )
        ).not.toBeInTheDocument();
      }
    );

    it(
      "C1_WEB_04_ApiFailure_ShowsFriendlyError",
      async () => {
        api.get.mockRejectedValue({
          response: {
            data: {
              message:
                "Verification queue is temporarily unavailable.",
            },
          },
        });

        renderQueue();

        await waitFor(
          () => {
            expect(
              screen.getByText(
                "Verification queue is temporarily unavailable."
              )
            ).toBeInTheDocument();
          }
        );

        expect(
          api.get
        ).toHaveBeenCalledTimes(
          1
        );
      }
    );
  }
);