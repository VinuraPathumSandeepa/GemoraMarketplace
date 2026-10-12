using Gemora.Infrastructure.Data;
using Npgsql;

namespace Gemora.Tests;

public class PostgreSqlSchemaFactAttribute : FactAttribute
{
    public PostgreSqlSchemaFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMORA_SCHEMA_TEST_CONNECTION")))
            Skip = "Set GEMORA_SCHEMA_TEST_CONNECTION to run isolated PostgreSQL temporary-table tests.";
    }
}

public class OrderApprovalSchemaTests
{
    [PostgreSqlSchemaFact]
    public async Task LegacyDuplicatesArePreserved_NewDuplicatesFail_AndUpgradeIsIdempotent()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("GEMORA_SCHEMA_TEST_CONNECTION"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        async Task Execute(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteNonQueryAsync();
        async Task<long> Count(string sql) => Convert.ToInt64(await new NpgsqlCommand(sql, connection, transaction).ExecuteScalarAsync());

        // Every schema/data statement resolves only to this session's temporary table.
        // No persistent application table is read or changed. Disposal rolls everything back.
        await Execute("""
            SET LOCAL search_path = pg_temp;
            CREATE TEMP TABLE "Orders" (
                "Id" uuid PRIMARY KEY, "GemListingId" integer, "BuyerId" uuid,
                "Status" text NOT NULL, "CreatedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
                "PaidAt" timestamptz NULL
            ) ON COMMIT DROP;
            INSERT INTO "Orders" ("Id", "GemListingId", "BuyerId", "Status", "PaidAt") VALUES
            ('00000000-0000-0000-0000-000000000001', 1, '00000000-0000-0000-0000-000000000011', 'Paid', CURRENT_TIMESTAMP),
            ('00000000-0000-0000-0000-000000000002', 1, '00000000-0000-0000-0000-000000000012', 'Paid', CURRENT_TIMESTAMP),
            ('00000000-0000-0000-0000-000000000003', 1, '00000000-0000-0000-0000-000000000013', 'Confirmed', NULL),
            ('00000000-0000-0000-0000-000000000004', 2, '00000000-0000-0000-0000-000000000011', 'Pending', NULL),
            ('00000000-0000-0000-0000-000000000005', 2, '00000000-0000-0000-0000-000000000011', 'Pending', NULL),
            ('00000000-0000-0000-0000-000000000006', 2, '00000000-0000-0000-0000-000000000012', 'Pending', NULL);
            """);
        await Execute(OrderApprovalSchema.Sql);
        Assert.Equal(6, await Count("SELECT COUNT(*) FROM \"Orders\""));
        Assert.Equal(2, await Count("SELECT COUNT(*) FROM \"Orders\" WHERE \"Status\" = 'Paid' AND \"PaidAt\" IS NOT NULL"));
        Assert.Equal(3, await Count("SELECT COUNT(*) FROM \"Orders\" WHERE \"IsLegacyDuplicate\""));
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM \"Orders\" WHERE \"Status\" = 'Confirmed' AND \"PaymentDueAt\" IS NOT NULL"));
        await Execute(OrderApprovalSchema.Sql);
        Assert.Equal(3, await Count("SELECT COUNT(*) FROM \"Orders\" WHERE \"IsLegacyDuplicate\""));

        async Task RejectDuplicate(string sql, string index)
        {
            await transaction.SaveAsync("duplicate_check");
            var error = await Assert.ThrowsAsync<PostgresException>(() => Execute(sql));
            Assert.Equal("23505", error.SqlState);
            Assert.Equal(index, error.ConstraintName);
            await transaction.RollbackAsync("duplicate_check");
        }
        await RejectDuplicate("""
            INSERT INTO "Orders" ("Id", "GemListingId", "BuyerId", "Status") VALUES
            ('00000000-0000-0000-0000-000000000007', 1, '00000000-0000-0000-0000-000000000014', 'Confirmed');
            """, "IX_Orders_ReservedGem");
        await RejectDuplicate("""
            INSERT INTO "Orders" ("Id", "GemListingId", "BuyerId", "Status") VALUES
            ('00000000-0000-0000-0000-000000000008', 2, '00000000-0000-0000-0000-000000000011', 'Pending');
            """, "IX_Orders_ActiveBuyerGem");
        await Execute("""
            INSERT INTO "Orders" ("Id", "GemListingId", "BuyerId", "Status") VALUES
            ('00000000-0000-0000-0000-000000000009', 2, '00000000-0000-0000-0000-000000000014', 'Pending');
            """);
        Assert.Equal(0, await Count("SELECT COUNT(*) FROM \"Orders\" WHERE \"Id\" = '00000000-0000-0000-0000-000000000009' AND \"IsLegacyDuplicate\""));
        await transaction.RollbackAsync();
    }
}
