using System.Text.Json;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Gemora.Infrastructure.Data;
using Gemora.Application.Services;

// Read credentials without printing them. Diagnostics are read-only by default.
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
var adminPassword = Environment.GetEnvironmentVariable("SeedUsers__AdminPassword");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var secretsPath = args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Microsoft", "UserSecrets", "aa613f71-8308-4457-9a10-625ab30cb1ae", "secrets.json");
    using var secrets = JsonDocument.Parse(await File.ReadAllTextAsync(secretsPath));
    connectionString = secrets.RootElement.GetProperty("ConnectionStrings:DefaultConnection").GetString();
    if (secrets.RootElement.TryGetProperty("SeedUsers:AdminPassword", out var configuredPassword))
        adminPassword ??= configuredPassword.GetString();
}
var settings = new NpgsqlConnectionStringBuilder(connectionString);
var diagnosticPort = Environment.GetEnvironmentVariable("GEMORA_DATABASE_CHECK_PORT");
if (!string.IsNullOrWhiteSpace(diagnosticPort))
    settings.Port = int.Parse(diagnosticPort);
settings.MaxAutoPrepare = 0;
settings.Multiplexing = false;
settings.Pooling = false;
settings.Timeout = 10;
settings.CommandTimeout = 10;
settings.ApplicationName = "Gemora.Database.Checks";
try
{
    if (args.Length >= 3 && args[1] == "--preview-shipment")
    {
        Console.Error.WriteLine("Shipping preview is unavailable: the current shipping service does not expose a read-only preview operation.");
        Environment.ExitCode = 1;
        return;
    }
    await using var connection = new NpgsqlConnection(settings.ConnectionString);
    await connection.OpenAsync();
    Console.WriteLine("Database connection succeeded.");
    if (args.Contains("--reset-admin-password"))
    {
        if (string.IsNullOrWhiteSpace(adminPassword))
            throw new InvalidOperationException("SeedUsers:AdminPassword is not configured.");
        await using var reset = new NpgsqlCommand("""
            UPDATE "Users" SET "PasswordHash" = @hash
            WHERE "Email" = @email AND "Role" = 'Admin'
            """, connection);
        reset.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(adminPassword));
        reset.Parameters.AddWithValue("email", "admin@gemora.com");
        var updated = await reset.ExecuteNonQueryAsync();
        if (updated != 1)
            throw new InvalidOperationException($"Expected one admin account; updated {updated}.");
        await using var verify = new NpgsqlCommand(
            "SELECT \"PasswordHash\" FROM \"Users\" WHERE \"Email\" = @email AND \"Role\" = 'Admin'", connection);
        verify.Parameters.AddWithValue("email", "admin@gemora.com");
        var storedHash = (string?)await verify.ExecuteScalarAsync();
        if (storedHash == null || !BCrypt.Net.BCrypt.Verify(adminPassword, storedHash))
            throw new InvalidOperationException("Admin password verification failed.");
        Console.WriteLine("Admin password updated and verified.");
        return;
    }
    if (Environment.GetEnvironmentVariable("GEMORA_DATABASE_CHECK_CONNECTION_ONLY") == "true")
        return;
    if (args.Length >= 3 && args[1] == "--align-pending-shipment")
    {
        // Explicit, scoped repair for a legacy shipment. Never modify a planned,
        // insured, booked, or unpaid shipment. The update and audit are atomic.
        await using var repair = new NpgsqlCommand("""
            WITH eligible AS (
                SELECT s."Id", s."DeclaredValue" AS old_value, s."Currency" AS old_currency,
                       o."TotalAmount" AS new_value, o."Currency" AS new_currency
                FROM "Shipments" s JOIN "Orders" o ON o."Id" = s."OrderId"
                WHERE s."Id" = @id AND s."Status" = 'Pending'
                  AND s."TrackingNumber" IS NULL AND o."Status" = 'Paid'
                  AND o."TotalAmount" > 0 AND length(trim(o."Currency")) > 0
                  AND s."SellerId" = o."SellerId" AND s."BuyerId" = o."BuyerId"
                  AND NOT EXISTS (SELECT 1 FROM "ShippingPlans" p WHERE p."ShipmentId" = s."Id")
                  AND NOT EXISTS (SELECT 1 FROM "InsuranceRecords" i WHERE i."ShipmentId" = s."Id")
                  AND (s."DeclaredValue" <> o."TotalAmount" OR s."Currency" <> o."Currency")
                FOR UPDATE OF s
            ), corrected AS (
                UPDATE "Shipments" s
                SET "DeclaredValue" = e.new_value, "Currency" = e.new_currency, "UpdatedAt" = NOW()
                FROM eligible e WHERE s."Id" = e."Id"
                RETURNING s."Id", e.old_value, e.old_currency, e.new_value, e.new_currency
            ), audited AS (
                INSERT INTO "ShipmentTrackingEvents"
                    ("Id", "ShipmentId", "EventType", "Location", "Description",
                     "PerformedByRole", "Reason", "OccurredAt", "RecordedAt")
                SELECT @event_id, "Id", 'OrderValueCorrected', 'Shipping administration',
                       'Legacy shipment value corrected from ' || old_currency || ' ' || old_value ||
                       ' to paid order total ' || new_currency || ' ' || new_value,
                       'System', 'Corrected order/shipment mismatch before risk analysis', NOW(), NOW()
                FROM corrected RETURNING "ShipmentId"
            )
            SELECT c.new_value, c.new_currency FROM corrected c JOIN audited a ON a."ShipmentId" = c."Id"
            """, connection);
        repair.Parameters.AddWithValue("id", Guid.Parse(args[2]));
        repair.Parameters.AddWithValue("event_id", Guid.NewGuid());
        await using var result = await repair.ExecuteReaderAsync();
        if (!await result.ReadAsync())
            throw new InvalidOperationException("No eligible mismatched pending shipment was changed.");
        Console.WriteLine($"Shipment corrected and audited: {result.GetDecimal(0)} {result.GetString(1)}");
        return;
    }
    if (args.Length >= 3 && args[1] == "--shipment")
    {
        await using var shipmentCommand = new NpgsqlCommand("""
            SELECT s."Status", s."DeclaredValue", s."Currency", o."TotalAmount", o."Currency", o."Status"
            FROM "Shipments" s JOIN "Orders" o ON o."Id" = s."OrderId"
            WHERE s."Id" = @id
            """, connection);
        shipmentCommand.Parameters.AddWithValue("id", Guid.Parse(args[2]));
        await using var shipmentReader = await shipmentCommand.ExecuteReaderAsync();
        if (await shipmentReader.ReadAsync())
            Console.WriteLine($"Shipment: {shipmentReader.GetString(0)}, {shipmentReader.GetDecimal(1)} {shipmentReader.GetString(2)}; Order: {shipmentReader.GetDecimal(3)} {shipmentReader.GetString(4)}, {shipmentReader.GetString(5)}");
        else
            throw new InvalidOperationException("Shipment not found.");
        return;
    }
    await using var command = new NpgsqlCommand("""
        SELECT pid, COALESCE(application_name, ''), COALESCE(state, ''),
               COALESCE(wait_event_type, ''), COALESCE(wait_event, ''),
               pg_blocking_pids(pid)::text
        FROM pg_stat_activity
        WHERE datname = current_database() AND pid <> pg_backend_pid()
        ORDER BY pid
        """, connection);
    await using var reader = await command.ExecuteReaderAsync();
    var count = 0;
    while (await reader.ReadAsync())
    {
        count++;
        Console.WriteLine($"PID={reader.GetInt32(0)} app={reader.GetString(1)} state={reader.GetString(2)} wait={reader.GetString(3)}/{reader.GetString(4)} blockers={reader.GetString(5)}");
    }
    Console.WriteLine($"Database sessions inspected: {count}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Database check failed: {ex.GetType().Name}");
    for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        Console.Error.WriteLine($"Cause: {inner.GetType().Name}");
    if (ex is PostgresException postgres)
        Console.Error.WriteLine($"SQLSTATE={postgres.SqlState}; {postgres.MessageText}");
    Environment.ExitCode = 1;
}
