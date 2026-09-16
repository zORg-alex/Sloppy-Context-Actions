# Sloppy Context Actions

Sloppy Context Actions adds compact, hover-aware actions to Unity's Project window. It was generated with the assistance of an LLM and is provided **as is, without support or warranty**. Use it at your own risk and keep the project under version control.

Only Unity 6.5 and newer has been tested so far. Some features rely on Unity Editor internals and may break in another Unity release. If that happens, use Codex or another LLM to adapt the implementation to that Unity version. The same applies to performance: remove actions your project does not use, or ask an LLM to profile and optimize the lookups for your project. Lookups taking at least 250 ms emit a Console error with diagnostic guidance.

## Features

- Folder actions for creating folders, scripts, materials, and shaders.
- Script templates for common C# types, jobs, Entities types, assembly definitions, and optional integrations when their packages are installed.
- Destination-folder namespaces for every generated C# script: `Assets` is omitted, so `Assets/Scripts/Extensions` becomes `Scripts.Extensions`.
- Context-aware editor scripts and DOTS Baker creation from compatible source scripts.
- Optional inline custom inspectors and property drawers, disabled by default because they modify runtime source files.
- Material creation from folders, textures, and shaders.
- Unity's installed shader-template selection from folder and shader contexts.
- URP 17 RenderGraph templates for fullscreen blits, draw-object passes, and Volume components. The Fullscreen Shader Graph action starts with Screen Position feeding the URP Blit Source sample buffer.
- Texture opening in configurable external image editors.
- Reversible editor-texture hiding: original icon sources and meta files can be moved into an Asset Database-ignored `.hidden` folder while the UI keeps loading cached copies.
- Audio preview play and stop actions.
- Reveal assets in the system file browser and copy Unity, absolute, or parent paths.
- Project tree actions plus current-folder actions beside the Project breadcrumb area.

Left-click performs the primary action. Right-click opens the applicable choices. Configure button size and external image editors in **Edit > Preferences > Sloppy Context Actions**.
Each image editor can independently prefer handing files to an already-running instance; support depends on the editor, with a dedicated Aseprite handoff currently included on Windows.

## Hiding editor textures

Right-click the `Icons` folder and choose **Sloppy Context Actions > Editor Textures > Hide** to remove its textures from Unity's Project window and object selectors. The original files and their `.meta` files are moved into `Icons/.hidden`; they are not deleted. A volatile PNG cache generated from Unity's imported textures lives only under `Icons/.hidden/.cache`, is ignored by Git, and lets `EditorTextureStore.GetTexture("filename.svg")` continue loading SVG and raster icons after Unity stops importing that folder. Use **Show** from the same `Icons` folder to restore the original files and GUIDs.

`EditorTextureStore.cs` is intentionally asset-specific. To reuse it in another asset, copy the script and change its hard-coded texture-folder GUID and menu root. Keeping the folder reference as a GUID means the asset itself can still be moved anywhere under `Assets`.

## Installation and customization

### External integrations

Other assets can register editor actions through `SloppyContextActions.Editor.AssetContextActions.Register(id, assetType, button, execute, isAvailable, populateMenu, order)`. Use a globally unique id; registering it again replaces the action. `Unregister(id)` removes it. Actions appear on matching non-folder assets in both Project views, including one-column mode. The host handles button layout, size, tint and mouse input.

Register once after domain reload (`InitializeOnLoad` with `EditorApplication.delayCall`). `execute` receives the clicked `UnityEngine.Object`. An optional `Predicate<UnityEngine.Object>` controls visibility; keep it cheap because it runs during drawing. An optional `Action<UnityEngine.Object, GenericMenu>` builds a custom right-click menu. Without it, right-click offers the primary action. Click execution is deferred outside drawing. Availability checks exceeding 250 ms use the existing optimization diagnostic.

For an optional integration, keep its code in the integrating asset's Editor folder and resolve `Type.GetType("SloppyContextActions.Editor.AssetContextActions, SloppyContextActions.Editor", false)` once during registration. If it returns null, skip registration. Invoke the public `Register` method with standard Unity/System delegate types; no assembly reference or scripting define is required. This lets either asset be removed independently. For a required dependency, call the API directly and reference `SloppyContextActions.Editor` from your editor asmdef.

Place the complete `Sloppy Context Actions` folder anywhere under `Assets`. The implementation locates its resources by Unity GUID, so moving the folder does not require source changes. Keep Unity's `.meta` files when copying it. Custom script templates can be added under `Editor/ScriptTemplates`, following Unity's `Menu Name-DefaultFileName.extension.txt` naming convention.

The source is intentionally included. Treat it as project-local tooling: review it, delete actions you do not need, and adapt it to your project's conventions.

## Licensing and provenance

The generated implementation is distributed under the license in [LICENSE.md](LICENSE.md). Third-party icon credits and licenses are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

Sloppy Context Actions is inspired by [**Project Context Actions**](https://assetstore.unity.com/packages/tools/utilities/project-context-actions-267429) by Infinity Code. The asset's features and interactions served as the reference for recreating the idea for Unity 6 and extending it with this project's additional actions and preferences. Credit belongs to Infinity Code for the original concept and prior work.
