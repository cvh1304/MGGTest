using Microsoft.Data.Sqlite;

namespace Data.SqliteEF;

/// <summary>
/// EF database context for
/// saving/reading not published <see cref="Message"/>.
/// </summary>
public class MessagesDbContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MessagesDbContext).Assembly);
    }

    /// <summary>
    /// Configure sqlite connection
    /// to local file message.db.
    /// </summary>
    /// <param name="optionsBuilder"><inheritdoc/></param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = "messages.db"
        };

        var connection = new SqliteConnection(connectionStringBuilder.ToString());

        optionsBuilder.UseSqlite(connection);
    }
}
