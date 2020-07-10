# synca
Asynchronous API action generator for C# API projects. Using [C# Source Generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/), this framework shall generate actions in the API project to enable [asynchronous request-reply pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/async-request-reply) for actions.

## Prerequisite for code generation
### API Project
- Should have project reference to `synca.lib.csproj` project and `synca.gen.csproj` with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.
- Should have `<LangVersion>preview</LangVersion>` in the .csproj file.
- `services.AddSynca();` added at the `ConfigureServices()`.
### Controller class
- Should end with "Controller" suffix.
- Should be derived from `ControllerBase`.
- Should have `private readonly IMemoryCache` and `private readonly IBackgroundTaskQueue` fields from `synca.lib.Background` declared and instentiated in the class constructor.
### Action
- Should have the return type as `async Task<IActionResult>`.
- Should have `[Route]` attribute defined.
