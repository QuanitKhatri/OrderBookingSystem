using Distribution.Domain.Common;

namespace Distribution.Services.Admin;

public record UserListItem(int Id, string Username, string FullName, UserRole Role, bool IsActive);
public record UserEditModel(int Id, string Username, string FullName, UserRole Role);

public interface IUserService
{
    Task<List<UserListItem>> SearchAsync(string? query, CancellationToken ct = default);
    Task<UserEditModel?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(UserEditModel model, string password, string performedBy, CancellationToken ct = default);
    Task UpdateAsync(UserEditModel model, string performedBy, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, string newPassword, string performedBy, CancellationToken ct = default);
    Task SetActiveAsync(int id, bool isActive, string performedBy, CancellationToken ct = default);
}
