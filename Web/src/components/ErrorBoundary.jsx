import { Component } from "react";
import "./ErrorBoundary.css";

class ErrorBoundary extends Component {
  state = { hasError: false };

  static getDerivedStateFromError() {
    return { hasError: true };
  }

  componentDidCatch(error, info) {
    console.error("Gemora could not render this page:", error, info);
  }

  render() {
    if (this.state.hasError) {
      return (
        <main className="app-error-page" role="alert">
          <div className="app-error-card">
            <h1>We couldn't display this page</h1>
            <p>Something went wrong while loading this page. Please try again.</p>
            <button type="button" onClick={() => window.location.reload()}>
              Reload page
            </button>
            <a href="/">Go to home</a>
          </div>
        </main>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;
