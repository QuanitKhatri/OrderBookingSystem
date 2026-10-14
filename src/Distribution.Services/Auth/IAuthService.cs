namespace Distribution.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default);
    Task EnsureDefaultAdminAsync(CancellationToken ct = default);
}
