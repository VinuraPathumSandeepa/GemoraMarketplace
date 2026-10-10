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
  Route,
  Routes,
} from "react-router-dom";

import VerificationDetails from "./VerificationDetails";

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
  "./GemVerificationAgentPanel",
  () => ({
    default: ({
      verification,
      analyzing,
    }) => (
      <div data-testid="ai-panel">
        AI Panel -{" "}
        {verification?.aiStatus ??
          "NotStarted"}

        {analyzing
          ? " - Analyzing"
          : ""}
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

    title:
      "Royal Blue Sapphire",

    gemType:
      "Sapphire",

    sellerName:
      "Test Seller",

    description:
      "Natural blue sapphire submitted for professional verification.",

    caratWeight:
      2.5,

    color:
      "Royal Blue",

    clarity:
      "Eye Clean",

    cut:
      "Oval Mixed Cut",

    price:
      250000,

    currency:
      "LKR",

    primaryImageUrl:
      null,

    certificateNumber:
      "CERT-C1-101",

    certificateAuthority:
      "Gemora Test Laboratory",

    certificateUrl:
      null,

    listingStatus:
      "PendingVerification",

    decision:
      "Pending",

    reviewNotes:
      null,

    reviewedAt:
      null,

    gemologistName:
      null,

    aiStatus:
      "NotStarted",

    aiSuggestedGemType:
      null,

    aiConfidenceScore:
      null,

    aiFindings:
      null,

    aiRiskFlags:
      null,

    aiProcessedAt:
      null,

    createdAt:
      "2026-10-10T09:00:00Z",

    ...overrides,
  };
}

function renderDetails() {
  return render(
    <MemoryRouter
      initialEntries={[
        "/gemologist/verifications/101",
      ]}
    >
      <Routes>
        <Route
          path="/gemologist/verifications/:id"
          element={
            <VerificationDetails />
          }
        />

        <Route
          path="/gemologist/verifications"
          element={
            <div>
              Verification Queue
            </div>
          }
        />
      </Routes>
    </MemoryRouter>
  );
}

describe(
  "Component 1 - Verification Details",
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
      "C1_WEB_05_PendingVerification_RendersEvidenceAndHumanDecisionControls",
      async () => {
        api.get.mockResolvedValue({
          data:
            createVerification(),
        });

        renderDetails();

        expect(
          await screen.findByRole(
            "heading",
            {
              name:
                "Royal Blue Sapphire",
            }
          )
        ).toBeInTheDocument();

        expect(
          screen.getAllByText(
            "Test Seller"
          ).length
        ).toBeGreaterThanOrEqual(
          1
        );

        expect(
          screen.getByText(
            "PendingVerification"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByText(
            "Pending"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByTestId(
            "ai-panel"
          )
        ).toBeInTheDocument();

        expect(
          screen.getByRole(
            "button",
            {
              name: /Approve/i,
            }
          )
        ).toBeInTheDocument();

        expect(
          screen.getByRole(
            "button",
            {
              name:
                /Request Changes/i,
            }
          )
        ).toBeInTheDocument();

        expect(
          screen.getByRole(
            "button",
            {
              name: /Reject/i,
            }
          )
        ).toBeInTheDocument();

        expect(
          api.get
        ).toHaveBeenCalledWith(
          "/GemVerifications/101"
        );
      }
    );

    it(
      "C1_WEB_06_RequestChangesWithoutNotes_IsBlocked",
      async () => {
        const user =
          userEvent.setup();

        api.get.mockResolvedValue({
          data:
            createVerification(),
        });

        renderDetails();

        await screen.findByRole(
          "heading",
          {
            name:
              "Royal Blue Sapphire",
          }
        );

        await user.click(
          screen.getByRole(
            "button",
            {
              name:
                /Request Changes/i,
            }
          )
        );

        expect(
          screen.getByText(
            "Please explain what the seller needs to correct before requesting changes."
          )
        ).toBeInTheDocument();

        expect(
          api.put
        ).not.toHaveBeenCalled();
      }
    );

    it(
      "C1_WEB_07_ApproveDecision_SendsExpectedReviewRequest",
      async () => {
        const user =
          userEvent.setup();

        const pending =
          createVerification();

        api.get.mockResolvedValue({
          data: pending,
        });

        api.put.mockResolvedValue({
          data: {
            ...pending,

            decision:
              "Approved",

            listingStatus:
              "Approved",
          },
        });

        renderDetails();

        await screen.findByRole(
          "heading",
          {
            name:
              "Royal Blue Sapphire",
          }
        );

        await user.click(
          screen.getByRole(
            "button",
            {
              name: /Approve/i,
            }
          )
        );

        expect(
          screen.getByText(
            "Approve this gemstone verification?"
          )
        ).toBeInTheDocument();

        await user.click(
          screen.getByRole(
            "button",
            {
              name:
                "Confirm Approved",
            }
          )
        );

        await waitFor(
          () => {
            expect(
              api.put
            ).toHaveBeenCalledWith(
              "/GemVerifications/101/review",
              {
                decision:
                  "Approved",

                reviewNotes:
                  null,
              }
            );
          }
        );

        expect(
          await screen.findByText(
            "Gemstone verification approved successfully."
          )
        ).toBeInTheDocument();
      }
    );

    it(
      "C1_WEB_08_LoadFailure_ShowsFriendlyVerificationError",
      async () => {
        api.get.mockRejectedValue({
          response: {
            data: {
              message:
                "Verification record could not be loaded.",
            },
          },
        });

        renderDetails();

        expect(
          await screen.findByRole(
            "heading",
            {
              name:
                "Verification unavailable",
            }
          )
        ).toBeInTheDocument();

        expect(
          screen.getByText(
            "Verification record could not be loaded."
          )
        ).toBeInTheDocument();

        expect(
          api.get
        ).toHaveBeenCalledWith(
          "/GemVerifications/101"
        );
      }
    );
  }
);