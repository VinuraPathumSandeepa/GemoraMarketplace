import api from "./api";


// ============================================================
// MARKETPLACE AI ASSISTANT
// ============================================================

export async function askMarketplaceAgent(
  message,
  conversationId = null
) {
  const cleanMessage =
    message?.trim();


  if (!cleanMessage) {
    throw new Error(
      "Please enter a message."
    );
  }


  try {
    const response =
      await api.post(
        "/marketplace/agent/assist",
        {
          message:
            cleanMessage,

          conversationId:
            conversationId || null,
        }
      );


    return response.data;
  } catch (error) {
    const status =
      error?.response?.status;


    const backendMessage =
      error?.response?.data?.message;


    // ========================================================
    // GEMINI QUOTA
    // ========================================================

    if (status === 429) {
      throw new Error(
        "The AI assistant has reached its current Gemini request limit. Please try again later."
      );
    }


    // ========================================================
    // LOGIN / TOKEN
    // ========================================================

    if (status === 401) {
      throw new Error(
        "Your login session has expired. Please sign in again."
      );
    }


    // ========================================================
    // ROLE ACCESS
    // ========================================================

    if (status === 403) {
      throw new Error(
        "Only authenticated buyers can use the marketplace assistant."
      );
    }


    // ========================================================
    // TEMPORARY AI FAILURE
    // ========================================================

    if (
      status === 502 ||
      status === 503 ||
      status === 504
    ) {
      throw new Error(
        "The AI service is temporarily unavailable. Please try again shortly."
      );
    }


    throw new Error(
      backendMessage ||
      "Unable to contact the marketplace assistant."
    );
  }
}