# synca 🦁
Using [C# Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/), this framework shall generate actions in the API project to enable in-process, non-distributed [asynchronous request-reply pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/async-request-reply) for actions. 

![synca.lib](https://github.com/IshamMohamed/synca/workflows/synca.lib/badge.svg)

![synca.gen](https://github.com/IshamMohamed/synca/workflows/synca.gen/badge.svg)

## Internals
The generated action that follows async request-reply pattern uses `ConcurrentQueue<Func<CancellationToken, (string, Task<IActionResult>)>>` to get the actual long running action to a queue along with a runtime generated GUID to identify the job, store it in a cache, currently implementing `IMemoryCache` (`AddMemoryCache`, `AddDistributedMemoryCache` and `AddSyncaDistributedSql` are supported. Refer [Service Collections](https://github.com/IshamMohamed/synca/blob/master/README.md#service-collections) section below) and send the reply. More distributed caching machanisms are palnned to be supported in future. 

Below is the sequence of operations:
1) Queue the long running job along with the identifying GUID.
2) Reply the identifying GUID in the location header to user through `Accepted` HTTP response.
3) In-process background job, `QueuedHostedService` dequeues the job and executing it.
4) If there is any `GET` request comes to check the result of the job while it is still processing, the same reply sent in (2) will be served.
5) If there is any `GET` request comes to check the result after job completes - the actual response will be served.

The actual response will be kept in the memory cache for one day since first accessed. 

## Prerequisites for code generation
### .NET 5
- C# Source Generators are only supported in .NET 5. So make sure you've installed .NET 5 SDK to build the project.
### API Project
- Should have project reference to `synca.lib.csproj` project and `synca.gen.csproj` with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.
- `services.AddSynca();` added at the `ConfigureServices()`.
### Controller class
- Is a `partial` class
- Is ending with "Controller" suffix.
- Is derived from `ControllerBase`.
- Has `private readonly IMemoryCache` and `private readonly IBackgroundTaskQueue` fields from `synca.lib.Background` declared and instentiated in the class constructor.
### Action
- Has the return type as `async Task<IActionResult>`.
- Has `[Route]` attribute defined.

## Usage
For all actions having `[Route]` attribute under any controllers ending with `Controller`, derived from `ControllerBase`, an async method shall be generated. 
### Accessing the generated async action
The generated async action shall be called with the respective HTTP verb used in the original action and the original controller route, along with an additional `async/` prefixed to the action route. So if the original long running action is servered at `[host]/api/my_controller/my_action` as a `GET` call, considering `api/my_controller` is the controller route and `myaction` is the action route: the async action shall be accessed at `[host]/api/my_controller/async/my_action` as a `GET` call.
### Getting the response
An additional action is generated to view the response of the original action exection. This shall be accessed at `[host]/[controller_route]/GetResult{original_action_method_name}/{GUID}` as a `GET` call. In the above mentioned example, if the original action method name is `MyActionMethod` and the generated GUID is, 670bf3d2-32ae-4464-bf8f-876790701cf3: the location to check the response is `[host]/api/my_controller/getresultmyactionmethod/670bf3d2-32ae-4464-bf8f-876790701cf3`. 
### Service Collections
synca 🦁 uses the following service collections:
- `AddSynca` - Provides standard synca support in memory cache.
- `AddSyncaDistributed` - Provides standard synca support in distributed memory cache.
- `AddSyncaDistributedSql` - Provides standard synca support in distributed SQL Server cache. `SqlServerCacheOptions` must be provided.
