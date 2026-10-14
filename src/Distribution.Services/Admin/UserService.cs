using Distribution.Data;
using Distribution.Domain.System;
using Microsoft.EntityFrameworkCore;

namespace Distribution.Services.Admin;

public class UserService(DistributionDbContext db) : IUserService
{
    public async Task<List<UserListItem>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = db.Users.Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(u => EF.Functions.ILike(u.Username, $"%{term}%") || EF.Functions.ILike(u.FullName, $"%{term}%"));
        }

        return await q.OrderBy(u => u.Username)
            .Select(u => new UserListItem(u.Id, u.Username, u.FullName, u.Role, u.IsActive))
            .ToListAsync(ct);
    }

    public async Task<UserEditModel?> GetAsync(int id, CancellationToken ct = default)
    {
        return await db.Users.Where(u => u.Id == id)
            .Select(u => new UserEditModel(u.Id, u.Username, u.FullName, u.Role))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<int> CreateAsync(UserEditModel model, string password, string performedBy, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == model.Username, ct))
            throw new InvalidOperationException("Username already exists.");

        var entity = new User
        {
            Username = model.Username.Trim(),
            FullName = model.FullName.Trim(),
            Role = model.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };
        db.Users.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(UserEditModel model, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == model.Id, ct)
            ?? throw new InvalidOperationException("User not found.");

        if (entity.Username != model.Username.Trim() && await db.Users.AnyAsync(u => u.Username == model.Username && u.Id != model.Id, ct))
            throw new InvalidOperationException("Username already exists.");

        entity.Username = model.Username.Trim();
        entity.FullName = model.FullName.Trim();
        entity.Role = model.Role;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task ResetPasswordAsync(int id, string newPassword, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException("User not found.");

        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(int id, bool isActive, string performedBy, CancellationToken ct = default)
    {
        var entity = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException("User not found.");

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = performedBy;

        await db.SaveChangesAsync(ct);
    }
}
