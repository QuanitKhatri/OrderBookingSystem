using Distribution.Domain.Common;

namespace Distribution.Domain.System;

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public AuditAction Action { get; set; }
    public string Entity { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime At { get; set; }
}
