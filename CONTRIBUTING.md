# Contributing to BareMediator

Thank you for your interest in contributing to BareMediator. This repository intentionally targets and requires the .NET 10 SDK. We do not support older SDKs or runtimes. Follow the guidance below to make contributions smooth for everyone.

## Requirements

- Install the .NET 10 SDK and use it for development and CI. Verify with:

```bash
dotnet --info
```

- Use an editor/IDE that supports .NET 10.

## Enforced SDK

To ensure everyone uses the same SDK, add a `global.json` at the repository root. Example:

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

Set the `version` to the exact SDK patch you want to pin. This ensures contributors and CI use the pinned SDK.

## Local workflow

- Restore dependencies: `dotnet restore`
- Build: `dotnet build --configuration Release`
- Run tests: `dotnet test --configuration Release`
- Run formatter: `dotnet tool restore` (if `dotnet-format` used) then `dotnet format`

All commits should compile and tests must pass locally before opening a PR.

## Tests and Coverage

- Tests live in the `tests/` folder. Keep tests fast and focused.
- CI uses `.github/workflows/*` and is configured to run using `dotnet-version: '10.0.x'`.
- We collect coverage in CI (XPlat collector) and upload artifacts. If you change test behavior, update CI workflow accordingly.

## Code style

- Follow existing project conventions and C# idioms used in the repository.
- Prefer small, focused commits. Keep public APIs stable unless introducing a major version.

## Branching and Pull Requests

- Fork the repository or create a branch in this repo for your work. Branch names: `feature/<short-desc>`, `fix/<short-desc>`, or `chore/<short-desc>`.
- Push your branch and open a Pull Request against the `master` branch.
- Provide a clear PR description with motivation and a summary of changes.
- CI must be green before a PR will be merged.

## Commit messages

- Use concise, descriptive commit messages. We recommend following Conventional Commits (e.g. `fix:`, `feat:`, `chore:`), but this is not strictly enforced.

## Code review

- Reviewer approval is required for non-trivial changes.
- Address review comments by pushing follow-up commits to the same branch.

## Issues and bugs

- Before opening an issue, search existing issues and PRs.
- Provide a minimal reproduction, environment details (OS, `dotnet --info`), and steps to reproduce.

## Non-goals

- This repository intentionally targets only .NET 10. Do not add compatibility shims for older .NET versions.
- If you need multi-targeting, maintain it in a separate branch or fork.

## License

By contributing you agree that your contributions will be licensed under the [project license](LICENSE.md).
