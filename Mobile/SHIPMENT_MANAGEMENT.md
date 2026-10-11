# GEMORA mobile shipment management

Implemented as Flutter screens using the existing authenticated backend. No WebView, new backend endpoint, database migration, or dependency was added.

## Screens

- Seller Workspace: dedicated My Shipments and Orders cards; Orders app bar action. Removed the shipment button that was incorrectly nested in every dashboard card.
- My Shipments: cream background, green headings, gold accents, status badges, real routes/value/creation dates, loading/empty/error states, pull to refresh, creation and details navigation. The selected My Shipments title uses gold highlighting.
- Create Shipment: eligible paid-order selector, destination/value/currency autofill, origin and destination validation, package description, optional weight/dimensions, Standard/Express/Priority service, export documentation switch, handling notes, disabled controls during submission, backend validation messages, and navigation to the created shipment.
- Shipment Details: full IDs, routes, financial information, package fields, courier/service/tracking data, persisted shipping plan and approval status, plan generation, insurance, tracking timeline, refresh, and separate errors for optional sections.
- Order Requests: scrollable bottom sheet, unread pending-order count, show all/pending toggle, refresh, order details, approve/reject/already-sold confirmation dialogs, rejection reason, and mark read. Order details are reloaded from the backend.

Financial fields are displayed from the paid order, and deliberately omitted from the creation payload because the backend derives them. Insurance recommendation/coverage comes from the shipping plan; the website's creation contract has no insurance-purchase field. Final plan approval and insurance purchase remain administrator actions.

## File inventory

Created:

- `lib/models/shipment_model.dart`
- `lib/screens/shipments/shipment_widgets.dart`
- `lib/screens/shipments/seller_order_inbox.dart`
- `test/shipment_management_test.dart`
- `SHIPMENT_MANAGEMENT.md`

Modified:

- `lib/services/shipment_service.dart`
- `lib/screens/dashboards/seller_dashboard.dart`
- `lib/screens/shipments/seller_shipment_list.dart`
- `lib/screens/shipments/create_shipment_screen.dart`
- `lib/screens/shipments/shipment_detail_screen.dart`

The feature files are synchronized between the workspace checkout and the running checkout at `C:\Users\levin\Pictures\Project\GemoraMarketplace`.

## Reused API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/shipments/my` | Authenticated seller's shipments |
| GET | `/api/orders/my-shipment-eligible` | Paid orders without an existing shipment |
| POST | `/api/shipments` | Create shipment |
| GET | `/api/shipments/{id}` | Shipment details |
| GET / POST | `/api/shipments/{id}/plan` | Read / generate shipping plan |
| GET | `/api/shipments/{id}/insurance` | Insurance record |
| GET | `/api/shipments/{id}/tracking` | Tracking history |
| GET | `/api/seller/orders` | Seller inbox and all orders |
| GET | `/api/seller/orders/{id}` | Order details |
| POST | `/api/seller/orders/{id}/approve` | Existing order approval workflow |
| POST | `/api/seller/orders/{id}/reject` | Rejection or already-sold action |
| POST | `/api/seller/orders/{id}/message/read` | Mark request read |

Requests reuse `ApiConfig` and `TokenStorageService`, send bearer authentication, and surface session, authorization, network, timeout, and backend validation errors. Only a 404 is treated as an absent optional insurance/plan record.

## Verification

- `flutter test`: 15 passing tests, including 14 new shipment/model/API/widget tests and the existing basic test.
- `flutter analyze --no-pub`: no issues found.
- Widget verification includes a 320-pixel-wide phone layout, loading/empty/error/retry states, paid-order autofill, order filtering and unread counts, dashboard navigation, details navigation, and partial tracking failure.
- API tests verify authentication headers, correct creation payload, omitted financial fields, optional-record behavior, validation errors, and seller decision contracts. Production uses the real backend; HTTP mocks exist only in tests.

An Android/iOS device build and authenticated end-to-end creation/decisions have not been tested for this change. iOS cannot be built on this Windows laptop. No new backend functionality was required.

## Review locally

Use the existing local browser preview at `http://localhost:5174`. Refresh after rebuilding. This runs the same Flutter code used by the mobile app.

From the running `Mobile` directory, rebuild/start with `./run-browser.ps1`; if the server is already running, build with:

```powershell
& D:\GemoraTools\flutter\bin\flutter.bat --no-version-check build web --debug --no-pub --dart-define=API_SERVER_URL=http://localhost:5174
```

Manual acceptance steps:

1. Sign in as a seller and open Seller Workspace → My Shipments.
2. Verify existing shipment cards, then pull to refresh or use the refresh controls.
3. Select Create New Shipment and choose an eligible paid order. Verify its actual address, value, and currency autofill.
4. Enter origin/package details. Check required fields, two-letter country codes, and positive optional weight.
5. Create a test shipment and verify its detail screen and the refreshed list after returning.
6. Check saved plan, insurance, and tracking records. Where no plan exists, generate one and verify that approval remains pending until an administrator acts.
7. Open Orders. Check pending/all filters, unread count, refresh, and View Order. Perform approve/reject/already-sold actions only on designated test orders; these actions change real order and listing state.
8. On Android/iOS, check a narrow viewport and keyboard scrolling in the form and rejection dialog. Sign out/re-authenticate if the server reports an expired session.
