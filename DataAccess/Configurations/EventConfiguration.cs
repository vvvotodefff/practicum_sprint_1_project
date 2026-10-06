using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectWork.Models;

namespace ProjectWork.DataAccess;

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events", table =>
        {
            table.HasCheckConstraint("ck_events_dates", "\"EndAt\" > \"StartAt\"");
            table.HasCheckConstraint("ck_events_seats",
                "\"TotalSeats\" > 0 AND \"AvailableSeats\" >= 0 AND \"AvailableSeats\" <= \"TotalSeats\"");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.StartAt).IsRequired();
        builder.Property(e => e.EndAt).IsRequired();
        builder.Property(e => e.TotalSeats).IsRequired();
        builder.Property(e => e.AvailableSeats).IsRequired();
        builder.HasMany(e => e.Bookings).WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.StartAt);
    }
}
