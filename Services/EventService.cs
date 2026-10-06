using Microsoft.EntityFrameworkCore;
using ProjectWork.DataAccess;
using ProjectWork.DTO;
using ProjectWork.Exceptions;
using ProjectWork.Models;

namespace ProjectWork.Services;

public class EventService(AppDbContext context) : IEventService
{
    public async Task<PaginatedResult<Event>> GetEventsAsync(string? title, DateTime? from,
        DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        var query = context.Events.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(title))
        {
            var search = title.ToLower();
            query = query.Where(e => e.Title.ToLower().Contains(search));
        }
        if (from.HasValue)
        {
            var start = Event.ToUtc(from.Value);
            query = query.Where(e => e.StartAt >= start);
        }
        if (to.HasValue)
        {
            var end = Event.ToUtc(to.Value);
            query = query.Where(e => e.EndAt <= end);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var offset = (long)(page - 1) * pageSize;
        var items = offset >= totalCount
            ? new List<Event>()
            : await query.OrderBy(e => e.StartAt).ThenBy(e => e.Id)
                .Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedResult<Event>
        {
            TotalCount = totalCount, Page = page, PageSize = pageSize, Items = items
        };
    }

    public Task<Event?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Events.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<EventInfo> CreateEventAsync(CreateEvent request, CancellationToken cancellationToken = default)
    {
        var eventItem = Event.Create(request.Title, request.Description,
            request.StartAt, request.EndAt, request.TotalSeats ?? 0);
        context.Events.Add(eventItem);
        await context.SaveChangesAsync(cancellationToken);
        return EventInfo.FromEvent(eventItem);
    }

    public async Task<bool> UpdateEventAsync(Guid id, UpdateEvent request,
        CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await FindForUpdateAsync(id, cancellationToken);
            if (eventItem is null) return false;
            eventItem.Update(request.Title, request.Description, request.StartAt,
                request.EndAt, request.TotalSeats ?? 0);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task<bool> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await FindForUpdateAsync(id, cancellationToken);
            if (eventItem is null) return false;
            // Явная загрузка позволяет проверить каскадное удаление и с InMemory.
            await context.Entry(eventItem).Collection(e => e.Bookings).LoadAsync(cancellationToken);
            context.Events.Remove(eventItem);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task<bool> TryReserveSeatsAsync(Guid eventId, int count = 1,
        CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await FindForUpdateAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
            if (!eventItem.TryReserveSeats(count)) return false;
            await context.SaveChangesAsync(cancellationToken);
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
            var eventItem = await FindForUpdateAsync(eventId, cancellationToken);
            if (eventItem is null) return;
            eventItem.ReleaseSeats(count);
            await context.SaveChangesAsync(cancellationToken);
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    private async Task<Event?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var eventItem = await context.Events.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        // При повторном вызове в одном scope обновляем ранее отслеживаемый экземпляр.
        if (eventItem is not null)
            await context.Entry(eventItem).ReloadAsync(cancellationToken);
        return eventItem;
    }
}
