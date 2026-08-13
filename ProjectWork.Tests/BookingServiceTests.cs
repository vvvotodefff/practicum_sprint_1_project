using ProjectWork.DTO;
using ProjectWork.Exceptions;
using ProjectWork.Models;
using ProjectWork.Services;

namespace ProjectWork.Tests;

public class BookingServiceTests
{
    private readonly EventService _eventService = new();
    private readonly BookingService _service;

    public BookingServiceTests()
    {
        _service = new BookingService(_eventService);
    }

    private async Task<EventInfo> AddEvent(int totalSeats = 100)
    {
        return await _eventService.CreateEventAsync(new CreateEvent
        {
            Title = "Концерт",
            StartAt = new DateTime(2026, 9, 1, 19, 0, 0),
            EndAt = new DateTime(2026, 9, 1, 22, 0, 0),
            TotalSeats = totalSeats
        });
    }

    // ----- Успешные сценарии -----

    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsPendingBooking()
    {
        var eventItem = await AddEvent();
        var before = DateTime.UtcNow;

        var booking = await _service.CreateBookingAsync(eventItem.Id);

        Assert.NotEqual(Guid.Empty, booking.Id);
        Assert.Equal(eventItem.Id, booking.EventId);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.InRange(booking.CreatedAt, before, DateTime.UtcNow);
        Assert.Null(booking.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_SeveralBookingsForSameEvent_GetUniqueIds()
    {
        var eventItem = await AddEvent();

        var first = await _service.CreateBookingAsync(eventItem.Id);
        var second = await _service.CreateBookingAsync(eventItem.Id);
        var third = await _service.CreateBookingAsync(eventItem.Id);

        var ids = new[] { first.Id, second.Id, third.Id };
        Assert.Equal(3, ids.Distinct().Count());
        Assert.Equal(3, _service.GetBookings().Count);
    }

    [Fact]
    public async Task CreateBookingAsync_StoresBookingInStorage()
    {
        var eventItem = await AddEvent();

        var booking = await _service.CreateBookingAsync(eventItem.Id);

        var stored = Assert.Single(_service.GetBookings());
        Assert.Equal(booking.Id, stored.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsBooking()
    {
        var eventItem = await AddEvent();
        var created = await _service.CreateBookingAsync(eventItem.Id);

        var found = await _service.GetBookingByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(eventItem.Id, found.EventId);
        Assert.Equal(BookingStatus.Pending, found.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.Rejected)]
    public async Task GetBookingByIdAsync_AfterProcessing_ReflectsStatusChange(BookingStatus status)
    {
        var eventItem = await AddEvent();
        var created = await _service.CreateBookingAsync(eventItem.Id);
        var before = DateTime.UtcNow;

        var processed = _service.MarkAsProcessed(created.Id, status);

        Assert.True(processed);
        var found = await _service.GetBookingByIdAsync(created.Id);
        Assert.NotNull(found);
        Assert.Equal(status, found.Status);
        Assert.NotNull(found.ProcessedAt);
        Assert.InRange(found.ProcessedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPendingBookings_ReturnsOnlyUnprocessedBookings()
    {
        var eventItem = await AddEvent();
        var first = await _service.CreateBookingAsync(eventItem.Id);
        await _service.CreateBookingAsync(eventItem.Id);

        _service.MarkAsProcessed(first.Id, BookingStatus.Confirmed);

        var pending = _service.GetPendingBookings();

        Assert.Single(pending);
        Assert.DoesNotContain(pending, b => b.Id == first.Id);
    }

    // ----- Смена статуса брони -----

    [Fact]
    public async Task Confirm_SetsConfirmedStatusAndProcessedAt()
    {
        var eventItem = await AddEvent();
        var booking = await _service.CreateBookingAsync(eventItem.Id);
        var before = DateTime.UtcNow;

        booking.Confirm();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
        Assert.InRange(booking.ProcessedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task Reject_SetsRejectedStatusAndProcessedAt()
    {
        var eventItem = await AddEvent();
        var booking = await _service.CreateBookingAsync(eventItem.Id);

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task MarkAsProcessed_PendingStatus_ReturnsFalse()
    {
        var eventItem = await AddEvent();
        var booking = await _service.CreateBookingAsync(eventItem.Id);

        var result = _service.MarkAsProcessed(booking.Id, BookingStatus.Pending);

        Assert.False(result);
        Assert.Null(booking.ProcessedAt);
    }

    // ----- Места на событии -----

    [Fact]
    public async Task CreateBookingAsync_ReservesSeatOnEvent()
    {
        var eventItem = await AddEvent(10);

        await _service.CreateBookingAsync(eventItem.Id);
        await _service.CreateBookingAsync(eventItem.Id);

        var stored = _eventService.GetEventById(eventItem.Id);
        Assert.NotNull(stored);
        Assert.Equal(8, stored.AvailableSeats);
        Assert.Equal(10, stored.TotalSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsLeft_ThrowsNoAvailableSeats()
    {
        var eventItem = await AddEvent(1);

        var first = await _service.CreateBookingAsync(eventItem.Id);

        var exception = await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        Assert.Equal("No available seats for this event", exception.Message);
        Assert.Equal(BookingStatus.Pending, first.Status);

        var stored = _eventService.GetEventById(eventItem.Id);
        Assert.NotNull(stored);
        Assert.Equal(0, stored.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsLeft_DoesNotCreateBooking()
    {
        var eventItem = await AddEvent(1);

        await _service.CreateBookingAsync(eventItem.Id);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        // В хранилище осталась только успешная бронь
        Assert.Single(_service.GetBookings());
    }

    // ----- Неуспешные сценарии -----

    [Fact]
    public async Task CreateBookingAsync_UnknownEvent_ThrowsNotFound()
    {
        var unknownEventId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateBookingAsync(unknownEventId));

        Assert.Contains(unknownEventId.ToString(), exception.Message);
        Assert.Empty(_service.GetBookings());
    }

    [Fact]
    public async Task CreateBookingAsync_DeletedEvent_ThrowsNotFound()
    {
        var eventItem = await AddEvent();
        _eventService.DeleteEvent(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        Assert.Empty(_service.GetBookings());
    }

    [Fact]
    public async Task GetBookingByIdAsync_UnknownId_ReturnsNull()
    {
        var eventItem = await AddEvent();
        await _service.CreateBookingAsync(eventItem.Id);

        var found = await _service.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void GetBookings_EmptyStorage_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetBookings());
    }

    [Fact]
    public void MarkAsProcessed_UnknownId_ReturnsFalse()
    {
        var processed = _service.MarkAsProcessed(Guid.NewGuid(), BookingStatus.Confirmed);

        Assert.False(processed);
    }
}
