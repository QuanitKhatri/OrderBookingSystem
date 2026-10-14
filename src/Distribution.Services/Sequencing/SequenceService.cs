using Distribution.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Distribution.Services.Sequencing;

public class SequenceService(DistributionDbContext db) : ISequenceService
{
    public async Task<string> NextNumberAsync(string sequenceName, string prefix, CancellationToken ct = default)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened) await connection.OpenAsync(ct);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO "Sequences" ("Name", "LastNumber") VALUES (@name, 1)
                ON CONFLICT ("Name") DO UPDATE SET "LastNumber" = "Sequences"."LastNumber" + 1
                RETURNING "LastNumber";
                """;
            cmd.Parameters.AddWithValue("name", sequenceName);

            var result = await cmd.ExecuteScalarAsync(ct);
            var number = (int)result!;
            return $"{prefix}{number:D5}";
        }
        finally
        {
            if (opened) await connection.CloseAsync();
        }
    }
}
