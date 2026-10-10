import api from "./api";

/**
 * Opens a certificate only after the backend confirms that
 * the current authenticated user is allowed to access it.
 *
 * Supports:
 * - Private Supabase certificates through temporary signed URLs.
 * - Legacy local certificates through an authenticated blob request.
 */
export async function openProtectedCertificate(listingId) {
  if (!listingId) {
    throw new Error("A valid gemstone listing is required.");
  }

  // Open a blank tab immediately from the user's click.
  // This prevents browsers from blocking the new tab after
  // the asynchronous API request finishes.
  const certificateWindow = window.open(
    "about:blank",
    "_blank"
  );

  if (!certificateWindow) {
    throw new Error(
      "The certificate window was blocked. Please allow pop-ups for Gemora and try again."
    );
  }

  // Prevent the opened page from controlling the Gemora page.
  certificateWindow.opener = null;

  try {
    // ----------------------------------------------------------
    // STEP 1 — ASK BACKEND FOR AUTHORIZED ACCESS
    // ----------------------------------------------------------

    const accessResponse = await api.get(
      `/GemCertificates/listings/${listingId}/access`
    );

    const access = accessResponse.data;

    if (!access?.kind) {
      throw new Error(
        "The certificate access response was invalid."
      );
    }

    // ----------------------------------------------------------
    // STEP 2 — PRIVATE SUPABASE CERTIFICATE
    //
    // Backend has already:
    // - authenticated the user
    // - checked listing access
    // - generated a temporary signed URL
    // ----------------------------------------------------------

    if (access.kind === "signed") {
      if (!access.url) {
        throw new Error(
          "The certificate access link was not returned."
        );
      }

      certificateWindow.location.replace(access.url);

      return;
    }

    // ----------------------------------------------------------
    // STEP 3 — LEGACY LOCAL CERTIFICATE
    //
    // A normal browser link cannot attach our Bearer JWT.
    // Therefore Axios downloads the protected file using the
    // existing authenticated api instance.
    // ----------------------------------------------------------

    if (access.kind === "legacy") {
      const fileResponse = await api.get(
        `/GemCertificates/listings/${listingId}/legacy`,
        {
          responseType: "blob",
        }
      );

      const certificateBlobUrl =
        URL.createObjectURL(fileResponse.data);

      certificateWindow.location.replace(
        certificateBlobUrl
      );

      // Give the browser enough time to use the blob URL,
      // then release it from memory.
      window.setTimeout(() => {
        URL.revokeObjectURL(certificateBlobUrl);
      }, 60_000);

      return;
    }

    throw new Error(
      "The certificate storage type is not supported."
    );
  } catch (error) {
    if (
      certificateWindow &&
      !certificateWindow.closed
    ) {
      certificateWindow.close();
    }

    throw error;
  }
}