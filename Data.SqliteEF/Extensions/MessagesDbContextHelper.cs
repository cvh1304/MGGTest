using Microsoft.Extensions.DependencyInjection;

namespace Data.SqliteEF.Extensions;

/// <summary>
/// Helper static service for using
/// SqliteEF.
/// </summary>
public static class MessagesDbContextHelper
{
    /// <summary>
    /// Add to <see cref="IServiceCollection"/>
    /// Sqlite support and <see cref="MessagesDbContext"/>.
    /// </summary>
    /// <param name="services">Collection where add services.</param>
    /// <returns></returns>
    public static IServiceCollection AddSqliteEFDbContext(
        this IServiceCollection services)
    {
        services
            .AddEntityFrameworkSqlite()
            .AddDbContext<MessagesDbContext>();

        return services;
    }

}
