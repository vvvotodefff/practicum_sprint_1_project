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
    /// Получить бронь по идентификатору. Возвращает null, если бронь не найдена
    /// </summary>
    Booking? GetBookingById(Guid id);

    /// <summary>
    /// Создать бронь для события: присваивает новый идентификатор,
    /// статус <see cref="BookingStatus.Pending"/> и текущее время создания
    /// </summary>
    Booking AddBooking(Guid eventId);
}
