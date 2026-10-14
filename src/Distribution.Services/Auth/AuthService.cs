using Distribution.Data;
using Distribution.Domain.Common;
using Distribution.Domain.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Distribution.Services.Auth;

public class AuthService(DistributionDbContext db, ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && !u.IsDeleted, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new AuthResult(false, 0, string.Empty, string.Empty, default, "Invalid username or password.");
        }

        return new AuthResult(true, user.Id, user.Username, user.FullName, user.Role, null);
    }

    public async Task EnsureDefaultAdminAsync(CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            FullName = "Administrator",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync(ct);

        logger.LogWarning("No users existed. Created default admin account (username: admin, password: admin123). Change this password immediately.");
    }
}
