using CryptoPriceTraker.Modules.Track.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoPriceTracker.Api.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICryptoPriceService _cryptoPriceService;

        public HomeController(ICryptoPriceService cryptoPriceService)
        {
            _cryptoPriceService = cryptoPriceService;
        }

        public async Task<IActionResult> Index([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var latestPrices = await _cryptoPriceService.GetLatestPricesAsync(pageNumber, pageSize);
            return View(latestPrices);
        }
    }
}