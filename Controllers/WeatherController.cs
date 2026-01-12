using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Logging_ELK.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherController : ControllerBase
    {
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(ILogger<WeatherController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            // log thông thường
            _logger.LogInformation("Getting weather data");
            var requestData = new { User = "John", Timestamp = DateTime.Now };
            _logger.LogInformation("Processing request {@RequestData}", requestData);

            using (_logger.BeginScope(new { RequestId = Guid.NewGuid() }))
            {
                _logger.LogInformation("Processing with request ID");
            }

            // Log error
            try
            {
                List<int>? a = null;
                var b = a?.FirstOrDefault() + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weather data");
                throw;
            }

            return Ok();
        }
    }
}
