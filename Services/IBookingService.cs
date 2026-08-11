using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Хранилище бронирований
/// </summary>
public interface IBookingService
{
    /// <summary>
    /// Получить все брони
    /// </summary>
    List<Booking> GetBookings();

    /// <summary>
    /// Создать бронь для события: присваивает новый идентификатор,
    /// статус <see cref="BookingStatus.Pending"/> и текущее время создания
    /// </summary>
    /// <param name="eventId">Идентификатор события</param>
    Task<Booking> CreateBookingAsync(Guid eventId);

    /// <summary>
    /// Получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    /// <param name="bookingId">Идентификатор брони</param>
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);
}
