# FleetOps demo

A small multi-project .NET solution (domain, application, Azure adapters, minimal API) used as a realistic target for [Rollforward](https://github.com/winter-is-compiling/rollforward).

It targets `net8.0` on purpose: Rollforward scans it, upgrades it hop by hop, and opens a PR only when the existing tests prove the upgrade is safe.

- Patterns: CQRS handlers, Repository, Specification, Strategy/Composite, Decorator
- Azure: Blob Storage, Service Bus, Key Vault (behind ports; tests need no Azure account)
- Tests: xUnit, Moq, FluentAssertions

```bash
dotnet test
```
