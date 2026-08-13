using ProjectWork.DTO;
using ProjectWork.Exceptions;
using ProjectWork.Models;

namespace ProjectWork.Services;

public class EventService : IEventService
{
    private readonly List<Event> Events = [];

    // Со списком событий одновременно работают веб-запросы и фоновая обработка броней,
    // поэтому любое чтение и изменение выполняется под блокировкой
    private readonly object _eventLock = new();

    public PaginatedResult<Event> GetEvents(string? title, DateTime? from, DateTime? to, int page, int pageSize)
    {
        lock (_eventLock)
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
    }

    public Event? GetEventById(Guid id)
    {
        lock (_eventLock)
        {
            return Events.FirstOrDefault(e => e.Id == id);
        }
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

        lock (_eventLock)
        {
            Events.Add(eventItem);
        }

        return Task.FromResult(EventInfo.FromEvent(eventItem));
    }

    public bool UpdateEvent(Guid id, UpdateEvent request)
    {
        lock (_eventLock)
        {
            var existingEvent = Events.FirstOrDefault(e => e.Id == id);

            if (existingEvent is null)
                return false;

            // Сущность сама проверит данные и пересчитает свободные места
            existingEvent.Update(
                request.Title,
                request.Description,
                request.StartAt,
                request.EndAt,
                request.TotalSeats ?? 0);

            return true;
        }
    }

    public bool DeleteEvent(Guid id)
    {
        lock (_eventLock)
        {
            var eventItem = Events.FirstOrDefault(e => e.Id == id);

            if (eventItem is null)
                return false;

            Events.Remove(eventItem);
            return true;
        }
    }

    public bool TryReserveSeats(Guid eventId, int count = 1)
    {
        // Поиск события и занятие места — одна неделимая операция
        lock (_eventLock)
        {
            var eventItem = Events.FirstOrDefault(e => e.Id == eventId)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");

            return eventItem.TryReserveSeats(count);
        }
    }

    public void ReleaseSeats(Guid eventId, int count = 1)
    {
        lock (_eventLock)
        {
            // Событие могли удалить — возвращать место некуда, это не ошибка
            Events.FirstOrDefault(e => e.Id == eventId)?.ReleaseSeats(count);
        }
    }
}
