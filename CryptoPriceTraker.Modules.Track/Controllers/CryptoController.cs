using CryptoPriceTraker.Modules.Track.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPriceTraker.Modules.Track.Controllers
{
    [ApiController]
    [Route("api/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly ICryptoPriceService _service;

        public CryptoController(ICryptoPriceService service)
        {
            _service = service;
        }

        /// <summary>
        /// TODO: Implement logic to call the UpdatePricesAsync method from the service
        /// This endpoint should trigger a price update by fetching prices from the CoinGecko API
        /// and saving them in the database through the service logic.
        /// </summary>
        /// <returns>200 OK with a confirmation message once done</returns>
        [HttpPost("update-prices")]
        public async Task<IActionResult> UpdatePrices()
        {
            var assets = await _service.UpdatePricesAsync();

            return Ok(assets);
        }

        /// <summary>
        /// TODO: Implement an endpoint to return the latest prices per crypto asset.
        /// This will allow the frontend to display the most recent data saved in the database.
        /// </summary>
        /// <returns>A list of assets and their latest recorded price</returns>
        [HttpGet("latest-prices")]
        public async Task<IActionResult> GetLatestPrices([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var latest = await _service.GetLatestPricesAsync(pageNumber, pageSize);

            return Ok(latest);
        }
    }
}