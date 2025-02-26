using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace emc_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly string _dir;

        public SettingsController(IConfiguration configuration)
        {
            _dir = configuration.GetValue<string>("data:dir") ?? string.Empty;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var filePath = Path.Combine(_dir, "settings.json");
            if (!System.IO.File.Exists(filePath))
                return NotFound("Settings file not found");

            var jsonText = System.IO.File.ReadAllText(filePath);
            using var jsonDoc = JsonDocument.Parse(jsonText);
            var rootClone = jsonDoc.RootElement.Clone();
            return Ok(rootClone);
        }
    }
}
