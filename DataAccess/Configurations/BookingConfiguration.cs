using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectWork.Models;

namespace ProjectWork.DataAccess;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.ProcessedAt).IsRequired(false);
        builder.HasOne(b => b.Event).WithMany(e => e.Bookings)
            .HasForeignKey(b => b.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(b => b.Status);
    }
}
