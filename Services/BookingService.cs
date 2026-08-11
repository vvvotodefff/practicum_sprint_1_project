using ProjectWork.Models;

namespace ProjectWork.Services;

/// <inheritdoc />
public class BookingService : IBookingService
{
    private readonly List<Booking> Bookings = [];

    /// <inheritdoc />
    public List<Booking> GetBookings()
    {
        return Bookings;
    }

    /// <inheritdoc />
    public Booking? GetBookingById(Guid id)
    {
        return Bookings.FirstOrDefault(b => b.Id == id);
    }

    /// <inheritdoc />
    public Booking AddBooking(Guid eventId)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        Bookings.Add(booking);
        return booking;
    }
}
