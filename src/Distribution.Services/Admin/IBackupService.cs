namespace Distribution.Services.Admin;

public interface IBackupService
{
    Task BackupToFileAsync(string filePath, CancellationToken ct = default);
    Task RestoreFromFileAsync(string filePath, CancellationToken ct = default);
}
