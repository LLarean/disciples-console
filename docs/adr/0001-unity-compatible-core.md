# ADR 0001: Unity-compatible core

**Status:** accepted

## Context
Graphics may later move to Unity; the game logic should be reused as is.

## Decision
- `Disciples.Core` targets `netstandard2.1` with `LangVersion 9.0` — what Unity 2021.2+ / Unity 6 compile.
- Core has no dependencies on console, file system, JSON libraries or Unity.
- The console app and tests use `net8.0` (LTS) and may use newer language features.

## Consequences
- Core is copied or referenced as a DLL / source folder in Unity without changes.
- Some modern C# features are unavailable in Core (see ARCHITECTURE.md).
