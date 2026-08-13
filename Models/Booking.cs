namespace ProjectWork.Models;

/// <summary>
/// Бронирование места на событии
/// </summary>
public class Booking
{
    /// <summary>
    /// Уникальный идентификатор брони
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор события, к которому относится бронь
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Текущий статус брони
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// Дата и время создания брони
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата и время обработки брони. Пока бронь не обработана — null
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
