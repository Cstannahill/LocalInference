using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LocalInference.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        var connectionString = GetConnectionString();
        
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString()
    {
        // Try to find appsettings.json
        var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "src/LocalInference.Api/appsettings.json");
        
        if (!File.Exists(appSettingsPath))
        {
            appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }

        if (File.Exists(appSettingsPath))
        {
            using (var fs = File.OpenRead(appSettingsPath))
            {
                using (var doc = JsonDocument.Parse(fs))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("ConnectionStrings", out var connStrings) &&
                        connStrings.TryGetProperty("DefaultConnection", out var defaultConn))
                    {
                        return defaultConn.GetString() ?? "Host=localhost;Database=LocalInference;Username=postgres;Password=postgres";
                    }
                }
            }
        }

        return "Host=localhost;Database=LocalInference;Username=postgres;Password=postgres";
    }
}
