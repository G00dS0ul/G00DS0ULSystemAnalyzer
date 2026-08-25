using Microsoft.AspNetCore.Mvc;
using GSSystemAnalyzer.Interfaces;
using GSSystemAnalyzer.Models;

namespace GSSystemAnalyzer.Controllers
{
    [ApiController]
    [Route("api/watcher")]
    public class WatcherController : ControllerBase
    {
        private readonly IWatcherEventLogService _watcherLogService;

        public WatcherController(IWatcherEventLogService watcherLogService)
        {
            _watcherLogService = watcherLogService;
        }

        [HttpGet("log")]
        public IActionResult GetLog([FromQuery] int limit = 200, [FromQuery] WatcherChangeKind? kind = null)
        {
            var events = _watcherLogService.GetEvents(limit, kind);
            return Ok(events);
        }

        [HttpDelete("log")]
        public IActionResult ClearLog()
        {
            _watcherLogService.Clear();
            return NoContent();
        }
    }
}
