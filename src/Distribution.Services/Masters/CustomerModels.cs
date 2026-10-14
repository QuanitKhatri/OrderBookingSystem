namespace Distribution.Services.Masters;

public record CustomerListItem(int Id, string Code, string ShopName, string AreaName, decimal Balance, bool IsActive);

public record CustomerEditModel(int Id, string Code, string ShopName, string? OwnerName, string? Phone, int AreaId, string? Address, decimal OpeningBalance, decimal DefaultDiscountPct);
