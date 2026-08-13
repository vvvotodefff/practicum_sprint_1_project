using ProjectWork.DTO;
using ProjectWork.Models;

namespace ProjectWork.Services;

public class EventService : IEventService
{
    private readonly List<Event> Events = [];

    public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        IEnumerable<Event> filteredEvents = Events;

        if (!string.IsNullOrWhiteSpace(title))
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

    public Task<EventInfo> CreateEventAsync(CreateEvent request)
    {
        // Фабрика проверяет данные, выдаёт Id и делает все места свободными
        var eventItem = Event.Create(
            request.Title,
            request.Description,
            request.StartAt,
            request.EndAt,
            request.TotalSeats ?? 0);

        Events.Add(eventItem);

        return Task.FromResult(EventInfo.FromEvent(eventItem));
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
        existingEvent.TotalSeats = eventItem.TotalSeats;
        existingEvent.AvailableSeats = eventItem.AvailableSeats;
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