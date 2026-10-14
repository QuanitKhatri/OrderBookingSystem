using Distribution.Data;
using Distribution.Services.Auth;
using Distribution.Services.Expenses;
using Distribution.Services.Masters;
using Distribution.Services.Reports;
using Distribution.Services.Sales;
using Distribution.Services.Sequencing;
using Distribution.Services.Stock;
using Distribution.Services.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Distribution.Services;

public static class DependencyInjection
{
    public static IServiceCollection AddDistributionServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<DistributionDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<ISequenceService, SequenceService>();
        services.AddScoped<IStockQueryService, StockQueryService>();
        services.AddScoped<IOpeningStockService, OpeningStockService>();
        services.AddScoped<IDamageReasonService, DamageReasonService>();
        services.AddScoped<IDamageService, DamageService>();
        services.AddScoped<IPurchaseService, PurchaseService>();

        services.AddScoped<ICustomerBalanceService, CustomerBalanceService>();
        services.AddScoped<ISaleService, SaleService>();
        services.AddScoped<ICustomerLedgerService, CustomerLedgerService>();
        services.AddScoped<IMarketCreditService, MarketCreditService>();

        services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
        services.AddScoped<IExpenseService, ExpenseService>();

        services.AddScoped<IReportService, ReportService>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IBackupService, BackupService>();

        return services;
    }
}
