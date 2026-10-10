using ProjectWork.Application.Abstractions.Repositories;
using ProjectWork.Domain.Exceptions;
using ProjectWork.Domain.Entities;

namespace ProjectWork.Application.Services;

public class BookingService(IEventRepository eventRepository, IBookingRepository bookingRepository) : IBookingService
{
    public Task<List<Booking>> GetBookingsAsync(CancellationToken cancellationToken = default) =>
        bookingRepository.GetAllAsync(cancellationToken: cancellationToken);

    public Task<Booking?> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        bookingRepository.GetByIdAsync(bookingId, cancellationToken);

    public Task<List<Booking>> GetPendingBookingsAsync(CancellationToken cancellationToken = default) =>
        bookingRepository.GetAllAsync(BookingStatus.Pending, cancellationToken);

    public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await EventWriteLock.Gate.WaitAsync(cancellationToken);
        try
        {
            var eventItem = await eventRepository.GetByIdForUpdateAsync(eventId, cancellationToken)
                ?? throw new NotFoundException($"Событие с идентификатором '{eventId}' не найдено.");
            if (!eventItem.TryReserveSeats())
                throw new NoAvailableSeatsException("No available seats for this event");

            var booking = Booking.Create(eventId);
            // Репозитории разделяют контекст: бронь и места сохраняются одной транзакцией.
            await bookingRepository.AddAsync(booking, cancellationToken);
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
            var booking = await bookingRepository.GetByIdForUpdateAsync(bookingId, cancellationToken);
            if (booking is null) return false;
            // Повторная обработка не должна второй раз возвращать место.
            if (booking.Status != BookingStatus.Pending) return false;

            if (status == BookingStatus.Rejected)
            {
                var eventItem = await eventRepository.GetByIdForUpdateAsync(booking.EventId, cancellationToken)
                    ?? throw new NotFoundException($"Событие с идентификатором '{booking.EventId}' не найдено.");
                booking.Reject();
                eventItem.ReleaseSeats();
            }
            else
            {
                booking.Confirm();
            }

            await bookingRepository.UpdateAsync(booking, cancellationToken);
            return true;
        }
        finally { EventWriteLock.Gate.Release(); }
    }
}
