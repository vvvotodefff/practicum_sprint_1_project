using Microsoft.AspNetCore.Mvc;
using System.Net;
using MyWebApiProject;
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

    }
}
