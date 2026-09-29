# AssisticantMauiAlphaTest

A .NET MAUI sample that combines [Assisticant](https://github.com/Assisticant) view models with an in-memory [Jinaga](https://jinaga.com) fact store. It is a small "Friends" app used to try out the Assisticant 1.6 alpha and its source generators.

## What's inside

| Path | Contents |
|---|---|
| `Friends/` | The sample feature: facts, specifications, service, model, view models, and pages |
| `Patterns/` | Reusable helpers for mutable facts and watches |
| `tests/Friends.Tests/` | Tests for the Friends view models |
| `docs/` | [Building apps with Jinaga and Assisticant](docs/Building-Apps-With-Jinaga-And-Assisticant.md): architecture, patterns, and lessons learned |

## Requirements

- .NET 10 SDK with the MAUI workload (`dotnet workload install maui`)
- Windows, macOS, or Linux, depending on the target platform (Windows, Android, iOS, Mac Catalyst)

## Build and run

```
dotnet build
dotnet run --framework net10.0-windows10.0.19041.0   # on Windows
```

## Packages

- `Assisticant` 1.6.0-alpha.1
- `Assisticant.SourceGenerators` 0.1.0-alpha.1
- `Jinaga` 1.1.54

These are alpha packages, so expect breaking changes.

## License

[MIT](LICENSE)
