using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using synca.lib.Background;

namespace my.api.Controllers
{
    public class Something
    {

    }

    [ApiController]
    public class IshamController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly IBackgroundTaskQueue _queue;

        public IshamController(IMemoryCache memoryCache,
            IBackgroundTaskQueue queue)
        {
            _cache = memoryCache;
            _queue = queue;
        }
    }
}