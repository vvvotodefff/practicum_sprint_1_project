using ProjectWork.DataAccess.Repositories;
using ProjectWork.DTO;
using ProjectWork.Domain.Exceptions;
using ProjectWork.Domain.Entities;
using ProjectWork.Models;

namespace ProjectWork.Services;

public class EventService(IEventRepository eventRepository) : IEventService
{
    public Task<PaginatedResult<Event>> GetEventsAsync(string? title, DateTime? from,
        DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default) =>
        eventRepository.GetPageAsync(title,
            from.HasValue ? Event.ToUtc(from.Value) : null,
            to.HasValue ? Event.ToUtc(to.Value) : null,
            page, pageSize, cancellationToken);

    public Task<Event?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        eventRepository.GetByIdAsync(id, cancellationToken);

    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var eventItem = Event.Create(request.Title, request.Description,
            request.StartAt, request.EndAt, request.TotalSeats ?? 0);
        await eventRepository.AddAsync(eventItem, cancellationToken);
        return EventInfo.FromEvent(eventItem);
    }

    public async Task<bool> UpdateEventAsync(Guid id, UpdateEvent request,
        CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await eventRepository.GetByIdForUpdateAsync(id, cancellationToken);
            if (eventItem is null) return false;
            eventItem.Update(request.Title, request.Description, request.StartAt,
                request.EndAt, request.TotalSeats ?? 0);
            await eventRepository.UpdateAsync(eventItem, cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            return await eventRepository.DeleteAsync(id, cancellationToken);
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1,
        CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await eventRepository.GetByIdForUpdateAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
            if (!eventItem.TryReserveSeats(count)) return false;
            await eventRepository.UpdateAsync(eventItem, cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task ReleaseSeatsAsync(Guid eventId, int count = 1,
        CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await eventRepository.GetByIdForUpdateAsync(eventId, cancellationToken);
            if (eventItem is null) return;
            eventItem.ReleaseSeats(count);
            await eventRepository.UpdateAsync(eventItem, cancellationToken);
        }
        finally { EventWriteLock.Gate.Release(); }
    }
}
