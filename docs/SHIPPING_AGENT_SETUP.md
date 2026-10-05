# Real shipping agent

The API now registers `GeminiShippingAgentProvider`, not `MockLlmProvider`.
The existing `Gemini:ApiKey` and `Gemini:Model` settings are reused. No key is shipped in source.

## Configure locally

In Visual Studio, right-click Gemora.API -> Manage User Secrets. Add these settings alongside your existing secrets:

```json
{
  "Gemini:ApiKey": "YOUR_LOCAL_API_KEY",
  "Gemini:Model": "gemini-3.8-flash"
}
```

Use a supported function-calling and structured-output model available to your Gemini account.
Deployment uses `Gemini__ApiKey` and `Gemini__Model` environment variables.
Restart the API after configuring it. Do not share or commit the key.

## Execution

1. Seller/Admin starts Analyze Risk for an authorized, unbooked shipment.
2. Backend loads the order and gem; declared value and currency must match the order.
3. Gemini selects read-only tools. The backend validates names/arguments and supplies shipment context, eligible services and versioned rules.
4. Gemini synthesizes a schema-constrained recommendation using the tool results.
5. Backend independently validates the risk enum, service eligibility, coverage bounds and text limits.
6. The plan and a concise execution record are saved. Regeneration resets approval. Admin approval remains separate from booking.

The agent has no booking, insurance purchase, approval, filesystem or arbitrary network tool.
Notes and listing titles are untrusted data; tool scope cannot be changed by model arguments.
There are at most four tool rounds plus one synthesis round, bounded retries, and a 45-second deadline.

## Evidence in the UI

A successful live generation has `generationSource: AI` and an execution summary containing provider `Gemini`, configured model, a unique run ID, model request count, executed tool names, completion timestamp and validation result.
The metadata is created by the backend, not supplied by the model. Hidden reasoning and keys are never persisted.
Missing credentials, provider failure, timeout or invalid output yields `FallbackRules` with a failure explanation. That is not a successful AI run.
Old seeded plans remain old data until Analyze Risk runs successfully.

## Validation

```powershell
dotnet run --project Backend/Gemora.ShippingAgent.Checks/Gemora.ShippingAgent.Checks.csproj
```

These offline transport checks verify orchestration, tool isolation, schema/business validation and cancellation. They do not establish a live model call.
For live validation configure credentials, restart the API, open an unbooked shipment and click Analyze Risk. Confirm the UI reports `AI`, `Gemini`, tool execution and a new run ID. No courier booking or insurance purchase is performed by analysis.

Provider contracts: https://ai.google.dev/gemini-api/docs/function-calling and https://ai.google.dev/gemini-api/docs/structured-output.
