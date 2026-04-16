# StS2 Portrait Mod Generator

A toolchain for assembling Slay the Spire 2 portrait replacement mods from one or more `.pck` source packages.

The tool imports multiple packs, matches their images against the official card index, lets you resolve cross-pack conflicts in a GUI, and then builds a single integrated mod.

## Current capabilities

- Import one or more `.pck` files into the same session (button or drag-and-drop).
- Per-package GDRE recover, asset scan and mapping analysis against the bundled official card index.
- Session-level merge: identical `cardId` candidates from different packages form a conflict group.
- Mapping review window for inspecting, reassigning or discarding individual candidates.
- Conflict review window for picking the winning source per contested `cardId`.
- Build window that runs the template generation, copies portraits, writes `card_replacements.json` and invokes `dotnet build` to produce the final mod artifacts.

## Screenshots

Mapping review (main window), used to inspect per-package candidates, manually reassign cards and discard noise:

![Mapping Review](docs/images/main_window.png)

Conflict review, used to choose one source when several packages provide a portrait for the same `cardId`:

![Conflict Review](docs/images/conflict_window.png)

Build window, used to fill in mod metadata and trigger the final build:

![Build Mod](docs/images/build_window.png)

## Typical workflow

1. Launch [PortraitModGenerator.Gui](tools/PortraitModGenerator.Gui/).
2. Click **Import PCK** (or drag a `.pck` onto the window) to add packages to the session. Each package is recovered, scanned and analyzed independently.
3. In the main mapping review window, walk through candidates: confirm auto-matches, manually pick a card for unmatched assets, or mark noise as discarded.
4. Click **Open Conflicts** to resolve any `cardId` that has multiple candidates across packages. Each conflict group offers a default selection that you can override.
5. Click **Build Mod**, fill in mod metadata (id, name, author, description) and the artifact output directory, then **Build Mod** to produce the final `.dll` / `.json` / `.pck` set.

## Repository layout

- [templates/PortraitReplacementTemplate/](templates/PortraitReplacementTemplate/) — template mod project that the generator instantiates per build.
- [tools/PortraitModGenerator.Core/](tools/PortraitModGenerator.Core/) — core services: PCK import, asset scan, mapping analysis, merge, conflict resolution, materialization, build.
- [tools/PortraitModGenerator.Cli/](tools/PortraitModGenerator.Cli/) — CLI entry point for scripting individual stages.
- [tools/PortraitModGenerator.Gui/](tools/PortraitModGenerator.Gui/) — WinForms UI that wraps the full pipeline.
- [data/official_card_index.json](data/official_card_index.json) — bundled baseline of authoritative `cardId` values.
- [gdre/](gdre/) — bundled GDRETools used for `.pck` recovery.
- [docs/](docs/) — design notes and screenshots.

The GUI persists work under `cache/sessions/<timestamp>_<label>/`, with one subdirectory per imported package and a merged session JSON. Final mod artifacts are written under `artifacts/<ModId>/` by default.

## CLI

The CLI mirrors the underlying pipeline stages, useful for scripting or debugging:

- `generate-template` — instantiate the template into a target directory with `--mod-id` and other metadata tokens.
- `import-pck` — extract a single `.pck` via GDRETools.
- `scan-assets` — scan a recovered directory for image assets and emit `asset_scan_result.json`.
- `analyze-mappings` — match scanned assets against the official card index and emit `mapping_analysis_result.json`.
- `materialize-mappings` — copy selected portraits and write `card_replacements.json` into a generated mod project.

Run any command with `--help` for argument details.

## Build notes

- All projects target `net10.0`.
- The GUI is a Windows-only WinForms app.
- The template mod project depends on the user's local Slay the Spire 2 install for `sts2.dll` and Godot data — see [Sts2PathDiscovery.props](templates/PortraitReplacementTemplate/src/Sts2PathDiscovery.props).
- For an outline of what should and should not be bundled when shipping the tool to end users, see [docs/DEPENDENCY_BUNDLING.md](docs/DEPENDENCY_BUNDLING.md).

## Documentation

- [docs/MOD_GENERATOR_DESIGN.md](docs/MOD_GENERATOR_DESIGN.md) — original generator architecture design (template/generator split).
- [docs/MULTI_PCK_INTEGRATION_DESIGN.md](docs/MULTI_PCK_INTEGRATION_DESIGN.md) — multi-pack session, merge and conflict-resolution design (now implemented).
- [docs/OFFICIAL_CARD_INDEX.md](docs/OFFICIAL_CARD_INDEX.md) — notes on the bundled official card index dataset.
- [docs/DEPENDENCY_BUNDLING.md](docs/DEPENDENCY_BUNDLING.md) — what to include when packaging the tool for distribution.

## Repository hygiene

Generated and local-only content is not committed:

- `cache/`
- `artifacts/`
- `generated/`
- `bin/`, `obj/`
- IDE caches

The template under [templates/PortraitReplacementTemplate/](templates/PortraitReplacementTemplate/) stays clean and minimal — it is the source of truth for what every generated mod looks like.
