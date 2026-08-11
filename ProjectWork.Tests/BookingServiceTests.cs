using ProjectWork.Models;
using ProjectWork.Services;

namespace ProjectWork.Tests;

public class BookingServiceTests
{
    private readonly BookingService _service = new();

    [Fact]
    public void AddBooking_SetsPendingStatusIdAndCreatedAt()
    {
        var eventId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var booking = _service.AddBooking(eventId);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventId, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.InRange(booking.CreatedAt, before, DateTime.UtcNow);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public void AddBooking_SeveralBookings_GetUniqueIds()
    {
        var eventId = Guid.NewGuid();

        var first = _service.AddBooking(eventId);
        var second = _service.AddBooking(eventId);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AddBooking_StoresBookingInStorage()
    {
        var booking = _service.AddBooking(Guid.NewGuid());

        var stored = Assert.Single(_service.GetBookings());
        Assert.Equal(booking.Id, stored.Id);
    }

    [Fact]
    public void GetBookings_EmptyStorage_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetBookings());
    }

    [Fact]
    public void GetBookingById_ExistingId_ReturnsBooking()
    {
        var added = _service.AddBooking(Guid.NewGuid());

        var found = _service.GetBookingById(added.Id);

        Assert.NotNull(found);
        Assert.Equal(added.Id, found.Id);
        Assert.Equal(added.EventId, found.EventId);
    }

    [Fact]
    public void GetBookingById_UnknownId_ReturnsNull()
    {
        _service.AddBooking(Guid.NewGuid());

        var found = _service.GetBookingById(Guid.NewGuid());

        Assert.Null(found);
    }
}
