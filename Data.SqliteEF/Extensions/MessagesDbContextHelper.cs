using Microsoft.Extensions.DependencyInjection;

namespace Data.SqliteEF.Extensions;

public static class MessagesDbContextHelper
{
    public static IServiceCollection AddSqliteEFDbContext(
        this IServiceCollection services)
    {
        services
            .AddEntityFrameworkSqlite()
            .AddDbContext<MessagesDbContext>();

        return services;
    }

}
