using Microsoft.AspNetCore.Mvc;
using ShipmentManagement.Services;

namespace ShipmentManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentsController : ControllerBase
    {
        private readonly ILogger<ShipmentsController> _logger;
        private readonly IHillebrandClient _client;

        public ShipmentsController(ILogger<ShipmentsController> logger, IHillebrandClient client)
        {
            _logger = logger;
            _client = client;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? mainModality, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            // validation
            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100) return BadRequest(new { message = "Invalid pagination parameters" });

            try
            {
                var json = await _client.GetShipmentsAsync(mainModality, pageNumber, pageSize);
                // Return raw JSON from Hillebrand (proxy behaviour)
                return Content(json.GetRawText(), "application/json");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error fetching shipments");
                return StatusCode(502, new { message = "Upstream service error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest(new { message = "Invalid id" });

            try
            {
                var json = await _client.GetShipmentByIdAsync(id);
                return Content(json.GetRawText(), "application/json");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Error fetching shipment by id");
                return StatusCode(502, new { message = "Upstream service error" });
            }
        }
    }
}
