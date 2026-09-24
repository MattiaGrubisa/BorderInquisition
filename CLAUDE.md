# Border Inquisition

Turn-based multiplayer strategy game in Unity 6000.3.18f1 (URP 2D, UI-heavy). Risk/Catan inspired.
All gameplay logic is currently local (hotseat) — no networking layer yet.

Unity project root is `Border Inquisition/`, code lives in `Border Inquisition/Assets/Scripts/`.
Scenes in `Assets/Scenes/`: `Bootstrap` (persistent), `MainMenu`, `Lobby`, `WorldMap` (the map being
built), `GameOver`. All must be in Build Profiles with `Bootstrap` first.

## Build / run

No CLI build. Open `Border Inquisition/` in Unity 6000.3.18f1 and press Play from whichever scene is
open — `Editor/PlayFromBootstrap` redirects play mode to `Bootstrap`, because nothing runs without
the `GameStateMachine` that only `Bootstrap` holds. Toggle it under
`Border Inquisition > Always Play From Bootstrap`. Compilation errors show up in the Unity console.
There are no tests and no test framework set up.

Code can be type-checked without opening Unity: an SDK-style csproj outside the project, targeting
`netstandard2.1` with `UNITY_EDITOR` defined, compiling `Assets/Scripts/**/*.cs` against
`<unity>/Editor/Data/Managed/UnityEngine/*.dll` plus these from `Library/ScriptAssemblies`:
`UnityEngine.UI`, `Unity.TextMeshPro`, `Unity.InputSystem`, `Unity.RenderPipelines.Universal.Runtime`,
`Unity.RenderPipelines.Core.Runtime`. This catches compile errors only — nothing about scenes.

## Editor tools (`Assets/Scripts/Editor`, menu `Border Inquisition`)

- **Create Flow Scenes** — scaffolds `Bootstrap`, `MainMenu`, `Lobby` and `GameOver`, adds the
  `WorldMap` manager object and HUD, wires every view through `SerializedObject` and writes the build
  scene list. A scene that already exists is skipped, so re-running never overwrites hand-made work.
- **Create Countries** — spawns the 36 named territories under six continent parents, assigns ids and
  fills `GameController._countries`. Refuses to run if the scene already holds countries.
- **Link / Unlink Selected Countries** (`Ctrl+Shift+L` / `Ctrl+Shift+U`) — borders the active
  (last-clicked) country to the rest of the hierarchy selection, refusing duplicates in either
  direction. Borders and countries are also drawn as scene gizmos.
- **Always Play From Bootstrap** — toggles the play-mode redirect.
- Right-click the `GameController` component for **Assign Country Ids**, **Validate Map** and
  **Debug: Attack A Random Target** (play mode only).

## Architecture

**State machines drive the game flow.** `Gameplay.Helpers.StateMachine` is a plain C# class holding
one `IState`; each state gets a back-reference via `IState.StateMachine` and signals it is done with
`StateMachine.OnCompleted(this)`. The parent listens to `StateMachine.Completed` and maps the
finished state to the next one — transitions live in the parent, never in the state.

- `ChangeState` is deferred while a state is entering or updating and applied once that call
  returns, so a state may complete from `OnEnter`/`OnUpdate` without re-entrant transitions.
- `Stop()` exits the current state; a parent with a nested machine calls it from its own exit.
- `StateChanged` fires after a new state has entered (used to publish the current phase).
- A state with more than one exit exposes a `Result` enum that the parent reads in its
  `Completed` handler (`MainMenuState`, `LobbyState`, `GameOverState`).

**Outer flow** (`GameStateMachine`, Singleton in the `Bootstrap` scene):
MainMenu → Lobby (or Quit); Lobby → InGame (or back to MainMenu); InGame → GameOver;
GameOver → MainMenu (or rematch → Lobby).

**Scenes are owned by states.** Outer states derive from `SceneState`, which additively loads the
state's scene on enter, makes it the active scene, and unloads it on exit. Subclasses override
`OnSceneLoaded` / `OnSceneUpdate` / `OnSceneExit`; `IsSceneLoaded` is false until the scene is ready
and after exit, and every UI command checks it so a stray click on an inactive state is ignored.

**In-game phases.** `InGameState` owns a nested machine with three private phases deriving from
`Phase`: `FirstPhase` (`TurnPhase.Income`: queues, then the turn's d9 roll), `SecondPhase`
(`Attack`), `ThirdPhase` (`Build`). Every phase waits for End Phase; the 3 → 1 transition calls
`GameController.NextPlayer()`. A match starts in `InGameState.OnSceneLoaded` via
`GameController.StartNewMatch(settings)`, and ends when `GameController.MatchWon` fires.

**UI talks to the flow only through `GameStateMachine`.** Views in `UI` (`MainMenuView`,
`LobbyView`, `InGameHud`, `GameOverView`) call its command forwarders (`Play`, `Quit`,
`StartMatch`, `LeaveLobby`, `EndPhase`, `Rematch`, `ReturnToMainMenu`) and read `PhaseChanged` /
`CurrentPhase` / `Winner`; they never touch the state classes.

**Singletons** derive from `Gameplay.Helpers.Singleton<T>`; they override `protected virtual void
Awake()` and must call `base.Awake()` first. They are scene-enforced (duplicates destroy themselves,
no `DontDestroyOnLoad`): `GameStateMachine` survives because `Bootstrap` is never unloaded,
`GameController` lives in `WorldMap` and is recreated with it.

**Managers** (`Gameplay.Managers`): `GameController` holds the countries, the per-match player list,
turn order (`DetermineStartingPlayer`, `NextPlayer` — skips eliminated players), income, the map
graph and attack resolution (`CanAttack`, `AttackTargets`, `TryAttack`, `CanMoveArmy`,
`TryMoveArmy`).

**The map is a graph.** `Country` carries a stable `_id` and a `_borders` list authored one way only;
`MapGraph` — a plain C# class owned by `GameController` and baked in `Awake` — mirrors every edge
into symmetric id-based adjacency, so gameplay and the future network layer pass ids instead of
object references. It warns about missing or duplicate ids and borders pointing off the map, and
`ConnectedGroups()` reports whether the map is one reachable whole: six continents joined by sea
links, and a match cannot be won while any group is cut off. Gameplay asks the graph, never the
transforms.

**`View`** holds presentation that is not UI. `MapCamera` sits on the map sprite in `WorldMap`, finds
the Bootstrap camera through `Camera.main` (cross-scene references are impossible), and does
drag-pan, wheel-zoom towards the cursor and clamping so the view never leaves the map bounds. It
computes and sets the camera size itself on enable, from the sprite bounds and the current aspect.

**Players** are plain C# objects (`Gameplay.Player`), created per match from `MatchSettings`
(lobby output, 4–6 players, named by seat for now). Presentation data (colour etc.) belongs to the
view layer. Ownership is one-way: `Country.Owner` is the source of truth, `Player.OwnedCountries`
is derived from it; change it with `Country.SetOwner`.

**Structure is separate from presentation.** Gameplay code reads country state only; world
positions, sprites and UI are a separate layer on top. Do not put rendering decisions in `Gameplay`.

## Gameplay rules worth knowing

- Match setup: countries are shuffled and dealt round-robin (equal share ±1); the starting player
  is decided by roll-offs (`DetermineStartingPlayer`).
- Win condition: one player owns every country (checked after each conquest).
- `GameResources` is a struct (food/wood/gold/stone) with `+`, `-`, `>=`, `<=` operators. It is passed
  `ref` into countries, which spend from and add to the owning player's pool.
- Income is the Catan model: at the start of each player's turn one d9 is rolled, and every country
  whose `_nationDiceNumber` matches pays out `_baseResourceGain` plus its built buildings'
  `ProductionBoost`, for all players at once.
- `Dice.RollDice(minNumber)` rolls 1-9 with the floor raised by a bonus. Combat passes an army-power
  bonus derived from `log(ArmyPower)` — a diverse, larger army rolls from a higher floor.
- Combat (`Combat.AttemptAttack`) is Risk-style: each side rolls one die per *unique unit type*
  (Knight/Horseman/Archer), dice are paired highest-to-highest, ties go to the defender. **Deliberate
  deviation from Risk:** surplus dice on the attacking side count as automatic wins. `CombatResult`
  returns the dice unsorted (for display) plus a `AttackerWins[]` array.
- Losses are applied by `Army.RemoveRandomUnit`, weighted by how numerous each type is.
  A country with no army left is conquered (`GameController.TryAttack`).
- An attack is legal between neighbours whose owners differ, from a country the current player owns
  that still has an army. Attacks are unlimited per turn; the phase only ends on End Phase.
- On conquest the whole surviving attacking army occupies the conquered country and the attacking
  country is left empty — the force that won the ground holds it. Everything else is moved by hand
  with `TryMoveArmy`, between neighbouring countries the player already owns.
- One building per turn per country, each building unique per country; the building and training
  queues are processed in phase one and silently skipped when unaffordable.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Namespaces mirror folders: `Gameplay`, `Gameplay.Managers`, `Gameplay.Helpers`, `GameStates`,
  `UI`, `View`, `Diplomacy`, `Editor`. No assembly definitions for game code (only the Better
  Hierarchy plugin has one); `Assets/Scripts/Editor` is editor-only by folder name as well as by
  `#if UNITY_EDITOR`.
- Editor-only helpers go behind `#if UNITY_EDITOR`.
- Keep code and comments in English; discussion with the user may be in Croatian.
- Do not edit `.meta` files or anything under `Library/`, `Temp/`, `obj/`, or `Assets/Plugins/`.

## Handoff — resume here (2026-09-24)

All five scenes exist (generated by **Create Flow Scenes**), the map art is in
(`Assets/Art/WorldMap.jpg`, 1264×843, 3:2 — use 100 pixels per unit) and the 36 countries are
generated. Nothing has been **play-tested** yet.

The user is placing the countries over the map and authoring their borders by hand with
**Link Selected Countries**. Everything downstream waits on that: until `Validate Map` reports one
connected group of 36, the graph is empty, no attack is legal and the win condition cannot trigger.
Do not assume the map is wired — ask.

Once it is wired:
1. Play from `Bootstrap`: menu → lobby → match, End Phase cycles phases and players.
2. In play mode, right-click `GameController` → **Debug: Attack A Random Target** repeatedly until
   one player owns everything. That is the first path to `GameOver`.
3. Selecting countries with the mouse needs colliders (a `PolygonCollider2D` per region, drawn when
   the user does the art). A picker in `View` driving `CanAttack` / `AttackTargets` is the step after
   that, and it is what finally gates attacks to the attack phase.

Open question never answered: country distribution is shuffle + round-robin deal (equal share ±1);
confirm that is what "the dice decides" meant, versus a literal per-country roll.

## Known open threads

- Borders are not authored yet, so `MapGraph` currently bakes an empty graph.
- `SecondPhase` / `ThirdPhase` run no logic of their own — both only wait for End Phase. Unlimited
  attacks per turn is deliberate, so the attack phase needs no bookkeeping, but nothing yet stops an
  attack during the wrong phase: `GameController` is deliberately phase-blind and gating belongs to
  the view layer.
- Nothing in play mode can trigger an attack except the debug context menu.
- Countries start with whatever army is set in the inspector; an empty army is instantly conquerable.
- Occupying a conquered country empties the attacking one, so a counter-attack can walk straight
  back in. `TryMoveArmy` is the intended answer and has no UI.
- Costs are spent immediately per queue entry; accumulating the full cost before spending is planned.
- Phase one does not wait on animations yet (dice/income animations are TODO).
- `MapCamera` zooms even while the cursor is over the HUD; it needs an
  `EventSystem.IsPointerOverGameObject` check once the UI grows.
- `Market`, `Alliance`, `DiplomacySystem`, `FogOfWar` are empty placeholder classes.

**Planned, not implemented:** fog of war as depth-1 traversal from owned countries — the graph is in
place, `FogOfWar` is still empty. The map is fixed and hand-authored: 36 territories across six
continents (Vargmark, Zlatokraj, Higanshu, Aureliana, Kanembara, Ashqaran — each named from a
different real-world tradition, listed in `Editor/MapSetup.cs`), not procedural.
