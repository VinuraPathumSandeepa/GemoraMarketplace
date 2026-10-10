import { Dialog } from "./BuyerUI";
import {
  useRef,
  useState,
} from "react";

import {
  Bot,
  MessageCircle,
  Send,
  Trash2,
  X,
} from "lucide-react";

import {
  askMarketplaceAgent,
} from "../../services/marketplaceAgentApi";

import "../../styles/marketplaceAssistant.css";


function cleanAssistantText(text) {
  if (!text) {
    return "";
  }


  return text
    .replace(/\*\*/g, "")
    .replace(/^#{1,6}\s+/gm, "")
    .trim();
}


export default function MarketplaceAiAssistant() {
  const [
    isOpen,
    setIsOpen,
  ] = useState(false);


  const [
    message,
    setMessage,
  ] = useState("");


  const [
    conversationId,
    setConversationId,
  ] = useState(null);


  const [
    loading,
    setLoading,
  ] = useState(false);


  const [
    messages,
    setMessages,
  ] = useState([
    {
      id: 1,

      role:
        "assistant",

      text:
        "Hi! I’m the Gemora Buyer Assistant. Ask me to find gemstones, compare listings, check your orders, or track a shipment.",
    },
  ]);


  const messageIdRef =
    useRef(2);


  // ============================================================
  // ADD MESSAGE
  // ============================================================

  const addMessage = (
    role,
    text
  ) => {
    const newMessage = {
      id:
        messageIdRef.current++,

      role,

      text,
    };


    setMessages(
      (previous) => [
        ...previous,
        newMessage,
      ]
    );
  };


  // ============================================================
  // SEND MESSAGE
  // ============================================================

  const handleSend =
    async () => {
      const currentMessage =
        message.trim();


      if (
        !currentMessage ||
        loading
      ) {
        return;
      }


      addMessage(
        "user",
        currentMessage
      );


      setMessage("");

      setLoading(true);


      try {
        const response =
          await askMarketplaceAgent(
            currentMessage,
            conversationId
          );


        if (
          response?.conversationId
        ) {
          setConversationId(
            response.conversationId
          );
        }


        addMessage(
          "assistant",
          cleanAssistantText(
            response?.message
          ) ||
            "I received your request, but no response was returned."
        );
      } catch (error) {
        addMessage(
          "error",
          error?.message ||
            "Something went wrong while contacting the AI assistant."
        );
      } finally {
        setLoading(false);
      }
    };


  // ============================================================
  // ENTER TO SEND
  // SHIFT + ENTER FOR NEW LINE
  // ============================================================

  const handleKeyDown =
    (event) => {
      if (
        event.key === "Enter" &&
        !event.shiftKey
      ) {
        event.preventDefault();

        handleSend();
      }
    };


  // ============================================================
  // CLEAR CHAT
  // ============================================================

  const clearChat = () => {
    setConversationId(null);

    setMessage("");

    setMessages([
      {
        id:
          messageIdRef.current++,

        role:
          "assistant",

        text:
          "Chat cleared. How can I help you with the Gemora marketplace?",
      },
    ]);
  };


  return (
    <>
      {/* ======================================================
          FLOATING BUTTON
          ====================================================== */}

      {(
        <button
          type="button"
          className="gemora-ai-floating-button"
          onClick={() =>
            setIsOpen(true)
          }
          aria-label="Open Gemora AI Assistant"
        >
          <MessageCircle
            size={24}
          />

          <span>
            Ask Gemora AI
          </span>
        </button>
      )}


      {/* ======================================================
          CHAT PANEL
          ====================================================== */}

      <Dialog open={isOpen} onClose={() => setIsOpen(false)} title="Ask Gemora" className="gm-assistant-dialog">
        <section className="gemora-ai-panel">

          {/* ==================================================
              HEADER
              ================================================== */}

          <header className="gemora-ai-header">
            <div className="gemora-ai-title">
              <div className="gemora-ai-icon">
                <Bot size={22} />
              </div>

              <div>
                <strong>
                  Gemora Assistant
                </strong>

                <span>
                  Marketplace & Buyer Help
                </span>
              </div>
            </div>


            <div className="gemora-ai-header-actions">

              <button
                type="button"
                onClick={
                  clearChat
                }
                title="Clear chat"
              >
                <Trash2
                  size={18}
                />
              </button>


              <button
                type="button"
                onClick={() =>
                  setIsOpen(false)
                }
                title="Close"
              >
                <X
                  size={19}
                />
              </button>

            </div>
          </header>


          {/* ==================================================
              MESSAGES
              ================================================== */}

          <div className="gemora-ai-messages" role="log" aria-live="polite" aria-label="Conversation">

            {messages.map(
              (item) => (
                <div
                  key={
                    item.id
                  }
                  className={`gemora-ai-message-row ${item.role}`}
                >
                  {item.role !==
                    "user" && (
                    <div className="gemora-ai-message-avatar">
                      <Bot
                        size={16}
                      />
                    </div>
                  )}


                  <div
                    className={`gemora-ai-message ${item.role}`}
                  >
                    {item.text}
                  </div>
                </div>
              )
            )}


            {loading && (
              <div className="gemora-ai-message-row assistant">

                <div className="gemora-ai-message-avatar">
                  <Bot
                    size={16}
                  />
                </div>


                <div className="gemora-ai-message assistant gemora-ai-thinking">
                  <span />
                  <span />
                  <span />
                </div>

              </div>
            )}

          </div>


          {/* ==================================================
              QUICK QUESTIONS
              ================================================== */}

          <div className="gemora-ai-suggestions">

            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setMessage(
                  "Show me available gemstones"
                )
              }
            >
              Available gems
            </button>


            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setMessage(
                  "Show me my recent orders"
                )
              }
            >
              My orders
            </button>


            <button
              type="button"
              disabled={loading}
              onClick={() =>
                setMessage(
                  "What should I consider when buying a sapphire?"
                )
              }
            >
              Buying advice
            </button>

          </div>


          {/* ==================================================
              INPUT
              ================================================== */}

          <div className="gemora-ai-input-area">

            <textarea
              value={message}
              onChange={(event) =>
                setMessage(
                  event.target.value
                )
              }
              onKeyDown={
                handleKeyDown
              }
              aria-label="Your message to Gemora"
              placeholder="Ask about gems, orders or delivery..."
              rows={2}
              disabled={loading}
            />


            <button
              type="button"
              className="gemora-ai-send-button"
              onClick={
                handleSend
              }
              disabled={
                loading ||
                !message.trim()
              }
              aria-label="Send message"
            >
              <Send
                size={18}
              />
            </button>

          </div>


          <div className="gemora-ai-footer">
            Gemora AI uses verified marketplace data when tools are required.
          </div>

        </section>
      </Dialog>
    </>
  );
}
