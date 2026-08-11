using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Сервис для работы с бронированиями
/// </summary>
public class BookingService : IBookingService
{
    private readonly List<Booking> Bookings = [];

    /// <summary>
    /// Получить все брони
    /// </summary>
    /// <returns></returns>
    public List<Booking> GetBookings()
    {
        return Bookings;
    }

    /// <summary>
    /// Получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private Booking? GetBookingById(Guid id)
    {
        return Bookings.FirstOrDefault(b => b.Id == id);
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

        Bookings.Add(booking);
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


}
