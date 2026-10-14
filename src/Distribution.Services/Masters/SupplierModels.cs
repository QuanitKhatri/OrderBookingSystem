namespace Distribution.Services.Masters;

public record SupplierListItem(int Id, string Code, string Name, string CompanyName, decimal OpeningBalance, bool IsActive);

public record SupplierEditModel(int Id, string Code, string Name, int CompanyId, string? ContactPerson, string? Phone, string? Address, decimal OpeningBalance);
