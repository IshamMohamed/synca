using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace synca.lib.Background
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(Func<CancellationToken, (string, Task<IActionResult>)> workItem);

        Task<Func<CancellationToken, (string, Task<IActionResult>)>> DequeueAsync(
            CancellationToken cancellationToken);
    }
}