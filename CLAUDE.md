# Border Inquisition

Turn-based multiplayer strategy game in Unity 6000.3.18f1 (URP 2D, UI-heavy). Risk/Catan inspired.
All gameplay logic is currently local (hotseat) — no networking layer yet.

Unity project root is `Border Inquisition/`, code lives in `Border Inquisition/Assets/Scripts/`.
Scenes in `Assets/Scenes/`: `Bootstrap` (persistent), `MainMenu`, `Lobby`, `WorldMap` (the map being
built), `GameOver`. All must be in Build Profiles with `Bootstrap` first.

## Build / run

No CLI build. Open `Border Inquisition/` in Unity 6000.3.18f1, open `Bootstrap.unity` and press Play
(starting from any other scene has no `GameStateMachine`); compilation errors show up in the Unity
console. There are no tests and no test framework set up.

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
turn order (`DetermineStartingPlayer`, `NextPlayer` — skips eliminated players), income and attack
resolution.

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
  A country with no army left is conquered (`GameController.HandleAttack`).
- One building per turn per country, each building unique per country; the building and training
  queues are processed in phase one and silently skipped when unaffordable.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Namespaces mirror folders: `Gameplay`, `Gameplay.Managers`, `Gameplay.Helpers`, `GameStates`,
  `UI`, `Diplomacy`. No assembly definitions for game code (only the Better Hierarchy plugin has one).
- Editor-only helpers go behind `#if UNITY_EDITOR`.
- Keep code and comments in English; discussion with the user may be in Croatian.
- Do not edit `.meta` files or anything under `Library/`, `Temp/`, `obj/`, or `Assets/Plugins/`.

## Handoff — resume here (2026-09-22)

Game-flow code (scene-owning states, phases, lobby, UI views) is written and compiles outside Unity
(`dotnet build` against the Unity DLLs), but has **never been run in Unity** — the scenes below do
not exist yet. Next session: walk the user through this setup, then test the flow in Play mode.

1. Create scenes in `Assets/Scenes/` with exactly these names: `Bootstrap`, `MainMenu`, `Lobby`,
   `GameOver` (`WorldMap` exists).
2. Build Profiles: add all five scenes, `Bootstrap` first.
3. `Bootstrap`: empty GameObject with `GameStateMachine`. Keep the only camera here; remove cameras
   from the other scenes.
4. `MainMenu`: Canvas with Play/Quit buttons + `MainMenuView`, wire the buttons.
5. `Lobby`: Canvas with −, +, Start, Back buttons, a TMP text + `LobbyView`.
6. `WorldMap`: GameObject with `GameController`, `Dice`, `Combat` (wire references; `Combat` needs
   its `Dice` too). Canvas with End Phase button, TMP text + `InGameHud`. Countries may stay empty —
   turns still rotate.
7. `GameOver`: Canvas with TMP text, Rematch and Main Menu buttons + `GameOverView`.
8. Every UI scene needs an EventSystem (Unity adds one with the Canvas).
9. Press Play from `Bootstrap`.

Testable now: menu → lobby → match (End Phase cycles phases/players) and Back/Quit. GameOver is not
reachable by play yet (attacks are not wired to phase two).

Open questions for the user:
- Add an editor script that sets `EditorSceneManager.playModeStartScene` to `Bootstrap`, so Play
  works from any open scene? (Offered, not answered.)
- Country distribution is shuffle + round-robin deal (equal share); confirm that is what "the dice
  decides" meant, versus a literal per-country roll.

## Known open threads

- Scenes `Bootstrap`, `MainMenu`, `Lobby`, `GameOver` are created by hand in the editor; `WorldMap`
  needs a `GameController` (with `Dice`, `Combat`, countries) and a canvas with `InGameHud`.
- `SecondPhase` / `ThirdPhase` have no gameplay yet; `HandleAttack` is not reachable from anything,
  so the win condition cannot trigger in play yet.
- Phase one does not wait on animations yet (dice/income animations are TODO).
- Countries start with whatever army is set in the inspector; an empty army is instantly conquerable.
- Costs are spent immediately per queue entry; accumulating the full cost before spending is planned.
- `Market`, `Alliance`, `DiplomacySystem`, `FogOfWar` are empty placeholder classes.

**Planned, not implemented:** the map as a graph — countries as nodes, borders as undirected edges
authored one way per `Country` and baked by a `MapGraph` into symmetric id-based adjacency, so
gameplay and the future network layer pass a stable `Country.Id` instead of object references. Fog
of war = depth-1 traversal from owned countries. The map is fixed and hand-authored (~30
territories), not procedural.
