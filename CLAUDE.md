# Border Inquisition

Turn-based multiplayer strategy game in Unity 6000.3.18f1 (URP 2D, UI-heavy). Risk/Catan inspired.
All gameplay logic is currently local — no networking layer yet.

Unity project root is `Border Inquisition/`, code lives in `Border Inquisition/Assets/Scripts/`.
Scenes: `Assets/Scenes/WorldMap.unity` (the map being built), `SampleScene.unity`.

## Build / run

No CLI build. Open `Border Inquisition/` in Unity 6000.3.18f1 and press Play; compilation errors
show up in the Unity console. There are no tests and no test framework set up.

## Architecture

**State machines drive the game flow.** `Gameplay.Helpers.StateMachine` is a plain C# class holding
one `IState`; each state gets a back-reference via `IState.StateMachine` and signals it is done with
`StateMachine.OnCompleted(this)`. The parent listens to `StateMachine.Completed` and maps the
finished state to the next one — transitions live in the parent, never in the state.

- `GameStateMachine` (Singleton MonoBehaviour) — outer flow: MainMenu → Lobby → InGame → GameOver.
- `InGameState` owns a nested machine with three private phase states: `FirstPhase` (income,
  building/training queues), `SecondPhase` (attacks), `ThirdPhase` (building). Phases 2 and 3 are
  still empty stubs.

**Singletons** derive from `Gameplay.Helpers.Singleton<T>`; they override `protected virtual void
Awake()` and must call `base.Awake()` first. They are scene-enforced (duplicates destroy themselves,
no `DontDestroyOnLoad`).

**Managers** (`Gameplay.Managers`): `GameController` holds the player list, turn order
(`DetermineStartingPlayer`, `NextPlayer`) and attack resolution. `MapGraph` is the map topology.

**Map is a graph.** Countries are nodes, borders are undirected edges. Borders are authored one way
per `Country` (`_borders` list in the inspector) and `MapGraph` bakes them at `Awake` into symmetric
id-based adjacency (`int[][]`), so gameplay and the future network layer pass `Country.Id` (a stable
dense int) instead of object references. Query the graph, never a country's own border list.
Fog of war = `MapGraph.VisibleCountries(player)`, a depth-1 traversal from owned countries.
The map is fixed and hand-authored (~30 territories, 4-6 players), not procedural.

**Ownership** is two-way: `Player.OwnedCountries` and `Country.Owner`. Always go through
`Player.AddCountry` / `RemoveCountry`, which keep both sides in sync — `RemoveCountry` only clears
`Owner` if it still points at that player, so conquest can claim the country before the loser drops it.

**Structure is separate from presentation.** Gameplay code reads the graph and country state only;
world positions, sprites and UI are a separate layer on top. Do not put rendering decisions in
`Gameplay`.

## Gameplay rules worth knowing

- `GameResources` is a struct (food/wood/gold/stone) with `+`, `-`, `>=`, `<=` operators. It is passed
  `ref` into countries, which spend from and add to the owning player's pool.
- Income is the Catan model: one d9 roll per round, every country whose `_nationDiceNumber` matches
  pays out `_baseResourceGain` plus its built buildings' `ProductionBoost`, for all players at once.
- `Dice.RollDice(minNumber)` rolls 1-9 with the floor raised by a bonus. Combat passes an army-power
  bonus derived from `log(ArmyPower)` — a diverse, larger army rolls from a higher floor.
- Combat (`Combat.AttemptAttack`) is Risk-style: each side rolls one die per *unique unit type*
  (Knight/Horseman/Archer), dice are paired highest-to-highest, ties go to the defender. **Deliberate
  deviation from Risk:** surplus dice on the attacking side count as automatic wins. `CombatResult`
  returns the dice unsorted (for display) plus a `AttackerWins[]` array.
- Losses are applied by `Army.RemoveRandomUnit`, weighted by how numerous each type is.
  A country with no army left is conquered (`GameController.IsConquered`).
- One building per turn per country, each building unique per country; the building and training
  queues are processed in phase one and silently skipped when unaffordable.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Namespaces mirror folders: `Gameplay`, `Gameplay.Managers`, `Gameplay.Helpers`, `GameStates`,
  `Diplomacy`. No assembly definitions for game code (only the Better Hierarchy plugin has one).
- Editor-only helpers go behind `#if UNITY_EDITOR` (e.g. `Country.AssignId`,
  `MapGraph.CollectCountriesAndAssignIds`).
- Keep code and comments in English; discussion with the user may be in Croatian.
- Do not edit `.meta` files or anything under `Library/`, `Temp/`, `obj/`, or `Assets/Plugins/`.

## Known open threads

- `SecondPhase` / `ThirdPhase` are empty; the 3 → 1 transition must call `GameController.NextPlayer()`
  or the same player loops forever.
- `FirstPhase` completes immediately; once phases wait on an "End Phase" click (and phase one on its
  animations) the loop settles.
- Costs are spent immediately per queue entry; accumulating the full cost before spending is planned.
- `Market`, `Alliance`, `DiplomacySystem`, `FogOfWar` are empty placeholder classes.
