using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.SqliteEF.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable(nameof(Message));

        builder
            .Property(x => x.TextMessage)
            .HasColumnName("message")
            .HasColumnType("TEXT");

        builder
            .Property(x => x.PriorityLevel)
            .HasColumnName("priotiry")
            .HasColumnType("NUMBER");
    }
}
