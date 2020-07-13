using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using synca.lib.Background;

namespace my.api.Controllers
{
    [ApiController]
    [Route("api/example")]
    public partial class MyController : ControllerBase
    {
        // Must add readonly fields with types IMemoryCache and IBackgroundTaskQueue 
        // from synca.lib.Background namespace and instentiate them in the constructor.
        private readonly IMemoryCache _cache;
        private readonly IBackgroundTaskQueue _queue;

        public MyController(IMemoryCache memoryCache,
            IBackgroundTaskQueue queue)
        {
            _cache = memoryCache;
            _queue = queue;
        }

        /// <summary>
        /// No async action generated for this method 
        /// because this action does not have attribute routing defined
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MyMethod()
        {
            // No async action generated for this method
            System.Threading.Thread.Sleep(5000);
            return Ok("Good");
        }

        /// <summary>
        /// Async method generated for this action with route:
        /// [Route("async/MyMethod/{id}")]
        /// and inclusive of the rest of the defined attributes
        ///
        /// The async action is reachable at the below endpoint:
        /// GET [host]/api/example/async/mymethod/{id}
        /// </summary>
        [HttpGet]
        [Route("MyMethod/{id}")]
        public async Task<IActionResult> DCheckAsyncMyMethod(int id)
        {
            System.Threading.Thread.Sleep(5000);
            return Ok(id);
        }

        /// <summary>
        /// Async method generated for this action with route:
        /// [Route("async/MyMethod/{id}")]
        /// and inclusive of the rest of the defined attributes
        ///
        /// The async action is reachable at the below endpoint:
        /// POST [host]/api/example/async/mymethod/{id}
        /// </summary>
        [HttpPost]
        [Route("MyMethod/{id}")]
        public async Task<IActionResult> Post(int id)
        {
            System.Threading.Thread.Sleep(5000);
            return Ok(id);
        }
    }

    [ApiController]
    [Route("[controller]")]
    public partial class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IBackgroundTaskQueue _queue;


        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IMemoryCache memoryCache,
            IBackgroundTaskQueue queue)
        {
            _logger = logger;
            _cache = memoryCache;
            _queue = queue;
        }

        [HttpGet]
        [Route("Get")]
        public async Task<IActionResult> GetSome()
        {
            System.Threading.Thread.Sleep(20000);

            var rng = new Random();
            return Ok(Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray());
        }
    }
}
