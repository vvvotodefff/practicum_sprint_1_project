using ProjectWork.Models;

namespace ProjectWork.Services;

public interface IEventService
{
    List<Event> GetEvents();
    Event? GetEventById(Guid id);
    void AddEvent(Event eventItem);
    bool UpdateEvent(Guid id, Event eventItem);
    bool DeleteEvent(Guid id);
}
