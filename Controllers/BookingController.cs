using Microsoft.AspNetCore.Mvc;
using ProjectWork.Exceptions;
using ProjectWork.Models;
using ProjectWork.Services;

namespace ProjectWork.Controllers
{
    /// <summary>
    /// Бронирование мест на событиях
    /// </summary>
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;

        /// <summary>
        /// Создаёт контроллер бронирований
        /// </summary>
        public BookingController(IBookingService bookingService, IEventService eventService)
        {
            _bookingService = bookingService;
            _eventService = eventService;
        }

        /// <summary>
        /// Создать бронь для события. Бронь принимается в обработку и получает статус Pending
        /// </summary>
        /// <param name="id">Идентификатор события</param>
        /// <returns></returns>
        /// <response code="202">Бронь принята к обработке, ссылка на неё — в заголовке</response>
        /// <response code="404">Событие с указанным ID не найдено</response>
        [HttpPost("events/{id:guid}/book")]
        [ProducesResponseType(typeof(Booking), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Booking>> BookEvent(Guid id)
        {
            if (_eventService.GetEventById(id) is null)
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

            var booking = await _bookingService.CreateBookingAsync(id);

            return AcceptedAtAction(nameof(GetBookingById), new { id = booking.Id }, booking);
        }

        /// <summary>
        /// Получить текущее состояние брони по её уникальному идентификатору ID
        /// </summary>
        /// <param name="id">Идентификатор брони</param>
        /// <returns></returns>
        /// <response code="200">Успешно возвращает бронь с указанным ID</response>
        /// <response code="404">Бронь с указанным ID не найдена</response>
        [HttpGet("bookings/{id:guid}")]
        [ProducesResponseType(typeof(Booking), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Booking>> GetBookingById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            if (booking is null)
                throw new NotFoundException($"Бронь с идентификатором '{id}' не найдена.");

            return Ok(booking);
        }
    }
}
