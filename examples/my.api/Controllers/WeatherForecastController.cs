using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using synca.lib.Background;
using Microsoft.Extensions.Caching.Distributed;

namespace my.api.Controllers
{
    /// <summary>
    /// 1) Added [ApiController] to make attribite routing a requirement:
    ///    a)  With this defined, Actions are inaccessible via conventional routes defined 
    ///        by UseEndpoints, UseMvc, or UseMvcWithDefaultRoute in Startup.Configure.
    ///    b) This is added to make this API simply accessible without worring about the 
    ///       conventional route
    ///    c) [host]/WeatherForecast - is the [controller_route]
    ///
    /// 2) Controller is derived from ControllerBase to ensure the source generator
    ///    can consider the actions inside this contoller eligible for the compile-time
    ///    async action generation
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public partial class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        // Must add readonly fields with types IDistributedCache and IBackgroundTaskQueue 
        // from synca.lib.Background namespace and instentiate them in the constructor.
        private readonly IDistributedCache _cache;
        private readonly IBackgroundTaskQueue _queue;

        // Assigning the references for the readonly member from the DI
        // services.AddSyncaDistributed(); is already defined at the Startup.cs -> ConfigureServices
        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IDistributedCache memoryCache,
            IBackgroundTaskQueue queue)
        {
            _logger = logger;
            _cache = memoryCache;
            _queue = queue;
        }

        /// <summary>
        /// Async method generated for this action with route:
        /// [Route("[host]/[controller_route]/async/[action_route]")]
        /// and inclusive of the rest of the defined attributes
        ///
        /// The async action is reachable at the below endpoint:
        /// GET [host]/[controller_route]/async/GetResultGetSome/{guid}
        /// GetResultGetSome is coming from the concatinaton of "GetResult"+{Action_Name}
        /// </summary>
        [HttpGet]
        [Route("Get")]
        public async Task<IActionResult> GetSome()
        {
            await Task.Delay(20000);

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
