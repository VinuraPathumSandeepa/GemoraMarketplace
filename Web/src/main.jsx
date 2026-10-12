import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";

import App from "./App.jsx";
import ErrorBoundary from "./components/ErrorBoundary.jsx";
import "./index.css";
import "./App.css";

import { AuthProvider } from "./context/AuthContext.jsx";

console.log("✅ Gemora app starting...");

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <BrowserRouter>
      <ErrorBoundary>
        <AuthProvider>
          <App />
        </AuthProvider>
      </ErrorBoundary>
    </BrowserRouter>
  </StrictMode>

  // <React.StrictMode>
  //   <BrowserRouter>
  //     <AuthProvider>
  //       <App />
  //     </AuthProvider>
  //   </BrowserRouter>
  // </React.StrictMode>
);
