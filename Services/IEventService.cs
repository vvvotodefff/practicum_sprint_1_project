using ProjectWork.DTO;
using ProjectWork.Models;

namespace ProjectWork.Services;

public interface IEventService
{
    PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Event? GetEventById(Guid id);

    /// <summary>
    /// Создать событие из данных запроса. Бросает ValidationException, если данные некорректны
    /// </summary>
    Task<EventInfo> CreateEventAsync(CreateEvent request);
    /// <summary>
    /// Обновить событие. Свободные места пересчитываются сервером
    /// </summary>
    bool UpdateEvent(Guid id, UpdateEvent request);
    bool DeleteEvent(Guid id);

    /// <summary>
    /// Занять места на событии. Возвращает false, если свободных мест не хватает.
    /// Бросает NotFoundException, если события нет
    /// </summary>
    bool TryReserveSeats(Guid eventId, int count = 1);

    /// <summary>
    /// Вернуть места в пул. Если события уже нет, ничего не делает
    /// </summary>
    void ReleaseSeats(Guid eventId, int count = 1);
}
