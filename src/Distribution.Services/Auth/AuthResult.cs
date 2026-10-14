using Distribution.Domain.Common;

namespace Distribution.Services.Auth;

public record AuthResult(bool Succeeded, int UserId, string Username, string FullName, UserRole Role, string? Error);
