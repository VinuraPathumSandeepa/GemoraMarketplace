# Component 3: Secure Shipping & Insurance

## Overview

Component 3 implements a complete shipping and insurance workflow for the Gemora Marketplace, providing AI-powered shipping plan generation, admin-approved courier booking, insurance policy management, and real-time tracking with exception handling.

**Key Features:**
- AI-enhanced shipping risk assessment with deterministic fallback
- Multi-role authorization (Seller, Buyer, Admin)
- Admin approval lifecycle for shipping plans
- Simulated courier booking with idempotency
- Insurance policy creation and management
- Chronological tracking timeline with status transitions
- Exception handling and delivery failure workflows

---

## Architecture

### Layered Design

```
┌─────────────────────────────────────────┐
│         Presentation Layer               │
│  ┌──────────┐  ┌──────────┐            │
│  │  React   │  │ Flutter  │            │
│  │  Web App │  │ Mobile   │            │
│  └──────────┘  └──────────┘            │
└──────────────┬──────────────────────────┘
               │ HTTP/REST
┌──────────────▼──────────────────────────┐
│         API Layer (Gemora.API)          │
│  ┌──────────────────────────────────┐  │
│  │  ShipmentsController             │  │
│  │  OrdersController                │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     Application Layer                   │
│  ┌──────────────────────────────────┐  │
│  │  ShipmentService                 │  │
│  │  ShippingAgentService            │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     Domain Layer                        │
│  ┌──────────────────────────────────┐  │
│  │  Entities:                       │  │
│  │  - Shipment                      │  │
│  │  - ShippingPlan                  │  │
│  │  - InsuranceRecord               │  │
│  │  - ShipmentTrackingEvent         │  │
│  │                                  │  │
│  │  Interfaces:                     │  │
│  │  - ILlmProvider (AI abstraction) │  │
│  │  - IShippingProviderAdapter      │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     Infrastructure Layer                │
│  ┌──────────────────────────────────┐  │
│  │  ApplicationDbContext (EF Core)  │  │
│  │  MockLlmProvider (Phase 7)       │  │
│  │  MockShippingProviderAdapter     │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│     Database (Supabase PostgreSQL)      │
└─────────────────────────────────────────┘
```

### Key Design Patterns

1. **Provider Abstraction**: `ILlmProvider` and `IShippingProviderAdapter` isolate external services behind interfaces
2. **Retry with Timeout**: Bounded retry (3 attempts) with exponential backoff and 30s timeout
3. **Fallback Mechanism**: AI failures gracefully fall back to deterministic rule-based planning
4. **Idempotency**: Duplicate booking requests return existing data instead of creating duplicates
5. **Approval Invalidation**: Plan regeneration automatically clears previous admin approval

---

## Entity Relationships

```
User (Buyer/Seller/Admin)
    ├── Orders (1:N)
    │   └── Shipments (1:N via OrderId)
    │       ├── ShippingPlan (1:1)
    │       ├── InsuranceRecord (1:1)
    │       └── ShipmentTrackingEvent (1:N)

Shipment
├── Id (Guid, PK)
├── OrderId (Guid, FK → Orders.Id)
├── SellerId (Guid, FK → Users.Id)
├── BuyerId (Guid, FK → Users.Id)
├── OriginAddress, OriginRegion, OriginCountryCode
├── DestinationAddress, DestinationRegion, DestinationCountryCode
├── DeclaredValue (decimal), Currency
├── PackageDescription, PackageWeight, PackageDimensions
├── SpecialHandlingNotes
├── PreferredService, ExportRequired (bool)
├── Status (enum: Pending, Planning, ReadyForBooking, Booked, PickedUp, InTransit, CustomsHold, OutForDelivery, Delivered, DeliveryFailed, Cancelled, Exception)
├── RiskLevel (Low, Medium, High, Critical)
├── TrackingNumber, CourierName, ExternalShipmentReference
├── CreatedAt, UpdatedAt, BookedAt, ShippedAt, DeliveredAt
└── Navigation: ShippingPlan, InsuranceRecord, TrackingEvents

ShippingPlan
├── Id (Guid, PK)
├── ShipmentId (Guid, FK → Shipments.Id)
├── RiskLevel, RiskReasons (text)
├── RecommendedServiceType
├── InsuranceRecommended (bool), RecommendedCoverageAmount
├── HandlingRequirements (text), RequiredDocuments (text), Warnings (text)
├── GenerationSource (AI | FallbackRules) [Phase 7]
├── ExecutionSummary (text, audit trail) [Phase 7]
├── IsApproved (bool), ApprovedBy (Guid?), ApprovedAt (DateTime?)
├── AdminNotes (text)
└── CreatedAt, UpdatedAt

InsuranceRecord
├── Id (Guid, PK)
├── ShipmentId (Guid, FK → Shipments.Id)
├── DeclaredValue, CoverageAmount, Currency
├── CoverageType (Standard, Premium, Comprehensive)
├── PolicyNumber, PolicyReference, ProviderName
├── PremiumAmount
├── PolicyStartDate, PolicyEndDate
├── Status (Pending, Active, Claimed, Cancelled)
└── CreatedAt, UpdatedAt

ShipmentTrackingEvent
├── Id (Guid, PK)
├── ShipmentId (Guid, FK → Shipments.Id)
├── EventType (Booked, PickedUp, InTransit, CustomsHold, OutForDelivery, Delivered, DeliveryFailed, Exception)
├── Location, Description
├── ExternalEventCode (SIM-EVT-*)
├── OccurredAt (DateTime), RecordedAt (DateTime)
└── Navigation: Shipment
```

---

## API Endpoints

### Authentication
All endpoints require JWT Bearer token in `Authorization` header.

### Orders

#### GET `/api/Orders/my-shipment-eligible`
Returns paid orders without active shipments for the authenticated seller.

**Response:**
```json
[
  {
    "id": "guid",
    "totalAmount": 850000.00,
    "currency": "LKR",
    "status": "Paid",
    "paidAt": "2026-09-27T16:39:09Z",
    "gemListing": { ... }
  }
]
```

### Shipments

#### POST `/api/Shipments`
Create a new shipment from a paid order.

**Request:**
```json
{
  "orderId": "guid",
  "originAddress": "Seller Warehouse, Colombo",
  "originRegion": "Western Province",
  "originCountryCode": "LK",
  "destinationAddress": "Buyer Address",
  "destinationRegion": "Central Province",
  "destinationCountryCode": "LK",
  "declaredValue": 850000.00,
  "currency": "LKR",
  "packageDescription": "2.5 carat Blue Sapphire",
  "packageWeight": 0.5,
  "specialHandlingNotes": "Handle with care",
  "preferredService": "Insured Express",
  "exportRequired": false
}
```

**Authorization:** Seller must own the order.

#### GET `/api/Shipments/{id}`
Get shipment details by ID.

**Authorization:**
- Seller: Own shipments only
- Buyer: Own purchases only
- Admin: Any shipment

#### GET `/api/Shipments/my`
Get all shipments for authenticated user (filtered by role).

#### POST `/api/Shipments/{id}/plan/generate`
Generate or regenerate shipping plan using AI agent.

**Authorization:** Seller or Admin (shipment owner or oversight).

**Behavior:**
- Attempts AI-powered analysis first
- Falls back to deterministic rules on failure
- Regeneration invalidates previous admin approval

#### GET `/api/Shipments/{id}/plan`
Retrieve persisted shipping plan.

**Authorization:** Same as GET shipment.

#### POST `/api/Shipments/{id}/plan/approve`
Admin approves shipping plan.

**Request:**
```json
{
  "adminNotes": "Approved for high-value shipment"
}
```

**Authorization:** Admin only.

#### POST `/api/Shipments/{id}/book`
Book courier for approved shipment.

**Preconditions:**
1. Shipment exists
2. Shipping plan exists
3. Plan is approved
4. Status is ReadyForBooking
5. Not already booked

**Authorization:** Admin only.

**Response:**
```json
{
  "shipmentId": "guid",
  "trackingNumber": "SIM-TRK-0001",
  "courierName": "DEMO Gemora Courier Sandbox",
  "externalReference": "SIM-BOOK-0001",
  "status": "Booked"
}
```

#### GET `/api/Shipments/{id}/tracking`
Get chronological tracking timeline.

**Response:**
```json
[
  {
    "id": "guid",
    "eventType": "Booked",
    "location": "Colombo",
    "description": "Courier booking confirmed",
    "externalEventCode": "SIM-EVT-BOOK-0001",
    "occurredAt": "2026-10-01T16:39:10Z",
    "recordedAt": "2026-10-01T16:39:10Z"
  }
]
```

#### POST `/api/Shipments/{id}/tracking`
Add tracking event (updates shipment status).

**Request:**
```json
{
  "eventType": "PickedUp",
  "location": "Colombo Hub",
  "description": "Package collected from seller"
}
```

**Authorization:** Admin or authorized courier system.

**Valid Transitions:**
- Booked → PickedUp
- PickedUp → InTransit
- InTransit → CustomsHold / OutForDelivery
- OutForDelivery → Delivered / DeliveryFailed / Exception

**Restrictions:**
- Sellers cannot mark as Delivered
- Buyers cannot modify tracking

#### GET `/api/Shipments/{id}/insurance`
Get insurance record for shipment.

**Response:**
```json
{
  "id": "guid",
  "coverageAmount": 1450000.00,
  "currency": "LKR",
  "coverageType": "Full Declared Value",
  "policyNumber": "SIM-POL-0001",
  "policyReference": "DEMO-GEMORA-INS-0001",
  "providerName": "DEMO Gemora Insurance Sandbox",
  "premiumAmount": 14500.00,
  "status": "Active"
}
```

#### POST `/api/Shipments/{id}/insurance`
Create insurance record.

**Request:**
```json
{
  "coverageAmount": 1450000.00,
  "currency": "LKR",
  "coverageType": "Full Declared Value",
  "providerName": "DEMO Gemora Insurance Sandbox",
  "premiumAmount": 14500.00
}
```

**Authorization:** Admin only.

**Validation:**
- Coverage amount must be positive
- Premium must be positive
- Cannot exceed 200% of declared value

---

## Role Permissions

| Action | Seller | Buyer | Admin |
|--------|--------|-------|-------|
| Create shipment from own order | ✅ | ❌ | ❌ |
| View own shipments | ✅ | ✅ (purchases) | ✅ (all) |
| Generate shipping plan | ✅ (own) | ❌ | ✅ (all) |
| Approve shipping plan | ❌ | ❌ | ✅ |
| Book courier | ❌ | ❌ | ✅ |
| Create insurance record | ❌ | ❌ | ✅ |
| Add tracking events | ❌ | ❌ | ✅ |
| Mark as Delivered | ❌ | ❌ | ✅ |
| View tracking timeline | ✅ (own) | ✅ (own) | ✅ (all) |
| View insurance record | ✅ (own) | ✅ (own) | ✅ (all) |

---

## Shipment Status Flow

```
Pending
  ↓ (Plan Generated)
Planning
  ↓ (Plan Approved by Admin)
ReadyForBooking
  ↓ (Courier Booked)
Booked
  ↓ (Package Collected)
PickedUp
  ↓ (In Transit)
InTransit
  ↓ (Customs Hold - optional)
CustomsHold
  ↓ (Released from Customs)
InTransit
  ↓ (Out for Delivery)
OutForDelivery
  ↓ (Delivered / Failed / Exception)
Delivered / DeliveryFailed / Exception
```

**Exception Handling:**
- From any post-booking status → Exception
- Exception → InTransit (retry) or DeliveryFailed (final)

---

## Plan Approval Lifecycle

1. **Generation** (Seller triggers)
   - AI agent analyzes shipment risk
   - Generates recommendation with structured output
   - Validates output against schema
   - Falls back to deterministic rules if AI fails
   - Saves plan with `IsApproved = false`
   - Sets shipment status to `PlanGenerated`

2. **Review** (Admin)
   - Admin reviews plan details
   - Checks risk level, service type, coverage
   - Adds admin notes

3. **Approval** (Admin action)
   - Sets `IsApproved = true`
   - Records `ApprovedBy` (Admin ID) and `ApprovedAt`
   - Updates shipment status to `PlanApproved` or `ReadyForBooking`

4. **Regeneration** (Seller or Admin)
   - Creates new plan or updates existing
   - **Automatically invalidates approval:**
     - `IsApproved = false`
     - `ApprovedBy = null`
     - `ApprovedAt = null`
     - `AdminNotes = null`
   - Requires re-approval before booking

---

## Courier Simulation

### MockShippingProviderAdapter

**Configuration:**
- Max retry attempts: 3
- Timeout: 30 seconds
- Failure simulation: Configurable (default: false)

**Simulated Responses:**
```json
{
  "success": true,
  "trackingNumber": "SIM-TRK-{timestamp}",
  "externalReference": "SIM-BOOK-{timestamp}",
  "estimatedDelivery": "2026-10-10T00:00:00Z"
}
```

**Failure Handling:**
- Transient failures (network, timeout): Retry with exponential backoff
- Non-transient failures (invalid data): Throw immediately
- All retries exhausted: Return null, service handles gracefully

**Idempotency:**
- If shipment already has tracking number, return existing data
- No duplicate bookings created

---

## Insurance Simulation

### Demo Insurance Data

**Policy Format:**
- Policy Number: `SIM-POL-{sequence}`
- Policy Reference: `DEMO-GEMORA-INS-{sequence}`
- Provider: `DEMO Gemora Insurance Sandbox`

**Premium Calculation:**
- Standard risk: 1% of coverage amount
- High risk: 1.5% of coverage amount
- Critical risk: 2% of coverage amount

**Coverage Types:**
- Standard: 100% of declared value
- Premium: 110% of declared value
- Comprehensive: 120% of declared value

---

## Tracking & Exceptions

### Valid Event Types

| Event Type | Description | Status Update |
|------------|-------------|---------------|
| Booked | Courier booking confirmed | Booked |
| PickedUp | Package collected from seller | PickedUp |
| InTransit | Moving through network | InTransit |
| CustomsHold | Held at customs | CustomsHold |
| OutForDelivery | Final delivery attempt | OutForDelivery |
| Delivered | Successfully delivered | Delivered |
| DeliveryFailed | Delivery attempt failed | DeliveryFailed |
| Exception | Unexpected issue | Exception |

### External Event Codes

Format: `SIM-EVT-{TYPE}-{SEQUENCE}`

Examples:
- `SIM-EVT-BOOK-0001`
- `SIM-EVT-PICKUP-0001`
- `SIM-EVT-TRANSIT-0001`
- `SIM-EVT-EXCEPTION-0002`

---

## AI Agent Design (Phase 7)

### Architecture

```
┌─────────────────────────────────────┐
│   ShippingAgentService              │
│                                     │
│  1. Build trusted input from DB     │
│  2. Call ILlmProvider (if available)│
│  3. Validate AI output              │
│  4. If valid → use AI recommendation│
│  5. If invalid/null → fallback      │
│  6. Persist with generation source  │
│  7. Log execution summary           │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   ILlmProvider Interface            │
│   (abstraction layer)               │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   MockLlmProvider (development)     │
│   GeminiLlmProvider (production)    │
│   OpenAILlmProvider (alternative)   │
└─────────────────────────────────────┘
```

### Input Schema (Trusted Backend Data Only)

```csharp
public record LlmShippingPlanInput
{
    public Guid ShipmentId { get; init; }
    public decimal DeclaredValue { get; init; }
    public string Currency { get; init; }
    public string OriginCountryCode { get; init; }
    public string DestinationCountryCode { get; init; }
    public bool ExportRequired { get; init; }
    public decimal? PackageWeight { get; init; }
    public string? SpecialHandlingNotes { get; init; }
    public string? GemType { get; init; }
    public List<string> ApprovedServiceTypes { get; init; }
}
```

**Security:** Never includes client-supplied ownership or value data.

### Output Schema (Strict Validation)

```csharp
public record LlmShippingPlanRecommendation
{
    public string RiskLevel { get; init; } // Low, Medium, High, Critical
    public List<string> RiskReasons { get; init; }
    public string RecommendedServiceType { get; init; }
    public bool InsuranceRecommended { get; init; }
    public decimal? RecommendedCoverageAmount { get; init; }
    public List<string> HandlingRequirements { get; init; }
    public List<string> RequiredDocuments { get; init; }
    public List<string> Warnings { get; init; }
    public string GenerationSource { get; init; } // "AI"
    public string? ExecutionSummary { get; init; }
}
```

### Validation Rules

1. **RiskLevel**: Must be one of: Low, Medium, High, Critical
2. **RecommendedServiceType**: Non-empty string
3. **RecommendedCoverageAmount**:
   - Must be non-negative
   - Must not exceed 200% of declared value
4. **Lists**: Cannot be null (empty is acceptable)
5. **GenerationSource**: Must be "AI" or "FallbackRules"

### Fallback Mechanism

**Trigger Conditions:**
- LLM provider unavailable (null configuration)
- Timeout (30s per attempt)
- Network failure
- Malformed response
- Validation failure

**Fallback Behavior:**
- Uses deterministic rule-based assessment
- Calculates risk score from shipment attributes
- Maps to predefined service types
- Generates standard handling requirements
- Sets `GenerationSource = "FallbackRules"`

**Safety Guarantees:**
- AI cannot approve its own plans
- AI cannot book couriers
- AI cannot purchase insurance
- AI cannot mark shipments as delivered
- Admin approval always required regardless of source

### Configuration

**User Secrets (Development):**
```json
{
  "LlmProvider:ApiKey": "your-api-key-here",
  "LlmProvider:Model": "gemini-pro",
  "LlmProvider:TimeoutSeconds": 30,
  "LlmProvider:MaxRetries": 3
}
```

**Production:** Use environment variables or Azure Key Vault.

---

## Component Dependencies

### Component 2 (Order Management)

**Dependency:** Component 3 requires paid orders from Component 2.

**Integration Points:**
- `GET /api/Orders/my-shipment-eligible` returns Component 2 orders with `Status = "Paid"` and no active shipments
- Shipment creation validates order ownership and payment status
- Order `PaidAt` timestamp used for demo seeding

**Demo Data:** DbSeeder creates 6 paid orders representing Component 2 purchases.

### Component 4 (Export Clearance)

**Dependency:** Component 3 references export clearance but does not implement it.

**Current State:**
- `ExportRequired` flag on shipments indicates need for export clearance
- Required documents list includes export certificates
- No actual integration with Component 4 systems yet

**Future Integration:**
- When Component 4 is ready, add export clearance status check before booking
- Integrate with Component 4 API for clearance verification

---

## Development Demo Data

### Enabling Demo Seeding

**User Secrets Configuration:**
```bash
dotnet user-secrets set "SeedUsers:EnableShippingDemoReset" "true"
```

**Default Behavior:** Demo seeding is **disabled** by default to prevent blocking login during development.

### Demo Scenarios

| Scenario | Purpose | Order | Shipment | Status |
|----------|---------|-------|----------|--------|
| A | Fresh paid order | ✓ Order A (850K LKR) | ✗ None | N/A |
| B | Plan pending admin | ✓ Order B (620K LKR) | ✓ Shipment B | Planning |
| C | Approved ready booking | ✓ Order C (1.1M LKR) | ✓ Shipment C | ReadyForBooking |
| D | Booked + insured + transit | ✓ Order D (1.45M LKR) | ✓ Shipment D | InTransit |
| E | Delivery exception | ✓ Order E (1.75M LKR) | ✓ Shipment E | Exception |
| F | Completed delivery | ✓ Order F (480K LKR) | ✓ Shipment F | Delivered |

### Idempotency

The seeder handles Program.cs table drop/recreate asymmetry:
- Orders table uses `CREATE TABLE IF NOT EXISTS` (preserves data)
- Shipments table uses `DROP TABLE IF EXISTS CASCADE` (wipes data)
- Seeder detects this state and reuses existing orders while reseeding shipments
- Running API multiple times does not duplicate data

### Simulated External References

- **Courier:** `DEMO Gemora Courier Sandbox`
- **Booking refs:** `SIM-BOOK-0001`, `SIM-BOOK-0002`, `SIM-BOOK-0003`
- **Tracking refs:** `SIM-TRK-0001`, `SIM-TRK-0002`, `SIM-TRK-0003`
- **Insurance provider:** `DEMO Gemora Insurance Sandbox`
- **Policy refs:** `SIM-POL-0001`, `SIM-POL-0002`, `SIM-POL-0003`
- **Event codes:** `SIM-EVT-BOOK-*`, `SIM-EVT-PICKUP-*`, `SIM-EVT-TRANSIT-*`, etc.

---

## Setup & Running

### Backend

**Prerequisites:**
- .NET 8.0 SDK
- Supabase PostgreSQL account (or local PostgreSQL)

**User Secrets Setup:**
```bash
cd Backend/Gemora.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..."
dotnet user-secrets set "Jwt:Key" "your-development-secret-key-min-32-chars"
dotnet user-secrets set "Jwt:Issuer" "GemoraAPI"
dotnet user-secrets set "Jwt:Audience" "GemoraUsers"
dotnet user-secrets set "SeedUsers:AdminPassword" "YourAdminPassword"
dotnet user-secrets set "SeedUsers:GemologistPassword" "YourGemologistPassword"
dotnet user-secrets set "SeedUsers:ExportOfficerPassword" "YourExportOfficerPassword"
dotnet user-secrets set "SeedUsers:EnableShippingDemoReset" "true"  # Optional
```

**⚠️ Security Warning:** Never commit User Secrets to version control. Use environment variables in production.

**Build:**
```bash
cd Backend
dotnet build
```

**Run:**
```bash
cd Backend/Gemora.API
dotnet run
```

API will start on `http://localhost:5198` (check console output for actual port).

**Test:**
```bash
cd Backend
dotnet test
```

### React Frontend

**Prerequisites:**
- Node.js 18+
- npm or yarn

**Install Dependencies:**
```bash
cd Web
npm install
```

**Run Development Server:**
```bash
npm run dev
```

React app will start on `http://localhost:5173`.

**Build for Production:**
```bash
npm run build
```

Output in `Web/dist/`.

### Flutter Mobile

**⚠️ Limitation:** Flutter is **NOT installed** on this development machine.

**Expected Setup (when Flutter is available):**
```bash
cd Mobile
flutter pub get
flutter run  # Requires Android/iOS emulator or device
```

**Current State:**
- Flutter source code exists in `Mobile/lib/`
- Shipment service integration completed in Phase 5
- Screens implemented: CreateShipment, ShipmentList, ShipmentDetail
- Cannot verify build or runtime behavior without Flutter SDK

**Documentation Accuracy:** This document acknowledges that Flutter testing was not performed due to missing SDK installation.

---

## Troubleshooting

### Common Issues

**1. Database Connection Failed**
- Verify Supabase credentials in User Secrets
- Check network connectivity to Supabase
- Ensure SSL Mode is set to Require

**2. Demo Data Not Seeded**
- Check `SeedUsers:EnableShippingDemoReset` is set to "true"
- Review console logs for seeding messages
- Verify users and listings were seeded first

**3. Authorization Errors (401/403)**
- Ensure JWT token is included in Authorization header
- Verify user role matches endpoint requirements
- Check token expiration (default: 7 days)

**4. Plan Approval Not Working**
- Verify user role is Admin
- Check plan exists and is not already approved
- Review Admin ID matches token subject

**5. Booking Fails**
- Confirm plan is approved
- Verify shipment status is ReadyForBooking
- Check not already booked (has tracking number)

**6. AI Agent Not Using LLM**
- Verify `ILlmProvider` is registered in DI
- Check User Secrets for LLM configuration
- Review logs for fallback messages

---

## Testing Strategy

### Unit Tests
- Service layer logic (ShipmentService, ShippingAgentService)
- Validation rules (coverage amounts, status transitions)
- Authorization checks

### Integration Tests
- End-to-end shipment creation flow
- Plan generation and approval lifecycle
- Courier booking with mock provider
- Insurance record creation
- Tracking event addition

### API Verification
- Test all endpoints with Postman/curl
- Verify role-based access control
- Test error responses and edge cases

---

## Future Enhancements

1. **Real LLM Integration**: Replace MockLlmProvider with Gemini/OpenAI
2. **Component 4 Integration**: Connect export clearance workflow
3. **Real Courier APIs**: Integrate DHL/FedEx/UPS adapters
4. **Real Insurance Providers**: Connect to insurance company APIs
5. **Push Notifications**: Alert users on status changes
6. **PDF Generation**: Generate shipping labels and insurance certificates
7. **Multi-language Support**: Localize tracking descriptions
8. **Analytics Dashboard**: Track shipment metrics and performance

---

## Version History

- **v1.0** (2026-10-04): Initial Component 3 implementation
  - Phases 1-6: Core shipping workflow
  - Phase 7: AI-powered planning agent
  - Phase 8: Demo data repair and idempotency
  - Phase 9: Documentation and verification
