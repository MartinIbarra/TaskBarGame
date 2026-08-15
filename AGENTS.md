# AGENTS.md - Taskbar Tactics

## Scope and authority

This file applies to the entire repository.

Follow the user's current request first, then this file. Inspect the repository before changing it. Existing code and project structure provide context, but they are not automatically the desired standard. When touched code falls short of these rules, improve it within the requested scope without starting unrelated rewrites.

The goal is a maintainable Unity project that an experienced game developer can understand and edit through both code and the Unity Editor.

## Project baseline

- Product: `Taskbar Tactics`, a 2D idle tactical RPG designed to operate next to the Windows taskbar.
- Engine: Unity `6000.3.8f1`. Keep this exact version unless the user explicitly approves a change.
- Primary target: Windows x64 and Steam.
- Input: keyboard, mouse, and touch through Unity's Input System.
- Supported languages: English and Spanish, with English as the source and fallback language.
- Performance range: never intentionally exceed 144 FPS; support a playable minimum target of 30 FPS. Prefer a configurable 60 FPS default unless the user requests otherwise.
- Priorities: correctness and functionality first, then low memory use and measured performance improvements.
- Planned scene flow: `Bootstrap` -> `Main Menu` -> `Gameplay`, plus an isolated `Test` scene.
- Current migration note: the repository currently starts from `Assets/Scenes/Main.unity`. Do not rename, remove, or split it merely to match the planned scene flow. Introduce the planned scenes as part of an approved feature and preserve a working startup path throughout the migration.

## Required workflow

Before making changes:

1. Read this file and any more specific `AGENTS.md` below the files being changed.
2. Inspect the relevant scenes, prefabs, scripts, assembly definitions, packages, tests, and project settings.
3. Run `git status` and preserve all pre-existing user changes. Never discard or overwrite work that is outside the task.
4. Confirm the task's acceptance criteria. Ask when a missing decision would materially alter architecture, player behavior, save compatibility, assets, or platform behavior.
5. For every significant feature, ask whether the user wants a separate branch before implementation. A significant feature includes a new player-facing system, a save-format change, Steam/platform integration, a scene-flow change, or a broad refactor. Small fixes, documentation, and tightly scoped maintenance do not require this question.
6. Identify any approval-gated files or actions listed below and obtain permission before touching them.
7. Implement the smallest complete solution that leaves the affected area clearer than before.
8. Validate in proportion to risk and report exactly what was checked, what could not be checked, and any remaining limitations.

Do not claim completion based only on code inspection when the behavior requires Unity, a Windows build, Steam, or device-specific verification.

## Approval gates

Ask the user before:

- Creating, deleting, or modifying Unity scenes or prefabs.
- Changing files under `ProjectSettings/`.
- Adding, removing, or upgrading Unity packages, native plugins, SDKs, or other third-party dependencies, including Steam integration.
- Editing package manifests or lock files.
- Making a large migration, mass rename, generated-content rebuild, or architectural rewrite.
- Generating or sourcing art, animation, audio, fonts, icons, or other creative assets.
- Committing, pushing, merging, rebasing, publishing a build, or changing remote state unless the user explicitly requested that action.

Permission for one scoped change is not blanket permission for later unrelated changes.

Scripts and other code may be changed when required by the user's task. Scene, prefab, Project Settings, and package changes still require explicit permission even when they appear to be a natural follow-up.

## Unity Editor-first architecture

Prefer developer-editable Unity objects over hidden runtime construction.

- Keep long-lived, player-facing objects in scenes or prefabs so a developer can find and edit them without reading code first.
- Runtime creation is appropriate for repeated or dynamic instances such as enemies, projectiles, loot, pooled effects, and generated lists. It is not the default for cameras, persistent systems, menus, HUDs, or primary gameplay roots.
- Use code to bind, validate, and operate authored objects. Do not build entire persistent hierarchies from code.
- A bootstrap may initialize services and connect explicit references. It must not silently reconstruct the whole application, replace designer-authored objects, or hide important structure.
- Prefer explicit serialized references or deliberate dependency injection. Avoid `GameObject.Find`, string-based hierarchy paths, scene-wide searches, and service locators for required dependencies.
- If emergency auto-repair is necessary, log a clear warning and keep the expected object visible and editable for the developer.
- Do not hand-edit Unity YAML casually. Preserve file IDs, GUIDs, prefab links, and serialization. Use the Unity Editor or a deterministic Editor tool for non-trivial scene/prefab work, then verify the result in Unity.
- Commit every Unity asset together with its `.meta` file. Never regenerate or copy `.meta` files blindly.

## Scene organization and hierarchy

Scenes must be readable at a glance. Use role-based root objects as appropriate:

```text
Bootstrap
|- Systems
|- Persistence
|- Platform
`- Scene Flow

Main Menu
|- Cameras
|- UI
|- Audio
`- Systems

Gameplay
|- Cameras
|- Gameplay
|  |- Player Party
|  |- Enemies
|  |- Spawners
|  `- Environment
|- UI
|- Audio
`- Systems
```

- Use only the roots a scene actually needs. Empty organizational objects must have reset transforms unless a deliberate transform is documented.
- Name GameObjects by their role in clear English: `Main Menu Canvas`, `Player HUD`, `Enemy Spawner`, `Save Conflict Panel`. Avoid names such as `Object`, `Manager2`, `New Game Object`, or names that expose implementation details without explaining purpose.
- Keep hierarchy depth reasonable. Group related objects, but do not add nesting that provides no ownership or layout value.
- Put gameplay behavior on the logical owner. Put renderers, animators, particles, and cosmetic offsets under a `Visual` child when separation improves editability.
- Separate visual geometry from colliders and gameplay hit areas when they need independent tuning.
- Do not use one giant manager for scene setup, gameplay, persistence, UI, and platform behavior. Split responsibilities into focused components and plain C# services.
- Scenes included in the player flow must be present and ordered correctly in Build Settings. Test-only scenes must not accidentally ship.
- The `Test` scene must be self-contained, clearly labeled as non-production, and safe to open without changing player data.

## Inspector and component design

Human editability is a requirement, not an optional polish step.

- Keep fields private by default and expose designer-editable data with `[SerializeField]`.
- Use `[Header]`, `[Tooltip]`, `[Min]`, and `[Range]` when they materially clarify purpose or valid limits. Do not decorate fields redundantly.
- Expose prefabs, sprites, materials, audio clips, UI controls, cameras, and tunable values instead of hard-coding them.
- Use meaningful field names and units, such as `attackIntervalSeconds` or `movementSpeedPixelsPerSecond`.
- Prefer serializable configuration objects or ScriptableObjects when a coherent set of values is reused. Do not turn every constant into an asset.
- Validate invalid combinations in `OnValidate` where safe, and produce actionable messages for missing required references. Do not mutate unrelated scene content from `OnValidate`.
- Avoid public mutable fields. Expose behavior through methods and state through read-only properties or events.
- Components should have one clear reason to change. If a MonoBehaviour coordinates many unrelated domains, split it as part of the feature that touches those domains rather than performing an unrequested big-bang rewrite.

## Prefabs

- Use prefabs for reusable gameplay entities, UI panels, HUD sections, effects, and other repeated structures.
- Store swap-worthy visual references in the Inspector. Keep logic independent from a specific sprite or animation when practical.
- Prefer prefab variants for intentional variations; avoid duplicated prefabs that will drift.
- Keep nested prefabs understandable and avoid override chains that are difficult to audit.
- Do not unpack a prefab or apply broad overrides without reviewing the impact on every instance.
- A prefab must open without missing scripts or broken references and must not depend on an undocumented scene lookup.

## Code and assembly architecture

Use the existing assembly boundaries where they remain appropriate:

- `TaskbarTactics.Core`: deterministic domain models and gameplay rules. It must remain independent of `UnityEngine`.
- `TaskbarTactics.Content`: ScriptableObject definitions and content mapping; it may depend on Core.
- `TaskbarTactics.Infrastructure`: persistence and external implementations behind Core-facing abstractions.
- `TaskbarTactics.Platform.Windows`: Windows-specific behavior and native interop, guarded for supported Windows targets.
- `TaskbarTactics.Presentation`: MonoBehaviours, views, scene coordination, and UI wiring.
- `TaskbarTactics.Editor`: Editor-only tooling and project automation.

Maintain dependency direction and prevent circular assembly references. Add a new assembly only when it creates a meaningful compile-time or architectural boundary.

### C# conventions

- Use English for namespaces, types, members, filenames, asset names, GameObjects, developer messages, and comments.
- Use `PascalCase` for types, methods, properties, events, and constants; use `camelCase` for parameters, locals, and private fields.
- Keep one primary type per file and match the filename to that type.
- Use namespaces rooted at `TaskbarTactics` and aligned with the assembly and responsibility.
- Prefer composition over inheritance. Use interfaces at real boundaries such as persistence, cloud storage, clocks, and platform APIs.
- Keep methods short and intention-revealing. Extract domain logic from MonoBehaviours into testable plain C# when it has no Unity dependency.
- Use events for state changes and cross-system notifications when a direct reference would create inappropriate coupling. Always unsubscribe symmetrically.
- Avoid global mutable state and new singletons. Any unavoidable process-wide service must have explicit lifecycle, ownership, and test seams.
- Avoid magic strings and numbers. Use constants, enums, serialized settings, localization keys, or content definitions according to ownership.
- Avoid LINQ, allocations, reflection, repeated component lookups, and logging inside hot per-frame paths. These are acceptable outside hot paths when they improve clarity and measurements do not show a problem.
- Use `Update` only for work that genuinely must run every frame. Prefer events, timers, coroutines, or scheduled ticks for idle-game systems.
- Handle expected failures explicitly. Do not silently swallow unexpected exceptions or leave empty catch blocks.
- Comments explain non-obvious reasoning, platform constraints, invariants, and tradeoffs. Do not narrate self-explanatory code.

## Content and assets

The user normally provides creative resources.

- Do not create, generate, download, replace, or stylistically reinterpret art, animation, audio, fonts, icons, or marketing assets unless the user explicitly requests it.
- Before using a new third-party asset, ask permission and record its source, license, version, and attribution requirements.
- Keep imported third-party content isolated under a clearly named vendor/package folder. Do not edit vendor files when an adapter, prefab variant, material override, or wrapper is sufficient.
- Follow the category-oriented top-level layout: `Art`, `Animations`, `Audio` where applicable, `Prefabs`, `Scenes`, `Scripts`, `Localization`, `Tests`, `Editor`, and clearly identified generated or third-party content.
- Within `Scripts`, keep the architectural folders and assembly boundaries described above.
- Treat `Assets/Generated` as generated output. Find and update its source or generator instead of manually changing generated files unless the repository documents the file as intentionally editable.
- Use ScriptableObjects for authored configuration and catalogs, not as mutable runtime save state.
- Preserve stable content IDs. Renaming a display label must not change a persisted identifier.
- Prefer serialized references or Addressables for scalable content. Do not add new `Resources` dependencies without a specific justification; do not bulk-migrate existing `Resources` content outside an approved task.

## UI, resolution, input, and localization

- Use TextMesh Pro for player-facing text unless an existing control requires another solution.
- All player-facing text must use Unity Localization keys. Do not hard-code English or Spanish UI strings in scripts or prefabs.
- Add both English and Spanish entries for new visible text. English is the source and fallback language.
- Use stable semantic localization keys. Keep formatting parameters explicit and avoid building sentences by concatenating translated fragments.
- Use anchors, layout groups, content-size controls, and an intentional Canvas Scaler setup so UI adapts across supported resolutions and aspect ratios.
- Test management and taskbar-strip modes at representative small, standard, ultrawide, and scaled-DPI configurations. Prevent clipping, unreadable text, overlapping controls, and inaccessible buttons.
- Make interactive targets usable with mouse and touch. Do not rely exclusively on hover, right-click, or tiny hit areas.
- Define input in Input Action assets and separate action maps by context. Do not scatter raw key polling across unrelated components.
- Route mouse and touch through pointer-compatible UI/gameplay abstractions when behavior is shared.
- Keep EventSystem ownership clear and ensure only one active EventSystem exists per loaded UI flow.

## Windows taskbar behavior

- Isolate Win32 calls and Windows-only code in `TaskbarTactics.Platform.Windows` or another approved platform boundary.
- Guard native calls with appropriate compilation symbols and keep the Editor usable on non-player paths.
- Do not leak window handles, native structs, or Steam-specific types into Core gameplay code.
- Treat taskbar placement, window styles, topmost behavior, transparency, focus, and DPI as platform services with explicit configuration.
- Do not assume a fixed primary monitor, taskbar edge, work area, or DPI. Account for horizontal and vertical taskbars, multiple monitors, scaling, resolution changes, and Explorer/taskbar restarts when the feature requires them.
- Keep management-window and taskbar-strip behavior separate and test both in a Windows build; Editor behavior alone is insufficient.
- Avoid hard-coded window sizes in platform code when a serialized or central settings object can own them.
- Any click-through, always-on-top, transparent, or focus-stealing behavior must be intentional, reversible, and tested so the game does not block normal desktop use.

## Persistence and Steam Cloud

Player progress must support a local versioned save and Steam Cloud synchronization.

- Keep the local save authoritative enough to play when Steam is unavailable. Cloud failure must not destroy or block access to a valid local save.
- Use a versioned save schema with explicit migrations. Never invalidate existing player progress merely because the in-memory model changed.
- Persist stable IDs and plain data rather than Unity object references.
- Write locally through a temporary file and atomic replacement where supported. Maintain a last-known-good backup and validate data before accepting it.
- Store comparison metadata such as schema version, save revision, UTC modification time, installation/device identifier, and a content hash where appropriate.
- Access Steam through an interface implemented outside Core. The game must remain testable without a live Steam client.
- At startup, compare valid local and cloud saves before writing either one.
- If both copies are equivalent, continue without prompting. If only one valid copy exists, preserve a backup and use it according to the documented recovery flow.
- If valid copies differ and neither can be proven to supersede the other safely, show a localized conflict screen and ask the player which version to keep. Display enough information to make an informed choice, such as timestamp, progression summary, and origin.
- Never resolve a genuine conflict by silently choosing the newest timestamp alone.
- Back up both candidates before applying the player's choice, then upload/download deliberately and verify the resolved result.
- If Steam is offline or synchronization fails, load the local save, communicate status without blocking gameplay, and retry safely later.
- Steam packages, SDKs, App IDs, Cloud configuration, and platform deployment changes require user permission. Never commit credentials or user-specific Steam files.

## Performance and memory

- Clamp supported frame-rate options to 30–144 FPS and keep VSync/frame-cap behavior deliberate and documented.
- Consider a lower background or idle update rate when it does not break simulation timing, input, animations, or Steam behavior.
- Keep deterministic simulation time separate from rendered frame rate. Do not make progression depend on frame count.
- Profile before broad optimization. Use the Unity Profiler, Memory Profiler when available, and a Windows development build for evidence.
- Avoid unbounded collections, repeated asset loads, per-frame garbage, unnecessary texture duplication, and long-lived references to disposable screens or effects.
- Pool objects only when repeated creation is measured or clearly frequent. Do not introduce complex pooling for rare objects.
- Prefer sprite atlases and appropriate import/compression settings when approved assets make this beneficial, while preserving visual quality.
- Functionality has priority over speculative micro-optimization, but known hot-path regressions must not be ignored.

## Testing and verification

A formal coverage target has not been chosen yet. Do not invent one.

- Preserve the existing Edit Mode and Play Mode suites and run the tests relevant to changed behavior when the environment permits.
- For a significant feature, ask whether new automated tests are part of the requested scope until the user establishes a broader testing policy.
- For a bug fix in deterministic Core logic, prefer a small regression test when it is practical and does not require a new testing architecture.
- Never weaken, delete, or bypass a test merely to make a change pass without confirming that the test is obsolete.
- Verify Unity compilation after code changes.
- Verify scene and prefab references after approved serialization changes.
- Verify UI changes in all affected window modes, languages, input methods, and representative resolutions.
- Verify Windows window/taskbar behavior in a Windows standalone build.
- Verify persistence with new save, existing save, corrupted primary, valid backup, offline Steam, cloud-only, local-only, matching copies, and conflict paths whenever that system changes.
- Keep the Console free of new exceptions, missing-reference errors, and avoidable warnings.

## Definition of done

A task is complete only when all applicable statements are true:

- The requested behavior and acceptance criteria are implemented.
- The solution respects assembly boundaries and keeps components focused.
- Important objects and settings are easy to find in the Hierarchy and edit in the Inspector.
- Persistent UI and gameplay structure are authored in scenes or prefabs; runtime creation is limited to genuinely dynamic content.
- No required serialized references, scripts, or assets are missing.
- Player-facing text is localized in English and Spanish.
- Input remains usable with every affected supported device.
- Save compatibility and failure behavior were considered for any state change.
- Frame-rate and memory impact are reasonable for the affected system.
- Unity compiles without new errors, applicable tests pass, and required platform/build checks were performed or explicitly reported as unavailable.
- `git diff` contains only intentional changes, Unity assets include their `.meta` files, and generated/cache/build output is not committed.
- The final report lists changed files, validation performed, approvals or migrations required, and any known limitation.

## Git rules

- This repository does not use Git LFS. Do not introduce it without explicit approval.
- Use basic, non-destructive Git commands to inspect and manage work. Prefer `status`, `diff`, `log`, `branch`, and user-approved `switch`, `add`, `commit`, and `push` operations.
- Ask whether to create a dedicated branch before every significant feature. Suggested names are `feature/<short-name>` and `fix/<short-name>`.
- Do not commit or push unless the user requests it.
- Never use destructive history or worktree commands such as `reset --hard`, forced checkout, clean, rebase, or force-push unless the user explicitly requests the exact operation and target.
- Do not mix unrelated cleanup, formatting, generated files, or user changes into a feature diff.
- Keep `Library`, `Temp`, `Logs`, `UserSettings`, IDE files, test result output, and builds out of version control according to `.gitignore`.
