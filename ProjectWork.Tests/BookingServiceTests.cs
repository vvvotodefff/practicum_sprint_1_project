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

    /// <summary>
    /// Создаёт событие заданной ёмкости для теста
    /// </summary>
    private async Task<EventInfo> CreateTestEvent(int totalSeats = 100)
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
        var eventItem = await CreateTestEvent();
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
        var eventItem = await CreateTestEvent();

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
        var eventItem = await CreateTestEvent();

        var booking = await _service.CreateBookingAsync(eventItem.Id);

        var stored = Assert.Single(_service.GetBookings());
        Assert.Equal(booking.Id, stored.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingId_ReturnsBooking()
    {
        var eventItem = await CreateTestEvent();
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
        var eventItem = await CreateTestEvent();
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
        var eventItem = await CreateTestEvent();
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
        var eventItem = await CreateTestEvent();
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
        var eventItem = await CreateTestEvent();
        var booking = await _service.CreateBookingAsync(eventItem.Id);

        booking.Reject();

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.ProcessedAt);
    }

    [Fact]
    public async Task Reject_AndReleaseSeats_RestoresAvailableSeats()
    {
        var eventItem = await CreateTestEvent(3);
        var booking = await _service.CreateBookingAsync(eventItem.Id);
        Assert.Equal(2, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);

        booking.Reject();
        _eventService.ReleaseSeats(booking.EventId);

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.Equal(3, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);
    }

    [Fact]
    public async Task Reject_AndReleaseSeats_AllowsBookingTheFreedSeat()
    {
        // Единственное место занято
        var eventItem = await CreateTestEvent(1);
        var booking = await _service.CreateBookingAsync(eventItem.Id);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        // Отклоняем бронь и возвращаем место в пул
        booking.Reject();
        _eventService.ReleaseSeats(booking.EventId);

        // Теперь место снова можно занять
        var newBooking = await _service.CreateBookingAsync(eventItem.Id);

        Assert.Equal(BookingStatus.Pending, newBooking.Status);
        Assert.NotEqual(booking.Id, newBooking.Id);
        Assert.Equal(0, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);
    }

    [Fact]
    public async Task MarkAsProcessed_PendingStatus_ReturnsFalse()
    {
        var eventItem = await CreateTestEvent();
        var booking = await _service.CreateBookingAsync(eventItem.Id);

        var result = _service.MarkAsProcessed(booking.Id, BookingStatus.Pending);

        Assert.False(result);
        Assert.Null(booking.ProcessedAt);
    }

    // ----- Места на событии -----

    [Fact]
    public async Task CreateBookingAsync_ReservesSeatOnEvent()
    {
        var eventItem = await CreateTestEvent(10);

        await _service.CreateBookingAsync(eventItem.Id);
        await _service.CreateBookingAsync(eventItem.Id);

        var stored = _eventService.GetEventById(eventItem.Id);
        Assert.NotNull(stored);
        Assert.Equal(8, stored.AvailableSeats);
        Assert.Equal(10, stored.TotalSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_DecreasesAvailableSeatsByOne()
    {
        var eventItem = await CreateTestEvent(5);

        await _service.CreateBookingAsync(eventItem.Id);

        var stored = _eventService.GetEventById(eventItem.Id);
        Assert.NotNull(stored);
        Assert.Equal(4, stored.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_BookingsUpToLimit_AllSucceedWithUniqueIds()
    {
        const int totalSeats = 5;
        var eventItem = await CreateTestEvent(totalSeats);

        var bookings = new List<Booking>();
        for (var i = 0; i < totalSeats; i++)
        {
            bookings.Add(await _service.CreateBookingAsync(eventItem.Id));
        }

        Assert.Equal(totalSeats, bookings.Count);
        Assert.Equal(totalSeats, bookings.Select(b => b.Id).Distinct().Count());
        Assert.All(bookings, b => Assert.Equal(BookingStatus.Pending, b.Status));
        Assert.Equal(0, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_NoSeatsLeft_ThrowsNoAvailableSeats()
    {
        var eventItem = await CreateTestEvent(1);

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
        var eventItem = await CreateTestEvent(1);

        await _service.CreateBookingAsync(eventItem.Id);
        await Assert.ThrowsAsync<NoAvailableSeatsException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        // В хранилище осталась только успешная бронь
        Assert.Single(_service.GetBookings());
    }

    // ----- Конкурентность -----

    [Fact]
    public async Task ConcurrentBookings_DoNotOverbookEvent()
    {
        const int totalSeats = 5;
        const int requests = 20;
        var eventItem = await CreateTestEvent(totalSeats);

        // Все запросы стартуют одновременно на разных потоках пула
        var tasks = Enumerable.Range(0, requests)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await _service.CreateBookingAsync(eventItem.Id);
                    return true;
                }
                catch (NoAvailableSeatsException)
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, results.Count(success => success));
        Assert.Equal(requests - totalSeats, results.Count(success => !success));
        Assert.Equal(0, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);
        Assert.Equal(totalSeats, _service.GetBookings().Count);
    }

    [Fact]
    public async Task ConcurrentBookings_ProduceUniqueIds()
    {
        const int totalSeats = 10;
        var eventItem = await CreateTestEvent(totalSeats);

        var tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => Task.Run(() => _service.CreateBookingAsync(eventItem.Id)))
            .ToArray();

        var bookings = await Task.WhenAll(tasks);

        Assert.Equal(totalSeats, bookings.Length);
        Assert.Equal(totalSeats, bookings.Select(b => b.Id).Distinct().Count());
        Assert.Equal(0, _eventService.GetEventById(eventItem.Id)!.AvailableSeats);
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
        var eventItem = await CreateTestEvent();
        _eventService.DeleteEvent(eventItem.Id);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.CreateBookingAsync(eventItem.Id));

        Assert.Empty(_service.GetBookings());
    }

    [Fact]
    public async Task GetBookingByIdAsync_UnknownId_ReturnsNull()
    {
        var eventItem = await CreateTestEvent();
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
