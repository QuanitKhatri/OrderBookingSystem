using System.Diagnostics;
using Distribution.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Distribution.Services.Admin;

public class BackupService(DistributionDbContext db) : IBackupService
{
    public Task BackupToFileAsync(string filePath, CancellationToken ct = default) =>
        RunPgToolAsync("pg_dump", $"-Fc -f \"{filePath}\"", ct);

    public Task RestoreFromFileAsync(string filePath, CancellationToken ct = default) =>
        RunPgToolAsync("pg_restore", $"--clean --if-exists \"{filePath}\"", ct);

    private (string Host, int Port, string Database, string Username, string Password) ParseConnectionInfo()
    {
        var builder = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString());
        return (builder.Host!, builder.Port, builder.Database!, builder.Username!, builder.Password!);
    }

    private async Task RunPgToolAsync(string exeName, string args, CancellationToken ct)
    {
        var (host, port, database, username, password) = ParseConnectionInfo();

        var fullArgs = $"-h {host} -p {port} -U {username} -d {database} {args}";
        var psi = new ProcessStartInfo(exeName, fullArgs)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["PGPASSWORD"] = password;

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {exeName}. Ensure the PostgreSQL client tools are on PATH.");
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{exeName} failed: {error}");
    }
}
