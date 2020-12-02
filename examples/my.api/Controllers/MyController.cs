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
    ///    c) [host]/api/example - is the [controller_route]
    ///
    /// 2) Controller is derived from ControllerBase to ensure the source generator
    ///    can consider the actions inside this contoller eligible for the compile-time
    ///    async action generation
    /// </summary>
    [ApiController]
    [Route("api/example")]
    public partial class MyController : ControllerBase
    {
        // Must add readonly fields with types IDistributedCache and IBackgroundTaskQueue 
        // from synca.lib.Background namespace and instentiate them in the constructor.        
        private readonly IDistributedCache _cache;
        private readonly IBackgroundTaskQueue _queue;
        
        // Assigning the references for the readonly member from the DI
        // services.AddSyncaDistributed(); is already defined at the Startup.cs -> ConfigureServices
        public MyController(IDistributedCache memoryCache,
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
            await Task.Delay(20000);
            return Ok("Good");
        }

        /// <summary>
        /// Async method generated for this action with route:
        /// [Route("[host]/[controller_route]/async/[action_route]")]
        /// and inclusive of the rest of the defined attributes
        ///
        /// The async action is reachable at the below endpoint:
        /// GET [host]/[controller_route]/async/GetResultGet/{guid}
        /// GetResultGet is coming from the concatinaton of "GetResult"+{Action_Name}
        /// </summary>
        [HttpGet]
        [Route("MyMethod/{id}")]
        public async Task<IActionResult> Get(int id)
        {
            await Task.Delay(20000);
            return Ok(id);
        }

        /// <summary>
        /// Async method generated for this action with route:
        /// [Route("[host]/[controller_route]/async/[action_route]")]
        /// and inclusive of the rest of the defined attributes
        ///
        /// The async action is reachable at the below endpoint:
        /// POST [host]/[controller_route]/async/GetResultPost/{guid}
        /// GetResultPost is coming from the concatinaton of "GetResult"+{Action_Name}
        /// </summary>
        [HttpPost]
        [Route("MyMethod/{id}")]
        public async Task<IActionResult> Post(int id)
        {
            await Task.Delay(20000);
            return Ok(id);
        }
    }
}