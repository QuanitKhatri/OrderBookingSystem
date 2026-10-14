namespace Distribution.Services.Masters;

public record CompanyListItem(int Id, string Code, string Name, string? ContactPerson, string? Phone, bool IsActive);

public record CompanyEditModel(int Id, string Code, string Name, string? ContactPerson, string? Phone);
