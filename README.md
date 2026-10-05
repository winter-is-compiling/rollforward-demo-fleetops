# FleetOps demo

A small multi-project .NET solution (domain, application, Azure adapters, minimal API) used as a realistic target for [Eolup](https://github.com/winter-is-compiling/eolup), a continuous EOL upgrade tool.

It targets `net8.0` on purpose: Eolup scans it, upgrades it hop by hop, and opens a PR only when the existing tests prove the upgrade is safe. See [pull request #1](https://github.com/winter-is-compiling/eolup-demo-fleetops/pull/1) for the result. (The project was called Rollforward until v0.3.0, so that PR is labelled with the old name.)

- Patterns: CQRS handlers, Repository, Specification, Strategy/Composite, Decorator
- Azure: Blob Storage, Service Bus, Key Vault (behind ports; tests need no Azure account)
- Tests: xUnit, Moq, FluentAssertions

```bash
dotnet test
```
