using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ProjectWork.DTO;
using ProjectWork.Services;
using ProjectWork.Models;
using ProjectWork.Exceptions;

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
        /// Получить список событий с возможностью фильтрации по названию и датам, постранично
        /// </summary>
        /// <param name="title">Поиск по названию: без учёта регистра, частичное совпадение</param>
        /// <param name="from">События, которые начинаются не раньше указанной даты</param>
        /// <param name="to">События, которые заканчиваются не позже указанной даты</param>
        /// <param name="page">Номер страницы, начиная с 1</param>
        /// <param name="pageSize">Количество элементов на странице</param>
        /// <returns></returns>
        /// <response code="200">Успешно возвращает страницу списка событий</response>
        /// <response code="400">Некорректные параметры пагинации</response>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<Event>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public ActionResult<PaginatedResult<Event>> GetEvents(
            [FromQuery] string? title,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery][Range(1, int.MaxValue)] int page = 1,
            [FromQuery][Range(1, int.MaxValue)] int pageSize = 10)
        {
            return Ok(_eventService.GetEvents(title, from, to, page, pageSize));
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
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

            return Ok(eventItem);
        }

        /// <summary>
        /// Создать новое событие. ID и количество свободных мест назначает сервер
        /// </summary>
        /// <param name="request">Данные нового события</param>
        /// <returns></returns>
        /// <response code="201">Успешно создает новое событие и возвращает его с сгенерированным ID</response>
        /// <response code="400">Некорректные данные события</response>
        [HttpPost]
        [ProducesResponseType(typeof(EventInfo), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EventInfo>> CreateEvent(CreateEvent request)
        {
            var created = await _eventService.CreateEventAsync(request);

            return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Обновить существующее событие по его уникальному идентификатору ID.
        /// </summary>
        /// <param name="id">Идентификатор события</param>
        /// <param name="request">Новые данные события. Свободные места пересчитывает сервер</param>
        /// <returns></returns>
        /// <response code="204">Успешно обновляет событие с указанным ID</response>
        /// <response code="400">Некорректные данные или мест меньше, чем уже занято</response>
        /// <response code ="404">Событие с указанным ID не найдено</response>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public IActionResult UpdateEvent(Guid id, UpdateEvent request)
        {
            if (!_eventService.UpdateEvent(id, request))
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

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
                throw new NotFoundException($"Событие с идентификатором '{id}' не найдено.");

            return NoContent();
        }
    }
}
