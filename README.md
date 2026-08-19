# Sloppy Context Actions

Sloppy Context Actions adds compact, hover-aware actions to Unity's Project window. It was generated with the assistance of an LLM and is provided **as is, without support or warranty**. Use it at your own risk and keep the project under version control.

Only Unity 6.5 and newer has been tested so far. Some features rely on Unity Editor internals and may break in another Unity release. If that happens, use Codex or another LLM to adapt the implementation to that Unity version. The same applies to performance: remove actions your project does not use, or ask an LLM to profile and optimize the lookups for your project. Lookups taking at least 250 ms emit a Console error with diagnostic guidance.

## Features

- Folder actions for creating folders, scripts, materials, and shaders.
- Script templates for common C# types, jobs, Entities types, assembly definitions, and optional integrations when their packages are installed.
- Context-aware editor scripts and DOTS Baker creation from compatible source scripts.
- Material creation from folders, textures, and shaders.
- Unity's installed shader-template selection from folder and shader contexts.
- Texture opening in configurable external image editors.
- Audio preview play and stop actions.
- Reveal assets in the system file browser and copy Unity, absolute, or parent paths.
- Project tree actions plus current-folder actions beside the Project breadcrumb area.

Left-click performs the primary action. Right-click opens the applicable choices. Configure button size and external image editors in **Edit > Preferences > Sloppy Context Actions**.

## Installation and customization

Place the complete `Sloppy Context Actions` folder anywhere under `Assets`. The implementation locates its resources by Unity GUID, so moving the folder does not require source changes. Keep Unity's `.meta` files when copying it. Custom script templates can be added under `Editor/ScriptTemplates`, following Unity's `Menu Name-DefaultFileName.extension.txt` naming convention.

The source is intentionally included. Treat it as project-local tooling: review it, delete actions you do not need, and adapt it to your project's conventions.

## Licensing and provenance

The generated implementation is distributed under the license in [LICENSE.md](LICENSE.md). Third-party icon credits and licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Sloppy Context Actions is inspired by [**Project Context Actions**](https://assetstore.unity.com/packages/tools/utilities/project-context-actions-267429) by Infinity Code. The asset's features and interactions served as the reference for recreating the idea for Unity 6 and extending it with this project's additional actions and preferences. Credit belongs to Infinity Code for the original concept and prior work.
