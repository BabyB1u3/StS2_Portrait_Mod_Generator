# StS2 Portrait Mod Generator

This repository contains a reusable toolchain for generating new Slay the Spire 2 portrait replacement mods.

## What is in the repo

- `templates/PortraitReplacementTemplate/`
  Clean reusable template for generated mods.
- `data/official_card_index.json`
  Pre-generated official release-card index used as baseline matching data.
- `tools/PortraitModGenerator.Core/`
  Core library for template-based mod generation.
- `tools/PortraitModGenerator.Cli/`
  CLI entry point for import, scan, and generation workflows.
- `docs/MOD_GENERATOR_DESIGN.md`
  Detailed design document for the generator architecture.
- `docs/OFFICIAL_CARD_INDEX.md`
  Notes about the built-in official card index dataset.

## Current direction

The long-term goal is:

1. Import one or more `.pck` files with GDRETools headless CLI.
2. Extract and scan image assets.
3. Normalize names and let the user confirm mappings.
4. Generate a new mod project from the template.
5. Fill `card_replacements.json` and copy selected portraits.
6. Build the generated mod.

## Current status

Already done:

- Extracted a reusable `PortraitReplacementTemplate`.
- Removed the old reference project after extracting the reusable template pieces it was providing.
- Added `PortraitModGenerator.Core` with template generation, GDRE recover import, and asset scanning.
- Added `PortraitModGenerator.Cli` with `generate-template`, `import-pck`, `scan-assets`, and `analyze-mappings`.
- Added a pre-generated `official_card_index.json` baseline for authoritative card ids.
- Added deterministic asset-to-card matching against the built-in official card index.

Not done yet:

- `card_replacements.json` generation from approved mappings
- Portrait copying into the generated mod project
- Candidate mapping review/edit flow
- Mapping editor UI
- End-to-end build pipeline for generated mods

## Build notes

The generator core is a standalone tooling project, while the generated mod template is tied to the Slay the Spire 2 / Godot mod environment.

In the current environment, `PortraitModGenerator.Core` targets `net10.0` so it can build with the locally available SDK/reference packs.

## Repository hygiene

Generated and local-only content should not be committed:

- `.dotnet_cli/`
- `bin/`
- `obj/`
- IDE caches

The template under `templates/PortraitReplacementTemplate/` is intended to stay clean and minimal.
