# DLR Repository Layout Alignment

This document records the Phase 1 decisions for aligning the DLR repository
layout with the repository bucket structure used by IronPython after the
January 2025 refactor.

## Scope

All changes in this effort must stay inside the DLR submodule root:

- `/Users/pawel/Code/ironlang/ironpython3.worktrees/ipy-dlr/src/dlr`

No modifications are allowed outside that path as part of this change.

## Target Layout

The adopted target layout is:

| Current path | Target path |
| --- | --- |
| `Src/` | `src/core/` |
| `Tests/` | `tests/` |
| `Docs/` | `docs/` |
| `Build/` | `eng/` |
| `Package/nuget/` | `eng/package/nuget/` |
| `Samples/` | `src/samples/` |

The root entry points remain at repository root:

- `Build.proj`
- `make.ps1`
- `Dlr.slnx`
- `Directory.Build.props`
- `CurrentVersion.props`
- `README.md`
- `LICENSE`
- `.github/`

The generated package output remains under the root `Package/` directory.
Only tracked packaging inputs move under `eng/package/nuget/`.

## Included Work

Later phases may update:

- directory names and tracked repository structure
- MSBuild path references
- solution and project paths
- CI template paths
- repository documentation that refers to moved directories

## Excluded Work

This effort does not include:

- namespace changes
- assembly identity changes
- package identity changes
- solution filename changes
- Markdown conversion of the legacy Word and PDF documentation files
- modifications outside the DLR submodule

## Legacy Areas Kept Out Of Scope

The following areas are explicitly excluded from active rehabilitation in this
change unless a later follow-up change says otherwise:

- `Tests/HostingTest/`
- files under `Samples/`

The `Samples/` directory is still planned to move to `src/samples/`, but file
content updates inside that tree are deferred to a separate cleanup effort.

## Execution Constraints

- Use Git-driven moves for directory renames.
- For case-sensitive rename problems on macOS and Windows, use a temporary
  intermediate path before the final destination.
- Keep the IronPython super-repository submodule mount path as `src/dlr`.
- Do not change the parent repository `.gitmodules` entry as part of this work.

## Phase 1 Outcome

Phase 1 is complete when later implementation phases use this document as the
authoritative scope and path-mapping contract for the DLR repository layout
alignment.
