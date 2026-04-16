using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace WMS.API.Data;

public class WmsDbContextFactory : IDesignTimeDbContextFactory<WmsDbContext>
{
    public WmsDbContext CreateDbContext(string[] args)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        //Tìm kiếm thư mục chứa appsettings.json bằng cách duyệt lên các thư mục cha từ thư mục hiện tại
        string? FindSettingsDir()
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (var d = dir; d != null; d = d.Parent)
            {
                var candidate = Path.Combine(d.FullName, "appsettings.json");
                if (File.Exists(candidate)) return d.FullName;
            }
            // Trở về thư mục gốc của ứng dụng nếu không tìm thấy trong thư mục hiện tại
            var baseDir = AppContext.BaseDirectory;
            var baseInfo = new DirectoryInfo(baseDir);
            for (var d = baseInfo; d != null; d = d.Parent)
            {
                var candidate = Path.Combine(d.FullName, "appsettings.json");
                if (File.Exists(candidate)) return d.FullName;
            }

            return null;
        }

        var settingsDir = FindSettingsDir();
        if (settingsDir == null)
            throw new InvalidOperationException("Could not locate appsettings.json. Ensure it exists in the project directory.");

        var builder = new ConfigurationBuilder()
            .SetBasePath(settingsDir)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        var configuration = builder.Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var optionsBuilder = new DbContextOptionsBuilder<WmsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new WmsDbContext(optionsBuilder.Options);
    }
}
