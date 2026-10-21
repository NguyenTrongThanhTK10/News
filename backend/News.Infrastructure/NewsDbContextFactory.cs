using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace News.Infrastructure;

// Migration SQL Server không cần kết nối server và không khởi động seed/auth của API.
public sealed class NewsDbContextFactory : IDesignTimeDbContextFactory<NewsDbContext>
{
    public NewsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<NewsDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__NewsDatabase")
            ?? "Server=localhost,1433;Database=Tin24;User Id=sa;Password=DesignTimeOnlyNotUsed!;TrustServerCertificate=True")
        .Options);
}
