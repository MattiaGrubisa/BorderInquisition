# Border Inquisition

Turn-based multiplayer strategy game in Unity 6000.3.18f1 (URP 2D, UI-heavy). Risk/Catan inspired.
All gameplay logic is local (hotseat); there is no networking layer yet.

Unity project root is `Border Inquisition/`, code lives in `Border Inquisition/Assets/Scripts/`.
Scenes in `Assets/Scenes/`: `Bootstrap` (persistent), `MainMenu`, `Lobby`, `WorldMap`, `GameOver`.
All must be in Build Profiles with `Bootstrap` first.

## Where things are written down

- `CLAUDE.md` (this file): the core, loaded every session. Keep it short.
- `Docs/Architecture.md`: how each system is built (flow, UI, AI, audio, save, map, editor tools,
  scene setup, `CountryPicker`, open threads). Read the section a task touches before changing it.
- `Docs/Rules.md`: the gameplay rules as implemented, and what is the user's to decide.
- `GDD.md` and `Architecture.svg` (repo root, the svg labelled in Croatian): the user's private
  overview. Out of git (`.git/info/exclude`) and **never changed unless the user asks** (user's
  decision, 2026-09-27). If something there looks outdated, mention it at most.

**When a piece of work is finished** (user's standing request): update the `Docs/` file for the
system that changed, **rewrite** the Handoff below so it describes the state now (replace, never
append; git holds the history), then give the user a commit message. The user makes the commit.

## Build / run

No CLI build. Open `Border Inquisition/` in Unity 6000.3.18f1 and press Play from any scene:
`Editor/PlayFromBootstrap` redirects play mode to `Bootstrap`, because nothing runs without the
`GameStateMachine` that only `Bootstrap` holds (toggle: `Border Inquisition > Always Play From
Bootstrap`). There are no tests and no test framework.

Type-check without Unity (compile errors only, nothing about scenes). No .NET SDK is installed, so
drive Unity's bundled compiler:
`<unity>/Editor/Data/NetCoreRuntime/dotnet.exe <unity>/Editor/Data/DotNetSdkRoslyn/csc.dll @build.rsp`
with `-target:library -define:UNITY_EDITOR`, every `Assets/Scripts/**/*.cs`, and `-r:` for
`<unity>/Editor/Data/NetStandard/ref/2.1.0/netstandard.dll`,
`<unity>/Editor/Data/Managed/UnityEngine/*.dll`, and from `Library/ScriptAssemblies`:
`UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.RenderPipelines.Universal.Runtime`,
`Unity.RenderPipelines.Core.Runtime`. Use Windows paths (`C:/...`) for `-out:`.

## Architecture in brief (details in `Docs/Architecture.md`)

- **State machines drive the flow.** `Gameplay.Helpers.StateMachine` holds one `IState`; a state
  signals it is done with `StateMachine.OnCompleted(this)` and the **parent** picks the next state
  (transitions never live in the state). States with several exits expose a `Result` enum.
- Outer flow in `GameStateMachine` (Singleton, Bootstrap): MainMenu → Lobby → InGame → GameOver.
  Outer states derive from `SceneState` and own their scene (additive load on enter, unload on exit).
- A turn is three phases inside `InGameState`: Income (ends itself), Attack, Build & Move.
- **UI talks to the flow only through `GameStateMachine`** command forwarders and events; views
  never touch state classes. In-game panels are built in code (`UiFactory`), not wired in scenes.
- `GameController` (Singleton in WorldMap) owns the match: players, countries, turn order, combat,
  and the per-match plain C# systems (`MapGraph`, `Fog`, `Market`, `Diplomacy`). It is deliberately
  phase-blind; the view-owned turn rules live in `CountryPicker`.
- **The map is a graph.** Gameplay asks `MapGraph` by country id, never the transforms.
- **Structure is separate from presentation.** No rendering decisions in `Gameplay`; `View` holds
  non-UI presentation, `UI` the panels. Gameplay code never calls audio (`View/SoundCues` does).
- `Country.Owner` is the source of truth for ownership; change it with `Country.SetOwner`.
- AI (`AI/`): `AiDriver` plays AI seats through the same `GameStateMachine.EndPhase` as the button;
  `AiBrain` decides. `GameController.Simulating` silences presentation during editor simulations.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Singletons derive from `Gameplay.Helpers.Singleton<T>`, override `protected virtual void Awake()`
  and call `base.Awake()` first. Scene-enforced, no `DontDestroyOnLoad`.
- Namespaces mirror folders: `Gameplay`, `Gameplay.Managers`, `Gameplay.Helpers`, `GameStates`,
  `UI`, `View`, `Diplomacy`, `AI`, `Audio`, `Save`, `Editor`. One exception: `Assets/Scripts/Helpers`
  (StateMachine, IState, Singleton) is in namespace `Gameplay.Helpers`. No assembly definitions for
  game code (only the Better Hierarchy plugin has one).
- `Assets/Scripts/Editor` is editor-only by folder name and by `#if UNITY_EDITOR`; editor-only
  helpers elsewhere go behind `#if UNITY_EDITOR`.
- Code and comments in English; discussion with the user may be in Croatian.
- Do not edit `.meta` files or anything under `Library/`, `Temp/`, `obj/`, or `Assets/Plugins/`.

## Standing decisions

- **Order of work:** polish local hotseat first. The online layer comes only once local play is
  polished; do not start it earlier.
- The numbers (prices, bonuses, building effects, incomes, starting armies) are the user's. Read
  them from the assets; do not pick them. See `Docs/Rules.md` for the full list.
- Starting armies (Risk-style placement turn): the user is still thinking it over; do not start it.
- Hotseat has no handover screen between turns, by decision.

## Handoff (as of 2026-09-29)

**Built, committed, not yet tested in Unity by the user** (up to commit `8a880b7`): raze on
conquest, regional trade ratio, AI simulation / Play Out Match, AI pace, 2–6 players, lobby memory,
audio system (empty slots, the user adds clips), the simple save + Continue, `UiTheme` with
`ThemedText` font scale.

**Uncommitted, the user's to decide:** Unity emptied `preloadedAssets` in `ProjectSettings.asset`
(it listed `InputSystem_Actions`). `Scripts/ScriptableObjects/UiTheme.asset` is an unused duplicate
of `Resources/UiTheme.asset` and can be deleted.

**Next, unless the user says otherwise:**
1. Ask whether the untested work above was tried in Unity, and fix what comes up.
2. Test the save: Continue after leaving mid-match, after quitting Play mode, on a human and an AI
   turn.
3. UI polish: ask how the theme looks and what comes next (panels, frames, scene buttons, marker
   icons). The user is adding assets first.

**Waiting on the user:** audio clips, UI assets, balancing (with the AI simulation), starting
armies, a later rethink of the save (open notes in `Docs/Architecture.md` > Save).

**Working mode:** fast. Build agreed features whole, type-check, list the changed files and give a
commit message. When the user says so, slow down: explain as you go and ask them questions.
