using System;
using ProjectWork.Models;

namespace ProjectWork.Services;

public interface IEventService
{
    PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Event? GetEventById(Guid id);
    void AddEvent(Event eventItem);
    bool UpdateEvent(Guid id, Event eventItem);
    bool DeleteEvent(Guid id);
}
