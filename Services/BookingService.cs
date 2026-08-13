using System.Collections.Concurrent;
using ProjectWork.Exceptions;
using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService : IBookingService
{
    // Потокобезопасное хранилище: чтение и добавление не требуют блокировки
    private readonly ConcurrentDictionary<Guid, Booking> Bookings = new();

    // Замок нужен только для атомарной пары «проверить места + создать бронь»
    private readonly object _bookingLock = new();

    private readonly IEventService _eventService;

    /// <summary>
    /// Создаёт сервис бронирований
    /// </summary>
    public BookingService(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Получить все брони
    /// </summary>
    /// <returns></returns>
    public List<Booking> GetBookings()
    {
        return Bookings.Values.ToList();
    }

    /// <summary>
    /// Получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Booking? GetBookingById(Guid id)
    {
        return Bookings.TryGetValue(id, out var booking) ? booking : null;
    }

    /// <summary>
    /// Создать бронь для события: присваивает новый идентификатор
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    private Booking AddBooking(Guid eventId)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        Bookings[booking.Id] = booking;

        return booking;
    }

    /// <summary>
    /// Создать бронь для события: занимает место и сохраняет бронь.
    /// Бросает <see cref="NoAvailableSeatsException"/>, если свободных мест нет
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    public Task<Booking> CreateBookingAsync(Guid eventId)
    {
        // Критическая секция: поиск события, занятие места и создание брони
        // выполняются целиком, без вмешательства других потоков
        lock (_bookingLock)
        {
            // Бронировать можно только существующее событие
            var eventItem = _eventService.GetEventById(eventId)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");

            if (!eventItem.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            return Task.FromResult(AddBooking(eventId));
        }
    }

    /// <summary>
    /// Асинхронно получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        return Task.FromResult(GetBookingById(id));
    }

    /// <summary>
    /// Получить брони, ожидающие обработки (статус Pending)
    /// </summary>
    /// <returns>Копия списка</returns>
    public List<Booking> GetPendingBookings()
    {
        return Bookings.Values.Where(b => b.Status == BookingStatus.Pending).ToList();
    }

    /// <summary>
    /// Перевести бронь в указанный статус и проставить время обработки
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="status">Новый статус брони</param>
    /// <returns>false, если бронь с таким идентификатором не найдена</returns>
    public bool MarkAsProcessed(Guid bookingId, BookingStatus status)
    {
        if (!Bookings.TryGetValue(bookingId, out var booking))
            return false;

        booking.Status = status;
        booking.ProcessedAt = DateTime.UtcNow;
        return true;
    }
}
