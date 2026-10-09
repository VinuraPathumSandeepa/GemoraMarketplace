/* Local presentation fixtures only. No calls reach the real API. */
const { chromium } = require("playwright");
const fs = require("node:fs");
const path = require("node:path");
const assert = require("node:assert/strict");
const root = path.resolve(__dirname, "../..");
const output = path.join(root, "docs/ui-redesign-qa");
fs.mkdirSync(output, { recursive: true });
const base = "http://127.0.0.1:5173";
const user = { id: "ui-fixture", fullName: "Amaya Perera", email: "amaya@example.test", role: "Buyer", region: "Colombo", countryCode: "LK", phoneNumber: "0771234567" };
const gems = Array.from({ length: 14 }, (_, i) => ({
  id: i + 1, title: ["Ceylon Blue Sapphire", "Padparadscha Sapphire", "Golden Yellow Sapphire", "Burmese Ruby"][i % 4],
  gemType: i % 4 === 3 ? "Ruby" : "Sapphire", description: "A distinctive gemstone with a carefully documented listing. Review the specifications and certificate reference to discover its individual character.",
  caratWeight: +(2.14 + i * .31).toFixed(2), color: ["Royal blue", "Peach pink", "Golden yellow", "Red"][i % 4],
  clarity: "Eye clean", cut: "Oval", price: 125000 + i * 18450, currency: "LKR",
  primaryImageUrl: i === 1 ? base + "/missing-image.png" : i === 2 ? null : base + (i % 4 === 3 ? "/images/gems/ruby.webp" : "/images/gems/sapphire.webp"),
  certificateNumber: i === 2 ? null : "GEM-2026-" + (i + 100), certificateAuthority: "Sample laboratory",
  countryCode: "LK", region: "Ratnapura", sellerName: "Ceylon Stone House", isAvailable: i !== 1
}));
const delivery = { recipientName: user.fullName, recipientPhone: user.phoneNumber, addressLine1: "10 Sample Lane", city: "Colombo", district: "Colombo", region: "Western", countryCode: "LK", postalCode: "00100", isLocked: false };
const orders = ["Paid", "Confirmed", "Pending", "Completed", "Cancelled", "AwaitingPayment", "Refunded", "Failed"].map((status, i) => ({
  id: String(i + 1), orderNumber: "GEM-UI000" + i, gemListingId: i + 1, gemTitle: gems[i].title,
  sellerName: gems[i].sellerName, gemImageUrl: gems[i].primaryImageUrl, agreedPrice: gems[i].price, currency: "LKR",
  status, fulfillmentStatus: i === 0 ? "Delivered" : "Pending", createdAt: "2026-10-05T09:30:00Z", updatedAt: "2026-10-08T09:30:00Z",
  paidAt: ["Paid", "Completed"].includes(status) ? "2026-10-06T09:30:00Z" : null,
  shippingRegion: "Western", shippingCountryCode: "LK", shippingAddress: "10 Sample Lane, Colombo",
  deliveryDetails: delivery, statusHistory: [{ newStatus: status, createdAt: "2026-10-06T09:30:00Z", reason: "Sample order update" }],
  shipment: { courierName: "Sample Courier", trackingNumber: "TRACK-001", expectedDeliveryDate: "2026-10-10", trackingUrl: "https://example.test/tracking" }, fulfillmentHistory: []
}));
const pages = { marketplace: "/buyer/marketplace", dashboard: "/buyer/dashboard", orders: "/buyer/orders", profile: "/buyer/profile", details: "/buyer/marketplace/1", checkout: "/buyer/checkout/1", payment: "/buyer/orders/2/payment" };
const widths = process.argv.includes("--quick") ? [390, 1440] : [320, 360, 390, 430, 480, 768, 1024, 1280, 1440, 1536, 1920];
let scenario = "normal";
const calls = [];
async function fixture(route) {
  const url = new URL(route.request().url());
  if (url.pathname.toLowerCase().startsWith("/api/")) {
    const p = url.pathname.toLowerCase();
    const method = route.request().method();
    calls.push({ p, method, body: route.request().postData() });
    const respond = data => route.fulfill({ status: 200, contentType: "application/json", body: JSON.stringify(data) });
    if (p === "/api/auth/me") return respond(user);
    if (p === "/api/marketplace/stats") return respond({ authorizedSellers: 8, registeredBuyers: 26, activeGemListings: 14, successfulTransactions: 25 });
    if (p === "/api/marketplace/gems") {
      if (scenario === "error") return route.fulfill({ status: 503, contentType: "application/json", body: JSON.stringify({ message: "Sample service unavailable." }) });
      if (scenario === "empty") return respond({ items: [], totalPages: 1, totalItems: 0 });
      const current = +(url.searchParams.get("page") || 1), size = +(url.searchParams.get("pageSize") || 12);
      return respond({ items: gems.slice((current - 1) * size, current * size), totalPages: Math.ceil(gems.length / size), totalItems: gems.length });
    }
    if (p.startsWith("/api/marketplace/gems/")) return respond(gems.find(g => g.id === +p.split("/").pop()));
    if (p === "/api/orders/my") return respond(scenario === "empty" ? [] : orders);
    if (p.endsWith("/complete")) return respond({ ...orders[0], status: "Completed" });
    if (p.endsWith("/payment")) return respond({ orderId: "2", status: "Paid", paymentStatus: "Paid", transactionReference: "UI-TEST", amount: orders[1].agreedPrice, currency: "LKR" });
    if (p.endsWith("/delivery-details")) return respond({ ...orders[1], deliveryDetails: { ...delivery, ...route.request().postDataJSON() } });
    if (p === "/api/orders" && method === "POST") return respond({ id: "ui-created", status: "Pending" });
    if (p.startsWith("/api/orders/")) return respond(orders.find(o => o.id === p.split("/").pop()) || orders[1]);
    if (p.endsWith("/agent/assist")) return respond({ answer: "Sample assistant response. Review gemstone details before choosing.", conversationId: "ui-test" });
    throw new Error("Unexpected API path: " + p);
  }
  if (url.hostname !== "127.0.0.1" && url.hostname !== "localhost") return route.abort();
  return route.continue();
}
async function main() {
  const browser = await chromium.launch({ channel: "chrome", headless: true });
  const context = await browser.newContext({ reducedMotion: "reduce" });
  await context.addInitScript(() => { localStorage.setItem("gemora_token", "local-ui-fixture-only"); });
  await context.route("**/*", fixture);
  const page = await context.newPage();
  const errors = [];
  page.on("pageerror", e => errors.push(e.message));
  const report = [];
  for (const theme of process.argv.includes("--quick") ? ["light"] : ["light", "dark"]) {
    await page.goto(base);
    await page.evaluate(theme => localStorage.setItem("gemora_buyer_theme", theme), theme);
    for (const [name, route] of Object.entries(pages)) {
      for (const width of widths) {
        await page.setViewportSize({ width, height: 960 });
        await page.goto(base + route);
        await page.waitForSelector(".gm-buyer");
        await page.waitForTimeout(500);
        await page.evaluate(async () => { for (const img of document.images) { img.loading = "eager"; } await Promise.all(Array.from(document.images).map(img => img.decode().catch(() => {}))); });
        const metrics = await page.evaluate(() => {
          const visible = e => { const r = e.getBoundingClientRect(); return r.width && r.height && getComputedStyle(e).visibility !== "hidden" && !e.closest("dialog:not([open])"); };
          const intentional = e => !!e.closest(".gm-featured-grid,.orders-filter-tabs");
          const overflow = Array.from(document.querySelectorAll(".gm-buyer *")).filter(e => visible(e) && !intentional(e) && e.getBoundingClientRect().right > innerWidth + 1).slice(0, 12).map(e => ({ tag: e.tagName, class: e.className, right: Math.round(e.getBoundingClientRect().right) }));
          const tiny = Array.from(document.querySelectorAll(".gm-main button,.gm-main input,.gm-main select")).filter(e => visible(e) && e.getBoundingClientRect().height < 43).map(e => ({ class: e.className, height: e.getBoundingClientRect().height }));
          return { documentWidth: document.documentElement.scrollWidth, viewport: innerWidth, overflow, tiny };
        });
        const file = name + "-" + theme + "-" + width + ".png";
        await page.screenshot({ path: path.join(output, file), fullPage: true });
        report.push({ name, theme, width, ...metrics, screenshot: file });
      }
      console.log("Checked " + name + " / " + theme + " at " + widths.join(", "));
    }
  }
  fs.writeFileSync(path.join(output, "matrix.json"), JSON.stringify({ report, errors }, null, 2));
  console.log(JSON.stringify({ cells: report.length, overflow: report.filter(r => r.overflow.length || r.documentWidth > r.viewport), smallControls: report.filter(r => r.tiny.length), errors }, null, 2));
  if (!process.argv.includes("--quick")) await interactions(page);
  await browser.close();
  assert.equal(errors.length, 0, "Browser runtime errors");
  assert.equal(report.filter(r => r.overflow.length || r.documentWidth > r.viewport).length, 0, "Viewport overflow");
}
async function interactions(page) {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(base + pages.marketplace);
  await page.getByRole("button", { name: "Open navigation" }).click();
  assert(await page.locator("dialog[open]").isVisible());
  for (let i = 0; i < 12; i++) await page.keyboard.press("Tab");
  assert(await page.evaluate(() => !!document.activeElement.closest("dialog[open]")));
  await page.keyboard.press("Escape");
  assert(await page.getByRole("button", { name: "Open navigation" }).evaluate(e => e === document.activeElement));
  await page.getByRole("button", { name: /^Filters/ }).click();
  await page.locator('dialog[open] select').selectOption("Ruby");
  await page.getByRole("button", { name: /Show .* gemstones/ }).click();
  assert.equal(await page.locator(".gm-gem-card").count(), 3);
  await page.getByRole("button", { name: "Remove filter Ruby" }).click();
  await page.getByRole("button", { name: "Next", exact: true }).click();
  assert.equal(await page.locator(".gm-gem-card").count(), 2);
  await page.getByRole("button", { name: "Previous", exact: true }).click();
  await page.getByRole("button", { name: "Quick view Ceylon Blue Sapphire", exact: true }).first().click();
  assert(await page.locator("dialog[open]").isVisible());
  await page.keyboard.press("Escape");
  await page.getByRole("searchbox").fill("No such stone");
  await page.getByRole("heading", { name: "A different search may uncover your gem" }).waitFor();
  await page.getByRole("button", { name: "Clear filters", exact: true }).click();
  await page.goto(base + "/buyer/marketplace/2");
  assert.equal(await page.getByRole("link", { name: /Place.*order/ }).count(), 0);
  await page.goto(base + pages.profile);
  await page.getByRole("button", { name: "Edit profile", exact: true }).click();
  await page.getByLabel("Full name", { exact: true }).fill("Preview Person");
  await page.getByRole("button", { name: "Save preview" }).click();
  await page.getByRole("status").waitFor();
  assert(!calls.some(c => c.method !== "GET" && c.p.includes("/auth/")));
  await page.goto(base + pages.orders);
  await page.getByRole("button", { name: "Order Details", exact: true }).first().click();
  await page.getByText("TRACK-001", { exact: true }).first().waitFor();
  assert.equal(await page.getByRole("button", { name: "Confirm Delivery", exact: true }).count(), 1);
  assert.equal(await page.getByRole("button", { name: "Pay Now", exact: true }).count(), 2);
  await page.goto(base + pages.checkout);
  await page.getByLabel("Recipient Name *", { exact: true }).fill("UI Test");
  for (const [name, value] of Object.entries({ recipientPhone: "0771234567", addressLine1: "10 Sample Lane", city: "Colombo", district: "Colombo", region: "Western", postalCode: "00100" })) await page.locator('[name="' + name + '"]').fill(value);
  await page.getByRole("button", { name: "Place Order Request" }).click();
  await page.waitForURL("**/buyer/orders");
  const orderCall = calls.find(c => c.p === "/api/orders" && c.method === "POST");
  assert.equal(JSON.parse(orderCall.body).deliveryDetails.recipientName, "UI Test");
  assert.equal(JSON.parse(orderCall.body).gemListingId, 1);
  await page.goto(base + pages.payment);
  assert(await page.getByRole("button", { name: /^Pay LKR/ }).isEnabled());
  await page.getByRole("button", { name: "Edit Delivery Details" }).click();
  assert(await page.getByRole("button", { name: /^Pay LKR/ }).isDisabled());
  await page.getByRole("button", { name: "Cancel", exact: true }).click();
  await page.getByRole("button", { name: "Open Gemora AI Assistant" }).click();
  assert(await page.locator("dialog[open]").isVisible());
  await page.keyboard.press("Escape");
  assert(await page.getByRole("button", { name: "Open Gemora AI Assistant" }).evaluate(e => e === document.activeElement));
  for (const state of ["empty", "error"]) {
    scenario = state;
    await page.goto(base + pages.marketplace);
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(output, "marketplace-" + state + ".png"), fullPage: true });
    assert(await page.locator(".gm-empty").isVisible());
  }
  scenario = "normal";
  console.log("PASS: drawer/focus, filters, pagination, quick view, empty/error, unavailable gem, local profile, order actions/history, checkout payload, payment gating, assistant focus.");
}
main().catch(e => { console.error(e); process.exit(1); });
