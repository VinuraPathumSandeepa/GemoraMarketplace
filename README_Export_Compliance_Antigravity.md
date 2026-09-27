# Gemora Marketplace
## Component 4 — Export & Compliance
### Implementation Plan for Antigravity

> **Important:** Extend the existing Gemora repository. Do not redesign the shared architecture, replace authentication, create duplicate shared entities, or modify unrelated components.

---

# 1. Objective

Implement the complete **Export & Compliance** component inside the existing Gemora Marketplace repository.

This component handles:

- export request creation
- export-related supporting documents
- compliance checking
- missing requirement detection
- AI-assisted compliance analysis
- Export Officer review
- approve / reject / request revision workflow
- export status/history
- security and auditability

The final export/compliance decision must remain with an authorized **Export Officer**.

The AI may assist the review, but it must never autonomously approve an export request.

---

# 2. Existing Project Architecture — DO NOT REPLACE

The repository already contains:

```text
Backend/
├── Gemora.Domain/
├── Gemora.Application/
├── Gemora.Infrastructure/
└── Gemora.API/

Web/
Mobile/
```

The current backend follows:

```text
Controller
    ↓
Application Service
    ↓
ApplicationDbContext
    ↓
Entity Framework Core
    ↓
PostgreSQL
```

Do **not** introduce a repository layer unless the shared project architecture is changed by the team.

Do not recreate or replace:

- authentication
- JWT configuration
- `User`
- `UserRoles`
- `ApplicationDbContext`
- Swagger
- CORS
- global exception middleware
- database seeding
- existing React project
- existing Flutter project

Extend the existing system only.

---

# 3. Existing Security Architecture

The project already provides:

```text
JWT Authentication
Role-Based Authorization
BCrypt Password Hashing
Admin Role
Buyer Role
Seller Role
Gemologist Role
ExportOfficer Role
```

The JWT includes:

```text
ClaimTypes.NameIdentifier → User.Id
ClaimTypes.Name           → FullName
ClaimTypes.Email          → Email
ClaimTypes.Role           → Role
```

Use `ClaimTypes.NameIdentifier` to identify the currently authenticated user.

Never accept trusted ownership information such as:

```text
RequestedByUserId
ReviewedByUserId
```

from the client.

These values must be derived by the backend.

---

# 4. Existing Export Officer

The shared system already contains:

```text
Role:
ExportOfficer
```

and a seeded development Export Officer account.

Do not create another Export Officer role.

Officer-only endpoints should use:

```csharp
[Authorize(Roles = UserRoles.ExportOfficer)]
```

---

# 5. Important Dependency Rule

Component 2 — Marketplace & Transactions — has not yet added the final shared `Order` entity to this branch.

Therefore:

**Do not create a duplicate `Order` entity inside Component 4.**

Initial Export & Compliance development must remain independent.

When Component 2's final `Order` model is merged later, the relationship can be added in a separate integration step:

```text
ExportRequest
    ↓
OrderId
    ↓
Order
```

This avoids cross-component merge conflicts.

---

# 6. Phase 1 — Domain Foundation

Create:

```text
Backend/Gemora.Domain/Enums/
├── ExportRequestStatus.cs
└── ComplianceDocumentStatus.cs
```

Create:

```text
Backend/Gemora.Domain/Entities/
├── ExportRequest.cs
└── ComplianceDocument.cs
```

## ExportRequestStatus

Recommended states:

```text
Draft
Submitted
UnderComplianceReview
UnderOfficerReview
RevisionRequired
Approved
Rejected
Cancelled
```

Expected workflow:

```text
Draft
  ↓
Submitted
  ↓
UnderComplianceReview
  ↓
UnderOfficerReview
  ↓
 ┌────────────┬────────────┬──────────────────┐
 ↓            ↓            ↓
Approved    Rejected    RevisionRequired
                           ↓
                       Resubmitted
```

Protected states such as:

```text
Approved
Rejected
UnderOfficerReview
```

must not be directly controlled by normal clients.

## ComplianceDocumentStatus

Recommended states:

```text
Pending
Valid
Invalid
Expired
RequiresReview
```

---

# 7. Phase 2 — ExportRequest Entity

Create an `ExportRequest` entity using `Guid` IDs to match the existing system.

Initial fields should include:

```text
Id
RequestedByUserId
OriginCountry
DestinationCountry
DeclaredValue
Currency
Purpose
Status
ReviewedByUserId
ReviewNotes
SubmittedAt
ReviewedAt
CreatedAt
UpdatedAt
```

Relationships:

```text
RequestedByUserId
        ↓
      User

ReviewedByUserId
        ↓
      User
```

One Export Request can contain many Compliance Documents:

```text
ExportRequest
     1
     │
     │
     *
ComplianceDocument
```

Do not add `OrderId` yet.

---

# 8. Phase 3 — ComplianceDocument Entity

Initial fields:

```text
Id
ExportRequestId
UploadedByUserId
DocumentType
DocumentNumber
Issuer
IssueDate
ExpiryDate
FileUrl
Status
UploadedAt
```

Relationships:

```text
ComplianceDocument
       ↓
ExportRequest

ComplianceDocument
       ↓
UploadedBy User
```

Actual binary file upload can be implemented in a later phase.

Initially support metadata and file references safely.

---

# 9. Phase 4 — EF Core Configuration

Extend:

```text
Backend/Gemora.Infrastructure/Data/ApplicationDbContext.cs
```

Add:

```text
DbSet<ExportRequest>
DbSet<ComplianceDocument>
```

Configure using Fluent API because the existing project follows Fluent API configuration.

Configure:

- primary keys
- foreign keys
- maximum lengths
- decimal precision
- required fields
- indexes
- delete behavior

Recommended indexes:

```text
ExportRequests(RequestedByUserId)
ExportRequests(Status)
ExportRequests(DestinationCountry)

ComplianceDocuments(ExportRequestId)
ComplianceDocuments(DocumentType)
```

Use:

```text
DeleteBehavior.Restrict
```

for User references where accidental user deletion must not cascade into historical compliance records.

Use cascading deletion only where appropriate between:

```text
ExportRequest
    ↓
ComplianceDocuments
```

---

# 10. Phase 5 — First Migration

Create:

```text
AddExportComplianceFoundation
```

Expected tables:

```text
ExportRequests
ComplianceDocuments
```

Do not modify the shared `Users` entity except for foreign-key relationships from the new tables.

Do not create:

```text
Orders
Shipments
GemListings
```

inside this migration.

After creating the migration:

1. inspect the generated migration
2. build the project
3. update the database
4. confirm tables in PostgreSQL
5. confirm existing authentication still works

---

# 11. Phase 6 — DTO Layer

Create:

```text
Backend/Gemora.Application/DTOs/ExportCompliance/
```

Suggested DTOs:

```text
CreateExportRequestDto
UpdateExportRequestDto
ExportRequestResponseDto
ExportRequestDetailsDto
SubmitExportRequestDto
CreateComplianceDocumentDto
ComplianceDocumentResponseDto
ExportDecisionDto
ComplianceAssessmentResponseDto
```

Follow the existing validation pattern using:

```text
[Required]
[StringLength]
[Range]
```

Provide clear user-facing validation messages.

Do not include trusted fields in normal user DTOs:

```text
RequestedByUserId
ReviewedByUserId
Status = Approved
Status = Rejected
```

---

# 12. Phase 7 — Service Interface

Create:

```text
Backend/Gemora.Application/Interfaces/IExportComplianceService.cs
```

Suggested operations:

```text
CreateExportRequestAsync
GetMyExportRequestsAsync
GetExportRequestByIdAsync
UpdateDraftExportRequestAsync
SubmitExportRequestAsync
AddComplianceDocumentAsync
GetComplianceDocumentsAsync
GetPendingOfficerRequestsAsync
GetOfficerRequestDetailsAsync
ReviewExportRequestAsync
```

Use async EF Core operations.

---

# 13. Phase 8 — ExportComplianceService

Create:

```text
Backend/Gemora.Application/Services/ExportComplianceService.cs
```

Inject:

```text
ApplicationDbContext
```

following the existing `AuthService` architecture.

Responsibilities:

## Request creation

Validate:

```text
OriginCountry
DestinationCountry
DeclaredValue
Currency
Purpose
```

Set:

```text
RequestedByUserId = authenticated user ID
Status = Draft
CreatedAt = UtcNow
UpdatedAt = UtcNow
```

## Ownership

The backend must verify:

```text
ExportRequest.RequestedByUserId == authenticated user ID
```

before allowing normal users to modify or submit a request.

## Submission

Allow:

```text
Draft → Submitted
RevisionRequired → Submitted
```

Do not allow invalid transitions such as:

```text
Approved → Submitted
Rejected → Submitted
```

unless business requirements explicitly change later.

---

# 14. Phase 9 — Export Request Controller

Create:

```text
Backend/Gemora.API/Controllers/ExportRequestsController.cs
```

Use:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
```

Read authenticated user ID using:

```csharp
User.FindFirstValue(ClaimTypes.NameIdentifier)
```

Initial APIs:

```http
POST /api/ExportRequests
GET  /api/ExportRequests/my
GET  /api/ExportRequests/{id}
PUT  /api/ExportRequests/{id}
POST /api/ExportRequests/{id}/submit
```

Later add:

```http
POST /api/ExportRequests/{id}/documents
GET  /api/ExportRequests/{id}/documents
```

---

# 15. Phase 10 — Export Officer API

Add officer-only endpoints.

Example:

```http
GET /api/ExportRequests/officer/pending
GET /api/ExportRequests/officer/{id}
POST /api/ExportRequests/officer/{id}/decision
```

Protect with:

```csharp
[Authorize(Roles = UserRoles.ExportOfficer)]
```

Supported decisions:

```text
Approve
Reject
RequestRevision
```

The authenticated Export Officer ID must be saved as:

```text
ReviewedByUserId
```

Store:

```text
ReviewNotes
ReviewedAt
```

---

# 16. Phase 11 — Deterministic Compliance Rules

Before AI integration, implement normal backend compliance checks.

Examples:

```text
Destination country present?
Declared value > 0?
Currency present?
Required documents uploaded?
Document expired?
Required information missing?
Request already approved/rejected?
Request owner valid?
```

Create a service such as:

```text
IComplianceRulesService
ComplianceRulesService
```

Return a structured result:

```json
{
  "isComplete": false,
  "missingRequirements": [],
  "warnings": [],
  "invalidDocuments": []
}
```

Do not rely on AI for deterministic checks.

---

# 17. Phase 12 — Compliance & Requirements AI Agent

Implement a distinct:

```text
Compliance & Requirements Agent
```

Purpose:

```text
Analyze export/compliance context
Identify missing requirements
Highlight risks or inconsistencies
Prepare a review package
Recommend officer checks
```

The agent must not make the final approval decision.

## Allowed information

The AI may receive:

```text
Export request data
Document metadata
Gem/transaction summary when available
Destination information
Deterministic rules result
```

Do not send:

```text
Passwords
JWT tokens
API secrets
Unnecessary private user information
Hidden reasoning
```

## Structured output

Example:

```json
{
  "summary": "The request requires additional documentation.",
  "requirements": [
    {
      "name": "Certificate",
      "status": "met"
    }
  ],
  "missingRequirements": [],
  "warnings": [],
  "recommendedOfficerChecks": []
}
```

Validate AI output before storing or displaying it.

---

# 18. Phase 13 — Mandatory Human Approval Gate

Final flow:

```text
User submits Export Request
        ↓
Deterministic Rules
        ↓
Compliance Agent
        ↓
Structured Assessment
        ↓
Export Officer Review
        ↓
 ┌───────────┬──────────┬──────────────────┐
 ↓           ↓          ↓
Approve    Reject    Request Revision
```

The AI must never directly write `Approved` as the final business state.

Only an authorized:

```text
ExportOfficer
```

may perform the final decision operation.

---

# 19. Phase 14 — Compliance Documents

Implement actual supporting-document handling.

Features:

```text
upload document
document metadata
view documents
remove/replace where allowed
expiry validation
document status
```

Security:

```text
validate extension
validate MIME type
validate size
generate server-side filename
do not trust original filename
authorize access
```

If local storage is used initially, wrap storage operations in a service so cloud storage can replace it later.

---

# 20. Phase 15 — Audit History

Add auditability for:

```text
request created
request updated
request submitted
document uploaded
document removed
compliance check executed
AI assessment executed
officer review performed
approved
rejected
revision requested
```

Use a shared `AuditLog` entity if another component introduces one before this phase.

If not, create a Component 4-specific status/decision history only after coordinating with the team to avoid duplicate shared audit models.

---

# 21. Phase 16 — React Export Officer UI

Extend the existing React project.

Create Export Officer screens:

```text
Export Compliance Dashboard
Pending Requests
Export Request Details
Document Review Panel
Compliance Rules Result
AI Assessment Panel
Approve
Reject
Request Revision
Decision History
```

The UI should clearly separate:

```text
Deterministic Check
AI Assessment
Human Decision
```

Do not visually present AI output as a final official decision.

Role-aware navigation:

```text
ExportOfficer → Compliance Dashboard
```

---

# 22. Phase 17 — Flutter Export UI

Extend the existing Flutter application.

User-facing screens:

```text
My Export Requests
Create Export Request
Edit Draft Request
Upload Documents
Export Request Details
Missing Requirements
Revision Feedback
Export Status
```

Do not expose officer actions in normal Flutter user flows.

---

# 23. Phase 18 — Security Tests

Test at minimum:

```text
Unauthenticated user → 401

Normal user accessing Export Officer endpoint → 403

Export Officer → allowed

User A cannot modify User B request

Client cannot assign RequestedByUserId

Client cannot assign ReviewedByUserId

Normal user cannot set Approved

Normal user cannot set Rejected

AI cannot directly approve request

Officer identity recorded automatically

Approved request protected from unsafe modification

Invalid document rejected

Expired document detected
```

---

# 24. Phase 19 — Functional Tests

Backend tests:

```text
Create export request
Get own requests
Get request details
Update draft
Submit request
Revision resubmit
Document add
Compliance validation
Officer queue
Approve
Reject
Request revision
```

AI tests:

```text
complete request
missing document
expired document
contradictory metadata
malformed AI output
timeout
prompt injection
```

---

# 25. Phase 20 — Git Strategy

Work only on:

```text
dev-Madhuka-ExportCompliance
```

Use small meaningful commits.

Recommended commit progression:

```text
feat(export): add export compliance domain model

feat(export): add export compliance database migration

feat(export): implement export request API

feat(export): add compliance document workflow

feat(export): add deterministic compliance validation

feat(export): implement export officer review workflow

feat(export-ai): add compliance requirements agent

feat(export-web): add export officer dashboard

feat(export-mobile): add export request workflow

test(export): add export compliance tests

docs(export): document export compliance component
```

Do not make one huge final commit.

---

# 26. Implementation Order for Antigravity

Antigravity must implement this component in the following order:

```text
PHASE 1
Inspect existing repository
        ↓
PHASE 2
Domain enums/entities
        ↓
PHASE 3
DbContext configuration
        ↓
PHASE 4
Migration + database verification
        ↓
PHASE 5
DTOs
        ↓
PHASE 6
Service interface
        ↓
PHASE 7
ExportComplianceService
        ↓
PHASE 8
Basic Export Request APIs
        ↓
PHASE 9
Ownership + authorization
        ↓
PHASE 10
Document workflow
        ↓
PHASE 11
Deterministic compliance rules
        ↓
PHASE 12
Export Officer review APIs
        ↓
PHASE 13
Compliance AI Agent
        ↓
PHASE 14
Human approval gate
        ↓
PHASE 15
React Export Officer UI
        ↓
PHASE 16
Flutter user UI
        ↓
PHASE 17
Tests
        ↓
PHASE 18
Audit + polish
        ↓
PHASE 19
Final integration
```

---

# 27. Antigravity Safety Instructions

Before modifying any file:

1. Inspect the existing implementation.
2. Reuse existing architecture.
3. Do not rename existing projects.
4. Do not replace authentication.
5. Do not replace `ApplicationDbContext`.
6. Do not change existing JWT behavior.
7. Do not create a new User model.
8. Do not create another ExportOfficer role.
9. Do not create an Order entity.
10. Do not modify unrelated components.
11. Keep the existing project buildable after every phase.
12. Never put credentials/API keys in source files.
13. Never trust ownership IDs supplied by frontend.
14. Never let AI directly approve export requests.
15. Preserve existing public authentication behavior.
16. Follow existing file-scoped namespace style.
17. Follow existing `Guid` ID conventions.
18. Follow existing direct `ApplicationDbContext` service pattern.
19. Follow existing DataAnnotations validation style for DTOs.
20. Follow existing role authorization constants from `UserRoles`.

---

# 28. Definition of Done

Component 4 is complete when:

- authenticated users can securely create export requests
- users can only manage their own export requests
- supporting compliance documents are handled securely
- deterministic rules identify missing/invalid requirements
- Compliance Agent creates structured assistance
- Export Officer receives a dedicated review workflow
- only Export Officer can approve/reject/request revision
- officer identity and timestamps are stored
- React provides officer-facing workflows
- Flutter provides user-facing export workflows
- backend authorization is enforced
- audit/history exists
- tests cover successful and malicious/invalid cases
- existing authentication remains working
- PostgreSQL migrations work
- React builds
- Flutter analyzes/builds
- backend builds and runs successfully
- Git history clearly shows Madhuka's contribution

---

# 29. First Antigravity Task

Do **not** implement the whole component in one run.

The first task should only be:

```text
1. Inspect existing Gemora architecture.
2. Create ExportRequestStatus.
3. Create ComplianceDocumentStatus.
4. Create ExportRequest.
5. Create ComplianceDocument.
6. Extend ApplicationDbContext.
7. Create AddExportComplianceFoundation migration.
8. Build the solution.
9. Apply the migration.
10. Do not create APIs, AI, React, or Flutter yet.
```

After this first phase is verified, continue to the API phase.

---

# 30. Current Repository Facts

The implementation must respect these confirmed facts:

- `User.Id` is a `Guid`.
- `UserRoles.ExportOfficer` already exists.
- JWT stores `User.Id` using `ClaimTypes.NameIdentifier`.
- The Export Officer development user is already seeded by the shared backend.
- Application services currently use `ApplicationDbContext` directly.
- The project currently does not use a repository layer.
- DTO validation currently uses DataAnnotations.
- Controllers use `[ApiController]`, `[Route("api/[controller]")]`, and `[Authorize]`.
- EF Core entity configuration is kept inside `ApplicationDbContext.OnModelCreating`.
- Component 2 has not yet provided the final shared `Order` entity on this branch.
