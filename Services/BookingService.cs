using Microsoft.EntityFrameworkCore;
using ProjectWork.DataAccess;
using ProjectWork.Exceptions;
using ProjectWork.Models;

namespace ProjectWork.Services;

public class BookingService(AppDbContext context) : IBookingService
{
    public Task<List<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        context.Bookings.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        context.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

    public Task<List<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default) =>
        context.Bookings.AsNoTracking().Where(b => b.Status == BookingStatus.Pending)
            .ToListAsync(cancellationToken);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await context.Events.SingleOrDefaultAsync(e => e.Id == eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
            await context.Entry(eventItem).ReloadAsync(cancellationToken);
            if (!eventItem.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = Booking.Create(eventId);
            context.Bookings.Add(booking);
            // PostgreSQL сохраняет бронь и уменьшение мест одной транзакцией.
            await context.SaveChangesAsync(cancellationToken);
            return booking;
        }
        finally { EventWriteLock.Gate.Release(); }
    }

    public async Task<bool> MarkAsProcessedAsync(Guid bookingId, BookingStatus status,
        CancellationToken cancellationToken = default)
    {
        if (status is not (BookingStatus.Confirmed or BookingStatus.Rejected)) return false;
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var booking = await context.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking is null) return false;
            await context.Entry(booking).ReloadAsync(cancellationToken);
            // Повторная обработка не должна второй раз возвращать место.
            if (booking.Status != BookingStatus.Pending) return false;

            if (status == BookingStatus.Rejected)
            {
                var eventItem = await context.Events.SingleAsync(e => e.Id == booking.EventId, cancellationToken);
                await context.Entry(eventItem).ReloadAsync(cancellationToken);
                booking.Reject();
                eventItem.ReleaseSeats();
            }
            else
            {
                booking.Confirm();
            }

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }
}
