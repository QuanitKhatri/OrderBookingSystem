namespace Distribution.Services.Masters;

public record CategoryListItem(int Id, string Name, string? Description, bool IsActive);

public record CategoryEditModel(int Id, string Name, string? Description);
