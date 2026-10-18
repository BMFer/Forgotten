using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Forgotten.Nft.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so <c>dotnet ef migrations</c> / <c>database update</c>
/// can construct the context without booting the full host. Uses a throwaway
/// design-time database file; runtime configuration comes from DI instead.
/// </summary>
public sealed class ForgottenDbContextFactory : IDesignTimeDbContextFactory<ForgottenDbContext>
{
    public ForgottenDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ForgottenDbContext>()
            .UseSqlite("Data Source=forgotten-design.db")
            .Options;

        return new ForgottenDbContext(options);
    }
}
