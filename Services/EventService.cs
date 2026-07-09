using ProjectWork.Models;

namespace ProjectWork.Services;

public class EventService : IEventService
{
    private static readonly List<Event> Events = [];

    public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        IEnumerable<Event> filteredEvents = Events;

        if (title != null)
        {
            filteredEvents = filteredEvents.Where(e => e.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }

        if (from != null)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= from);
        }

        if (to != null)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= to);
        }

        var totalCount = filteredEvents.Count();

        var items = filteredEvents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<Event>
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = items
        };
    }

    public Event? GetEventById(Guid id)
    {
        return Events.FirstOrDefault(e => e.Id == id);
    }

    public void AddEvent(Event eventItem)
    {
        eventItem.Id = Guid.NewGuid();
        Events.Add(eventItem);
    }

    public bool UpdateEvent(Guid id, Event eventItem)
    {
        var existingEvent = Events.FirstOrDefault(e => e.Id == id);

        if (existingEvent is null)
            return false;

        existingEvent.Title = eventItem.Title;
        existingEvent.Description = eventItem.Description;
        existingEvent.StartAt = eventItem.StartAt;
        existingEvent.EndAt = eventItem.EndAt;
        return true;
    }

    public bool DeleteEvent(Guid id)
    {
        var eventItem = Events.FirstOrDefault(e => e.Id == id);

        if (eventItem is null)
            return false;

        Events.Remove(eventItem);
        return true;
    }
}