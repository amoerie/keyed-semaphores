# Keyed Semaphores — Agent Guide

## Project overview

**KeyedSemaphores** is a C# library that provides per-key locking primitives to improve parallelism in multithreaded applications. Instead of a single global lock, callers acquire a lock scoped to a string key, so only threads sharing the same key are serialized.

NuGet package: `KeyedSemaphores` — targets .NET 8.0, .NET 9.0, .NET 10.0, and .NET Framework 4.8.1.

## Repository layout

| Path | Purpose |
|---|---|
| `KeyedSemaphores/` | Library source (the published NuGet package) |
| `KeyedSemaphores.Tests/` | xUnit v3 test suite |
| `KeyedSemaphores.Benchmarks/` | BenchmarkDotNet benchmarks |
| `KeyedSemaphores.Samples/` | Usage samples |
| `CHANGELOG.MD` | Version history |
| `publish.ps1` | NuGet publish script |

## Build and test

```bash
# Restore tools (CSharpier formatter)
dotnet tool restore

# Check formatting
dotnet csharpier check .

# Build
dotnet build --configuration Release

# Run tests (all target frameworks)
dotnet test --configuration Release --verbosity normal
```

CI runs on `windows-latest` and executes all four steps above. PRs must pass formatting, build, and tests before merging.

## Code style

- Formatting is enforced by **CSharpier** — run `dotnet csharpier .` to auto-format before committing.
- `TreatWarningsAsErrors` is enabled; fix all warnings.
- Nullable reference types are enabled (`<Nullable>Enable</Nullable>`).
- C# language version is locked to 8.0.

## Key source files

| File | Role |
|---|---|
| `KeyedSemaphores/KeyedSemaphore.cs` | Static entry-point — `LockAsync` / `Lock` / `TryLockAsync` |
| `KeyedSemaphores/KeyedSemaphoresDictionary.cs` | Default implementation; per-key `SemaphoreSlim` with ref-counting |
| `KeyedSemaphores/KeyedSemaphoresCollection.cs` | Array-based implementation; fixed-size, lower allocation |
| `KeyedSemaphores/IKeyedSemaphoresCollection.cs` | Shared interface |

## Making changes

1. Create a feature branch from `main`.
2. Edit source under `KeyedSemaphores/`.
3. Add or update tests in `KeyedSemaphores.Tests/` for every behavioural change.
4. Run `dotnet csharpier .` then `dotnet build --configuration Release` then `dotnet test --configuration Release`.
5. Update `CHANGELOG.MD` with the change and today's date.
6. Open a PR against `main`.

## Testing notes

- Tests are written with **xUnit v3**.
- Code coverage is collected via Coverlet and reported to Codecov on pushes to `main`.
- To run a single test class: `dotnet test --filter "FullyQualifiedName~TestsForKeyedSemaphore"`.

## What this library guarantees

- No two threads ever hold a lock for the same key simultaneously.
- Locks are released deterministically via `IDisposable` (use `using`).
- The dictionary implementation disposes its semaphore when the last holder releases, so memory is bounded.
- The collection implementation uses a fixed array of `SemaphoreSlim` instances; keys hash to a slot, so it is allocation-free on the hot path but may cause false contention between unrelated keys.
