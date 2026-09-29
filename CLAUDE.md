# Disciples Console — agent guide

Console prototype of Disciples II core mechanics. Logic must stay reusable in Unity.

## Read first
- `docs/ROADMAP.md` — current milestone and next unchecked task
- `docs/ARCHITECTURE.md` — layers, Core restrictions, patterns
- `docs/MECHANICS.md` — rules catalogue; items marked **(verify)** need confirmation before coding

## Commands
- Build: `dotnet build Disciples.sln`
- Test: `dotnet test Disciples.sln`
- Run: `dotnet run --project src/Disciples.Tui` (use Windows Terminal for proper UTF-8/colors)

## Hard rules
- `Disciples.Core`: netstandard2.1, C# 9, no dependencies, no Console/IO/Unity. See ADR 0001.
- All in-game text and docs in English.
- C# Microsoft conventions; private fields `_camelCase`; no comments unless logic is non-obvious.
- Balance numbers live in `content/*.json`, not in code.
- New rules in Core get xUnit tests.

## Workflow per task
1. Take the next unchecked item from `docs/ROADMAP.md`.
2. Implement; build and test pass; run the app to check manually when UI is affected.
3. Tick the item in ROADMAP, update MECHANICS/ARCHITECTURE if a rule or structure changed; add an ADR for significant decisions.
4. Commit with Conventional Commits (`feat:`, `fix:`, `refactor:`, `docs:`, `test:`, `chore:`), one logical change per commit.
5. Push only after the user confirms.
