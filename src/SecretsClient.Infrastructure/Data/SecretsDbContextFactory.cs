using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecretsClient.Infrastructure.Data
{
    /// <summary>
    /// Factory usado por las herramientas de EF Core (dotnet ef migrations / database update)
    /// en tiempo de diseño. NO afecta el runtime — la app usa la connection string de appsettings.json.
    /// </summary>
    public class SecretsDbContextFactory : IDesignTimeDbContextFactory<SecretsDbContext>
    {
        public SecretsDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<SecretsDbContext>();
            builder.UseSqlite("Data Source=secrets.db");
            return new SecretsDbContext(builder.Options);
        }
    }
}
