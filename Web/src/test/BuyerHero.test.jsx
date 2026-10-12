import { act, cleanup, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import BuyerHero from "../components/buyer/BuyerHero";

let notifyIntersection;

beforeEach(() => {
  vi.stubGlobal("IntersectionObserver", class {
    constructor(callback) { notifyIntersection = callback; }
    observe() {}
    disconnect() {}
  });
  vi.spyOn(HTMLMediaElement.prototype, "play").mockImplementation(function () {
    Object.defineProperty(this, "paused", { configurable: true, value: false, writable: true });
    this.dispatchEvent(new Event("play"));
    return Promise.resolve();
  });
  vi.spyOn(HTMLMediaElement.prototype, "pause").mockImplementation(function () {
    Object.defineProperty(this, "paused", { configurable: true, value: true, writable: true });
    this.dispatchEvent(new Event("pause"));
  });
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function renderHero() {
  return render(<MemoryRouter><BuyerHero /></MemoryRouter>);
}

describe("BuyerHero film", () => {
  it("plays automatically when visible and pauses when scrolled away", async () => {
    const { container } = renderHero();
    const media = container.querySelector("video");
    expect(media).toHaveAttribute("src", "/videos/buyerside.mp4");
    expect(media).toHaveAttribute("autoplay");
    expect(HTMLMediaElement.prototype.play).not.toHaveBeenCalled();

    await act(async () => notifyIntersection([{ isIntersecting: true }]));
    expect(media.paused).toBe(false);
    expect(container.querySelector(".gm-hero-media-control")).not.toBeInTheDocument();
    await act(async () => notifyIntersection([{ isIntersecting: false }]));
    expect(media.paused).toBe(true);
    await act(async () => notifyIntersection([{ isIntersecting: true }]));
    expect(media.paused).toBe(false);
  });

  it("keeps the requested background film running with reduced motion enabled", async () => {
    vi.stubGlobal("matchMedia", vi.fn(() => ({ matches: true, addEventListener() {}, removeEventListener() {} })));
    const { container } = renderHero();
    await act(async () => notifyIntersection([{ isIntersecting: true }]));
    expect(container.querySelector("video").paused).toBe(false);
    expect(HTMLMediaElement.prototype.play).toHaveBeenCalledTimes(1);
  });

  it("keeps the still hero visible if autoplay is blocked", async () => {
    HTMLMediaElement.prototype.play.mockRejectedValueOnce(new DOMException("Autoplay blocked", "NotAllowedError"));
    const { container } = renderHero();
    await act(async () => notifyIntersection([{ isIntersecting: true }]));
    expect(container.querySelector(".gm-video-hero")).toBeInTheDocument();
    expect(container.querySelector(".gm-hero-media-control")).not.toBeInTheDocument();
  });

  it("keeps the hero when the video fails", () => {
    const { container } = renderHero();
    fireEvent.error(container.querySelector("video"));
    expect(container.querySelector("video")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /Discover a gem/ })).toBeInTheDocument();
  });
});
