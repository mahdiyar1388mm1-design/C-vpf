# Nexus Control Center

A Windows-first WPF desktop system-management application built with C#/.NET 10, MVVM-style view models, dependency injection, EF Core, and SQLite.

## Requirements

- Windows 10 or 11 (x64)
- .NET 10 SDK

## Run

```powershell
dotnet restore .\NexusControlCenter\NexusControlCenter.csproj
dotnet run --project .\NexusControlCenter\NexusControlCenter.csproj
```

Build a distributable framework-dependent executable with:

```powershell
dotnet publish .\NexusControlCenter\NexusControlCenter.csproj -c Release -r win-x64 --self-contained false
```

## Included modules

- **Overview:** live CPU, physical memory, system-drive capacity, network throughput, process count, recent activity, device identity, and an updating performance chart.
- **System Monitor:** live Windows kernel/user CPU sampling, `GlobalMemoryStatusEx` memory readings, network-interface counters, system drive usage, and a rolling history chart.
- **Task Manager:** process enumeration, working-set and sampled CPU usage, window/status details, and guarded process termination (including a confirmation dialog and self-termination protection).
- **File Manager:** navigate folders, filter the current folder, inspect type/size/modified time, open files with their registered Windows application, and move to the parent folder.
- **Analytics:** export live session readings as CSV or a generated PDF into the current user's Documents folder.
- **Settings:** switch between light and dark themes; persist theme and toast-notification preferences locally.

Actions and activity events are stored locally in `%LOCALAPPDATA%\NexusControlCenter\nexus.db`; preferences are in `settings.json` beside it. The application does not upload telemetry. Windows APIs and standard .NET system interfaces supply live values; measurements unavailable on non-Windows systems are not fabricated.

## Architecture

`Models`, `Views`, `ViewModels`, `Services`, `Repositories`, `Data`, `Commands`, `Controls`, and `Resources` separate UI, state, system access, and persistence. EF Core creates the SQLite activity store at startup. WPF bindings marshal view-model changes; resource sampling, directory enumeration, and process operations use asynchronous/background work. The custom sparkline control renders its own live chart geometry without a charting dependency.

> This repository's execution environment does not include the .NET SDK or Windows desktop runtime, so a WPF compilation or interactive Windows smoke test cannot be run here. Build and validate on a Windows machine with the .NET 10 SDK.
