using Microsoft.Data.Sqlite;

namespace Data.SqliteEF;

public class MessagesDbContext : DbContext
{
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(MessagesDbContext).Assembly);
    }

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
