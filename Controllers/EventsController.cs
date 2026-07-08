using Microsoft.AspNetCore.Mvc;
using ProjectWork.Services;
using ProjectWork.Models;

namespace ProjectWork.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Получить список всех событий
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Успешно возвращает список событий</response>
        [HttpGet]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<Event>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Event>> GetEvents()
        {
            return Ok(_eventService.GetEvents());
        }

        /// <summary>
        /// Получить событие по его уникальному идентификатору ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">Успешно возвращает событие с указанным ID</response>
        /// <response code="404">Событие с указанным ID не найдено</response>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Event), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public ActionResult<Event> GetEventById(Guid id)
        {
            var eventItem = _eventService.GetEventById(id);

            if (eventItem is null)
                return EventNotFound(id);

            return Ok(eventItem);
        }

        /// <summary>
        /// Создать новое событие. ID события будет сгенерирован автоматически при сохранении в базе данных
        /// </summary>
        /// <param name="eventItem"></param>
        /// <returns></returns>
        /// <response code="201">Успешно создает новое событие и возвращает его с сгенерированным ID</response>
        [HttpPost]
        [ProducesResponseType(typeof(Event), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<Event> CreateEvent(Event eventItem)
        {
            _eventService.AddEvent(eventItem);
            return CreatedAtAction(nameof(GetEventById), new { id = eventItem.Id }, eventItem);
        }

        /// <summary>
        /// Обновить существующее событие по его уникальному идентификатору ID.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eventItem"></param>
        /// <returns></returns>
        /// <response code="204">Успешно обновляет событие с указанным ID</response>
        /// <response code ="404">Событие с указанным ID не найдено</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult UpdateEvent(Guid id, Event eventItem)
        {
            if (!_eventService.UpdateEvent(id, eventItem))
                return EventNotFound(id);

            return NoContent();
        }

        /// <summary>
        /// Удалить событие по его уникальному идентификатору ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="204">Успешно удаляет событие с указанным ID</response>
        /// <response code="404">Событие с указанным ID не найдено</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult DeleteEvent(Guid id)
        {
            if (!_eventService.DeleteEvent(id))
                return EventNotFound(id);

            return NoContent();
        }

        private ObjectResult EventNotFound(Guid id)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Событие не найдено",
                detail: $"Событие с идентификатором '{id}' не найдено.");
        }

    }
}
