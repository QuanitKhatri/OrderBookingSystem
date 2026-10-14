namespace Distribution.Services.Sequencing;

public interface ISequenceService
{
    /// <summary>Atomically increments the named sequence and returns the next number formatted as "{prefix}{number:D5}".</summary>
    Task<string> NextNumberAsync(string sequenceName, string prefix, CancellationToken ct = default);
}
