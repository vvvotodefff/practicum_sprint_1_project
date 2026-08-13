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
    bool UpdateEvent(Guid id, Event eventItem);
    bool DeleteEvent(Guid id);
}
