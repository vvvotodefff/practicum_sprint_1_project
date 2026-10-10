using Microsoft.EntityFrameworkCore;
using ProjectWork.Application.Abstractions.Repositories;
using ProjectWork.Domain.Entities;

namespace ProjectWork.DataAccess.Repositories;

/// <summary>Запросы и сохранение броней через общий scoped-контекст EF Core.</summary>
public sealed class BookingRepository(AppDbContext context) : IBookingRepository
{
    /// <inheritdoc />
    public Task<List<Booking>> GetAllAsync(BookingStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Bookings.AsNoTracking();
        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        return query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Booking?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await context.Bookings.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null) return null;

        await context.Entry(booking).ReloadAsync(cancellationToken);
        return context.Entry(booking).State == EntityState.Detached ? null : booking;
    }

    /// <inheritdoc />
    public Task<List<Guid>> GetIdsByStatusAsync(BookingStatus status,
        CancellationToken cancellationToken = default) =>
        context.Bookings.Where(b => b.Status == status)
            .Select(b => b.Id).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        context.Bookings.Add(booking);
        // В этом же контексте отслеживается событие с уменьшенным количеством мест.
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        context.Entry(booking).State = EntityState.Modified;
        // При отклонении брони также сохраняется возврат места на событии.
        await context.SaveChangesAsync(cancellationToken);
    }
}
