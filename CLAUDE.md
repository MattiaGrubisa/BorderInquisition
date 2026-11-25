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
- `Docs/Walkthrough.md`: the current code walkthrough: plan, progress, and findings to discuss.
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
drive Unity's bundled compiler (`<unity>` is the `Editor` folder of the install; on the desktop
`D:/Unity/Editor/6000.3.18f1/Editor`):
`<unity>/Data/NetCoreRuntime/dotnet.exe <unity>/Data/DotNetSdkRoslyn/csc.dll @<assembly>.rsp`.
Build **one dll per assembly, in layer order** (see Conventions), each with `-target:library
-define:UNITY_EDITOR`, the `.cs` files its asmdef/asmref folders own (Gameplay excludes
`Gameplay/GameStates`), `-r:` for `<unity>/Data/NetStandard/ref/2.1.0/netstandard.dll`,
`<unity>/Data/Managed/UnityEngine/*.dll`, the dlls already built for the assemblies it references,
and the packages it references from `Library/ScriptAssemblies` (`UnityEngine.UI`,
`Unity.TextMeshPro`, `Unity.InputSystem`; Editor also `Unity.RenderPipelines.Universal.Runtime`,
`Unity.RenderPipelines.Core.Runtime`). One flat build hides layering errors. For a player check,
build the runtime assemblies again without `UNITY_EDITOR` and without the `UnityEditor*` dlls.
Use Windows paths (`C:/...`) for `-out:`.

## Architecture in brief (details in `Docs/Architecture.md`)

- **State machines drive the flow.** `Core.StateMachine` holds one `Core.State` (abstract class,
  virtual `OnEnter`/`OnUpdate`/`OnExit`); a state signals it is done with `Complete()` and the
  **parent** picks the next state (transitions never live in the state). `State.Machine` is set
  only by `StateMachine` (`internal set`). States with several exits expose a `Result` enum.
- Outer flow in `GameStateMachine` (Singleton, Bootstrap): MainMenu → Lobby → InGame → GameOver.
  Outer states derive from `SceneState` and own their scene (additive load on enter, unload on exit;
  its `OnEnter`/`OnUpdate`/`OnExit` are `sealed`, subclasses hook in through `OnSceneLoaded` etc.).
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
- AI: `AI/` (`AiBrain`, `AiProfile`, settings) only decides. `View/AiDriver` plays AI seats through
  the same `GameStateMachine.EndPhase` as the button; it is a component on the `GameController`
  object in WorldMap (the scene owns it; `GameController` no longer adds it).
  `GameController.Simulating` silences presentation during editor simulations.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Singletons derive from `Core.Singleton<T>` and override `OnAwake()` / `OnSingletonDestroy()`.
  `Awake`/`OnDestroy` belong to the base (non-virtual); declaring `Awake` in a subclass hides it
  (warning CS0108) and breaks the singleton. Scene-enforced, no `DontDestroyOnLoad`.
- Namespaces mirror folders: `Core`, `Gameplay`, `SO` (`ScriptableObjects/`), `GameStates`
  (`Gameplay/GameStates/`), `Save`, `AI`, `Audio`, `UI`, `View`, `Editor`.
- **Assembly definitions enforce the layering** (each may reference only what is below it; Unity
  refuses cycles). Bottom to top: `BI.CORE` (`Core/`, references nothing) → `BI.Gameplay`
  (`Gameplay/`, plus `ScriptableObjects/` via `BI.SO.asmref`) → `BI.Save` → `BI.Flow`
  (`Gameplay/GameStates/`, own asmdef inside the Gameplay folder) and `BI.AI` (Gameplay only) →
  `BI.Audio` (Gameplay + Flow, temporary until the audio rework) → `BI.UI` (`UI/`, plus `View/` via
  `BI.View.asmref`; references everything below plus TMP, UGUI, Input System) → `BI.Editor`
  (editor-only, references everything). A new reference that points upwards is a design problem,
  not a missing asmdef entry: move the code or invert the dependency with an event.
- `Assets/Scripts/Editor` is editor-only (`BI.Editor` has `includePlatforms: Editor`); editor-only
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

## Handoff (as of 2026-11-25)

**Current phase: code walkthrough.** The earlier work was rushed for a deadline. The user wants to
go through all the code first (how, why, what to change), then discuss, then polish; assets come
last. Plan, progress and findings live in `Docs/Walkthrough.md`: continue from the first unchecked
block there. Nothing has been walked through yet; start with block 1 (Foundations, now `Core/`).

**Just done (uncommitted until the user commits):** the user's asmdef split (`3f587e3`) did not
build; it now does, with the layering in Conventions. Along the way: `IState` became the abstract
class `State`; `Singleton` uses `OnAwake`/`OnSingletonDestroy` and `Destroy`; GameStates got its
own `BI.Flow`; `AiDriver` moved from `AI/` to `View/`; UI and View are one assembly; namespace
`SObjects` became `SO`; `GameStateMachine.QuitApplication` uses `UnityEngine.Application.Quit()`
(it would have broken a player build). Unity has reimported. `Docs/Architecture.md` and
`Docs/Walkthrough.md` still describe the old layout and need the same update.

**Open from that work:**
- `WorldMap`'s `GameController` object has no `AiDriver` component yet (checked 2026-11-25); until
  the user adds it, AI seats never play. `SceneSetup` adds it for newly built scenes.
- Couplings left for the walkthrough (block 5): `CountryPicker` drives HUD panels, `MapView`
  listens to `InGameHud.TurnBegan`, markers use `UiFactory.ApplyFont`/`ThemedText`, Audio depends
  on Flow for music by stage.
- Suggested, not applied: `StateMachine.OnCompleted` ignoring a state that is no longer current;
  `Assets/csc.rsp` with `-warnaserror+:CS0108` so a subclass `Awake` fails the build; renaming
  `BI.CORE` to `BI.Core` (safe, references use GUIDs).

**State of the code:** everything up to `bde9e13` is committed (raze on conquest, regional trade
ratio, AI simulation / Play Out Match, AI pace, 2–6 players, lobby memory, audio system with empty
slots, simple save + Continue, `UiTheme` with `ThemedText`, then the broken asmdef split). The user
has tested some of it; testing is hard because much polish is missing. Bugs found go into the
walkthrough's fixes list.

**After the walkthrough (agreed):** the developer console (rules below) is one of the polish items.

**Waiting on the user:** audio clips and the audio rework, UI assets (after the walkthrough and
polish), balancing (with the AI simulation), starting armies, a later rethink of the save (notes in
`Docs/Architecture.md` > Save).

**Working mode:** slow. Explain as you go and ask the user questions; do not change code during the
walkthrough unless asked. Changes are made as needed, by opinion or necessity, after discussion.

## In-game developer console

### Purpose
A debug/testing console for forcing game states quickly (spawning units, setting resources, skipping turns, etc.). It is a development tool, not a gameplay feature.

### Architecture rules
- The console is a thin channel, not a system that knows about the game. It has exactly three responsibilities: capture input, parse it into a command name plus arguments, and look the name up in a registry and call the handler.
- The console must NOT reference gameplay systems (PlayerManager, UnitSpawner, Inventory, etc.). No switch statement over command names. Adding a command must never require editing the console itself.
- Dependency direction: gameplay systems depend on the console's small registration interface, never the other way round. Each system registers its own commands at startup.
- Registration should be near-free for the developer. Preferred approach: a custom attribute on a method (e.g. `[ConsoleCommand("spawn")]`) discovered by reflection at startup. Fallback: an explicit `Register(name, handler)` call.
- Output is a separate log sink. The logger is independent of the console, and the console UI only subscribes to it for display. Systems write to the logger, not to the console.

### Parsing
- Commands are `name arg1 arg2 ...`. Support quoted strings with spaces, typed argument conversion (int, float, bool, string, enums), and clear error messages for wrong arity or type.
- Autocomplete and `help` should be generated from the registry, not maintained by hand.

### Multiplayer
- The game is server-authoritative. Console commands that change game state are requests to the server, which validates them. They never mutate state directly on the client.
- Console commands are disabled in release builds or locked behind a debug flag. Cheat commands must not be reachable by players.

### What not to do
- Do not create a manager class that holds references to every system.
- Do not put game logic inside command handlers. A handler should only call the existing public API of the owning system.
- Do not add new commands by editing the console or parser code.