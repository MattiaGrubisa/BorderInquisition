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
No .NET SDK is installed, so drive Unity's bundled compiler directly:
`<unity>/Editor/Data/NetCoreRuntime/dotnet.exe <unity>/Editor/Data/DotNetSdkRoslyn/csc.dll @build.rsp`
with `-target:library -define:UNITY_EDITOR`, `-r:` for `<unity>/Editor/Data/NetStandard/ref/2.1.0/netstandard.dll`
plus the dlls above, and every script path. Use Windows paths (`C:/...`) for `-out:`.

## Editor tools (`Assets/Scripts/Editor`, menu `Border Inquisition`)

- **Create Flow Scenes** — scaffolds `Bootstrap`, `MainMenu`, `Lobby` and `GameOver`, adds the
  `WorldMap` manager object and HUD, wires every view through `SerializedObject` and writes the build
  scene list. A scene that already exists is skipped, so re-running never overwrites hand-made work.
- **Create Countries** — spawns the named territories under six continent parents, assigns ids and
  fills `GameController._countries`. Refuses to run if the scene already holds countries. (Already
  run; the scene now has 35 — Lucanor was removed on purpose and is still listed in `MapSetup`.)
- **Set Up Map View** — adds a `MapView` to the map sprite and a default `PolygonCollider2D` to every
  country without a collider. Existing colliders are never touched; the shapes are traced by hand.
- **Always Play From Bootstrap** — toggles the play-mode redirect.
- Countries and their borders are drawn as scene gizmos (`Editor/MapLinker`). Borders are authored
  in the `Country` inspector, one side only.
- Right-click the `GameController` component for **Assign Country Ids**, **Assign Regions** (puts a
  `Region` on each continent parent and links its countries) and **Validate Map**.
- The `GameController` inspector has a **Play Out Match** button (play mode, match running,
  `Editor/GameControllerEditor`): `GameController.PlayOutMatch()` has every player attack until no
  legal attack is left, then marches idle armies one step towards the nearest enemy, turn after turn,
  until someone wins (→ `GameOver`), every army is gone, or 1000 turns pass.

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

**In-game phases.** `InGameState` owns a nested machine with four private phases deriving from
`Phase`. A turn is:
1. `FirstPhase` (`TurnPhase.Income`) — not a phase the player waits in: queues are processed, the d9
   is rolled and paid out, and the phase ends itself from `OnEnter`. The HUD shows the roll.
2. `SecondPhase` (`Attack`) — unlimited attacks. After a **conquest** (only then) the player may move
   units between exactly those two countries, any amount.
3. `ThirdPhase` (`Build`) — queue buildings and soldiers per country.
4. `FourthPhase` (`Move`) — **one** move per turn, between two neighbouring own countries, any amount.

Phases 2-4 wait for End Phase; the 4 → 1 transition calls `GameController.NextPlayer()`. A match starts in `InGameState.OnSceneLoaded` via
`GameController.StartNewMatch(settings)`, and ends when `GameController.MatchWon` fires.

**UI talks to the flow only through `GameStateMachine`.** Views in `UI` (`MainMenuView`,
`LobbyView`, `InGameHud`, `GameOverView`) call its command forwarders (`Play`, `Quit`,
`StartMatch`, `LeaveLobby`, `EndPhase`, `Rematch`, `ReturnToMainMenu`) and read `PhaseChanged` /
`CurrentPhase` / `Winner`; they never touch the state classes. `InGameHud` builds the in-game
panels in code at `Awake` (`UiFactory`, layout groups — no scene wiring): the income die box (top
right), `CombatPanel` (top centre: the last attack's dice sorted and paired, tinted in seat colours; they
tumble, then each pair's loser dims and the outcome appears; never blocks map clicks, hides on phase
change),
`MovePanel` (pick knights/horsemen/archers with +/-/All; the caller performs the move in the confirm
callback) and `CountryPanel` (build phase: queue/unqueue buildings from `GameRules`
and soldiers, costs from the region). `View.CountryPicker` opens them through the HUD. A bottom-left
action bar (hidden in the income phase) opens `DiplomacyPanel` (any phase of the turn; opens by
itself at the start of a turn with offers waiting) and `TradePanel` (its button only exists in the
build phase); both sit on the left edge, one at a time, and close on phase change. The turn label
gains a "Traitors:" line while anyone is marked.

**Singletons** derive from `Gameplay.Helpers.Singleton<T>`; they override `protected virtual void
Awake()` and must call `base.Awake()` first. They are scene-enforced (duplicates destroy themselves,
no `DontDestroyOnLoad`): `GameStateMachine` survives because `Bootstrap` is never unloaded,
`GameController` lives in `WorldMap` and is recreated with it.

**Managers** (`Gameplay.Managers`): `GameController` holds the `GameRules` asset, the countries, the per-match player list,
turn order (`DetermineStartingPlayer`, `NextPlayer` — skips eliminated players), income, the map
graph and attack resolution (`CanAttack`, `AttackTargets`, `TryAttack`, `CanMoveArmy`,
`TryMoveArmy`). Like `MapGraph` it owns per-match plain C# systems: `Fog` (`FogOfWar`), `Market`
and `Diplomacy` (`Diplomacy.DiplomacySystem`). The diplomacy hooks run in `PhaseOne`
(`OnTurnStarted`) and `NextPlayer` (`OnTurnEnded`).

**The map is a graph.** `Country` carries a stable `_id` and a `_borders` list authored one way only;
`MapGraph` — a plain C# class owned by `GameController` and baked in `Awake` — mirrors every edge
into symmetric id-based adjacency, so gameplay and the future network layer pass ids instead of
object references. It warns about missing or duplicate ids and borders pointing off the map, and
`ConnectedGroups()` reports whether the map is one reachable whole: six continents joined by sea
links, and a match cannot be won while any group is cut off. Gameplay asks the graph, never the
transforms.

**`View`** holds presentation that is not UI. `MapView` spawns a `CountryMarker` over every country
at `Start`: a world-space disc in the owner's colour with the income dice number on it and the army
(knights/horsemen/archers) below, re-read every frame. World space, not a canvas, so markers pan and
zoom with the map. Seat colours come from `PlayerPalette` (seat = index in `GameController.Players`),
which the HUD uses too. `MapCamera` sits on the map sprite in `WorldMap`, finds
the Bootstrap camera through `Camera.main` (cross-scene references are impossible), and does
drag-pan, wheel-zoom towards the cursor and clamping so the view never leaves the map bounds. It
computes and sets the camera size itself on enable, from the sprite bounds and the current aspect.

**Regions** are the six continents. A `Region` component sits on each continent's parent object and
holds the soldier training costs; `Country._region` points at it, and a country without one warns in
`Awake` and cannot train.

**Players** are plain C# objects (`Gameplay.Player`), created per match from `MatchSettings`
(lobby output, 4–6 players, named by seat for now). Presentation data (colour etc.) belongs to the
view layer. Ownership is one-way: `Country.Owner` is the source of truth, `Player.OwnedCountries`
is derived from it; change it with `Country.SetOwner`.

**Structure is separate from presentation.** Gameplay code reads country state only; world
positions, sprites and UI are a separate layer on top. Do not put rendering decisions in `Gameplay`.

## Gameplay rules worth knowing

- Match setup (`StartNewMatch`): countries are shuffled and dealt round-robin (equal share ±1, not by
  region);
  every country gets a random army (`GameRules.RandomUnitsPerType`, never empty) and a random
  `_baseResourceGain` (`GameRules.RandomResourceGain`) — a stand-in until the user designs starting
  values; then
  income dice numbers are assigned; the starting player is decided by roll-offs
  (`DetermineStartingPlayer`).
- Income dice numbers are dealt per match, not stored in the scene: every number 1-9 lands on at least
  `GameRules.MinCountriesPerDiceNumber` (2) and at most `MaxCountriesPerDiceNumber` (5) countries; the rest is
  random.
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
- One building per turn per country, each building unique per country. Queues are processed in phase
  one and each entry is paid in full when it is delivered — nothing is reserved or paid in part. An
  entry that cannot be afforded **stays queued** and is retried next turn (user's decision); the
  building queue waits on its head, while a cheaper soldier further down may still be trained.
  Soldier prices come from the country's `Region`.
- A country that changes hands drops both queues (`Country.SetOwner`); built buildings stay.
- **Fog of war is always on**, hotseat or online (user's decision): a player sees their side's
  countries (their own and their allies') and the direct neighbours of those (`GameController.Fog`,
  `FogOfWar.IsVisible`, depth 1 on the graph). Every other country still shows on the map with its
  dice number and nothing else — grey disc, no owner, no army (confirmed by the user). Hotseat views
  the map as `CurrentPlayer`.
- **Market** (`Gameplay.Market`), build phase only: bank trade at `GameRules.DefaultTradeRatio`
  (4:1) — give 4 of one resource for 1 of another. A built building with `Building._tradeRatio` > 0 (the Market building: 3) lowers the
  owner's ratio while they hold its country; the best ratio wins. The Market building is
  `ScriptableObjects/Market.asset` (trade ratio 3), listed in `GameRules` next to `House`.
- **Diplomacy** (`Diplomacy.DiplomacySystem`), local rules decided with the user:
  - A **pact** (non-aggression) lasts `GameRules.PactTurns` (3) of the proposer's turns. An **alliance** has no
    end date and also shares vision through the fog. A pact can be upgraded to an alliance.
  - Any treaty forbids attacks both ways (`CanAttack` checks `AtPeace`); moving into allied countries
    is not allowed.
  - Offers wait for the target's turn and lapse when it ends unanswered.
  - **Breaking** a treaty takes effect at the start of the breaker's next turn — until then it still
    holds, and the breaker is marked **traitor**, visible to everyone for that one round, then the
    mark is gone. Nothing else happens to a traitor.
  - The win condition is unchanged: one player must own every country, so allies cannot win
    together. If only allies are left, one of them has to break the alliance.
- There is no unit cap per country, by decision.

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

## Handoff — resume here (2026-09-25)

The map is wired: 35 countries, one connected group. **Play Out Match** has reached `GameOver`, and
the user has played a short hotseat match by hand — the four-phase turn, the picker and panels, the
dice, fog of war, market and diplomacy all behaved. A detailed play-test is still to come.

Setup in `WorldMap` the code expects:
- `GameController._rules` → a `GameRules` asset (*Create > Create Game Rules*) holding the building
  catalogue (`House`, the Market building) and the rule numbers. Without it the game logs an error
  and runs on defaults with no buildings.
- `InGameHud._diceFaces` → the nine faces `Assets/Assets/Dice/Sprite-001..009.png` (128×128, white
  body, black pips, point filter — white so the UI tints them), face n at index n-1.
- Countries use a `CircleCollider2D` for now; traced `PolygonCollider2D` shapes come later.

`CountryPicker` (added at runtime by `MapView`) enforces the turn rules the view owns — left click
only, clicks over UI and while `MovePanel` is open are ignored, everything resets on phase change:
- Attack: own country → white halo + red halo on `AttackTargets`; red one → `TryAttack`, dice in
  `CombatPanel`. On conquest `MovePanel` offers to send units back; selection then follows the army.
- Build: own country → `CountryPanel`.
- Move: own country with more than the garrison → green halo on own neighbours; green one →
  `MovePanel`; a confirmed non-empty move uses up the turn's move.

**The user's to decide or make** (do not pick these yourself):
- Soldier prices per region (all 1/1/1/1 now).
- Starting armies and incomes — random stand-ins from `GameRules` until designed.
- Buildings on conquest: the conqueror may **choose to raze them or keep them** (direction agreed;
  details open — all or one, any refund). Today they are simply kept.
- The traced country shapes; the Market building's cost (`ScriptableObjects/Market.asset` exists,
  trade ratio 3, cost still 0).
- More ScriptableObjects besides `GameRules` will be needed; they are made when a feature asks.

**Order of work:** polish local hotseat first. The online layer comes only once local play is
polished — do not start it earlier.

## Known open threads

- `GameController` is deliberately phase-blind: the phase gates, the post-conquest move pair and the
  one-move-per-turn limit all live in `CountryPicker` (View), not in gameplay code.
- **Play Out Match** ignores phases and the garrison rule entirely (it marches whole armies) — it
  is a debug shortcut.
- A move (including the post-conquest one) must leave `GameRules.MinimumGarrison` (1) units
  behind: `TryMoveArmy` refuses otherwise, `MovePanel` caps the picks, and `CountryPicker` only
  offers countries with more than that. Conquest still empties the attacking country by design.
- Dice animations (`UI.DieRoll`: ~0.6 s tumble through random faces, then a pop) are presentation
  only — the roll is already applied and nothing waits for them, so markers and resources update
  before the dice land. Income animations (resources flying in) are still TODO.
- Country state that changes during a match (owner, army, built buildings, queues) lives in
  `Country` fields; `_trainingQueue` and `_army` are serialized, the rest is runtime-only. Saving or
  networking will want it pulled out into plain data.
- **Planned for online play, not before** (user's decision): player-to-player trade offers (the
  other player accepts or declines on their turn), and tribute/gifts between players (one-off or
  "X per turn for N turns", e.g. to buy peace). Everything so far is built for local hotseat.

The map is fixed and hand-authored: 35 territories across six
continents (Vargmark, Zlatokraj, Higanshu, Aureliana, Kanembara, Ashqaran — each named from a
different real-world tradition, listed in `Editor/MapSetup.cs`), not procedural.
