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

        [HttpGet]
        public ActionResult<IEnumerable<Event>> GetEvents()
        {
            return Ok(_eventService.GetEvents());
        }

        [HttpGet("{id:guid}")]
        public ActionResult<Event> GetEventById(Guid id)
        {
            var eventItem = _eventService.GetEventById(id);

            if (eventItem is null)
                return NotFound();

            return Ok(eventItem);
        }

        [HttpPost]
        public ActionResult<Event> CreateEvent(Event eventItem)
        {
            _eventService.AddEvent(eventItem);
            return CreatedAtAction(nameof(GetEventById), new { id = eventItem.Id }, eventItem);
        }


        [HttpPut("{id:guid}")]
        public IActionResult UpdateEvent(Guid id, Event eventItem)
        {
            if (!_eventService.UpdateEvent(id, eventItem))
                return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public IActionResult DeleteEvent(Guid id)
        {
            if (!_eventService.DeleteEvent(id))
                return NotFound();
            return NoContent();
        }

    }
}
