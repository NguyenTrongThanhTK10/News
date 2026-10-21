using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace News.Tests;

public sealed class NewsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"tin24-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "Sqlite", ["Database:Initialize"] = "true",
            ["ConnectionStrings:Sqlite"] = $"Data Source={_database};Pooling=False",
            ["Jwt:SigningKey"] = "TIN24-Integration-Tests-Only-Key-Minimum-32-Bytes-2026",
            ["Seed:DemoData"] = "true",
        }));
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            foreach (var suffix in new[] { "", "-shm", "-wal" })
                if (File.Exists(_database + suffix)) File.Delete(_database + suffix);
    }
}
