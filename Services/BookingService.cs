using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService : IBookingService
{
    private readonly List<Booking> Bookings = [];

    // Любое чтение и изменение списка под блокировкой
    private readonly object BookingsLock = new();

    /// <summary>
    /// Получить все брони
    /// </summary>
    /// <returns></returns>
    public List<Booking> GetBookings()
    {
        lock (BookingsLock)
        {
            return Bookings.ToList();
        }
    }

    /// <summary>
    /// Получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Booking? GetBookingById(Guid id)
    {
        lock (BookingsLock)
        {
            return Bookings.FirstOrDefault(b => b.Id == id);
        }
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

        lock (BookingsLock)
        {
            Bookings.Add(booking);
        }

        return booking;
    }

    /// <summary>
    /// Асинхронно создать бронь для события: присваивает новый идентификатор
    /// </summary>
    /// <param name="eventId"></param>
    /// <returns></returns>
    public async Task<Booking> CreateBookingAsync(Guid eventId)
    {
        // Симуляция асинхронной операции
        await Task.Delay(100);
        return AddBooking(eventId);
    }

    /// <summary>
    /// Асинхронно получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<Booking?> GetBookingByIdAsync(Guid id)
    {
        // Симуляция асинхронной операции
        await Task.Delay(100);
        return GetBookingById(id);
    }

    /// <summary>
    /// Получить брони, ожидающие обработки (статус Pending)
    /// </summary>
    /// <returns>Копия списка/returns>
    public List<Booking> GetPendingBookings()
    {
        lock (BookingsLock)
        {
            return Bookings.Where(b => b.Status == BookingStatus.Pending).ToList();
        }
    }

    /// <summary>
    /// Перевести бронь в указанный статус и проставить время обработки
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    /// <param name="status">Новый статус брони</param>
    /// <returns>false, если бронь с таким идентификатором не найдена</returns>
    public bool MarkAsProcessed(Guid bookingId, BookingStatus status)
    {
        lock (BookingsLock)
        {
            var booking = Bookings.FirstOrDefault(b => b.Id == bookingId);

            if (booking is null)
                return false;

            booking.Status = status;
            booking.ProcessedAt = DateTime.UtcNow;
            return true;
        }
    }
}
