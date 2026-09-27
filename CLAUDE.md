# Border Inquisition

Turn-based multiplayer strategy game in Unity 6000.3.18f1 (URP 2D, UI-heavy). Risk/Catan inspired.
All gameplay logic is currently local (hotseat) — no networking layer yet.

Unity project root is `Border Inquisition/`, code lives in `Border Inquisition/Assets/Scripts/`.
Scenes in `Assets/Scenes/`: `Bootstrap` (persistent), `MainMenu`, `Lobby`, `WorldMap` (the map being
built), `GameOver`. All must be in Build Profiles with `Bootstrap` first.

`GDD.md` (repo root) is the design document: the game as built and decided, the current numbers, and
every open decision. Keep it in step when a rule or a decision changes. It is the user's private
file and stays out of git (listed in `.git/info/exclude`).

**When a piece of work is finished** (user's standing request): update `CLAUDE.md` (including the
Handoff) and `GDD.md`, then give the user a commit message. The user makes the commit.

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
- **Set Up UI Theme** (`Editor/UiThemeSetup`) — makes `Assets/Resources/UiTheme.asset` if missing
  (font `Assets/Fonts/Daydream DEMO SDF`, large button `13_20` and small button `13_10` from
  `Assets/GUI/Retro Pixel Ribbons, Banners and Frames 2/13.png`; slots already set are kept), writes
  the theme font into the TMP Settings default and onto every `TMP_Text` in the enabled build scenes
  (inactive included; scenes are saved). Re-run after changing the theme's font.
- **Set Up Audio** (`Editor/AudioSetup`) — puts an `AudioManager` next to `GameStateMachine` in
  Bootstrap and links the `AudioLibrary`; it finds any existing library by type and only makes
  `Assets/Audio/AudioLibrary.asset` when none exists (the library now lives in
  `Scripts/ScriptableObjects/Audio`).
- Countries and their borders are drawn as scene gizmos (`Editor/MapLinker`). Borders are authored
  in the `Country` inspector, one side only.
- Right-click the `GameController` component for **Assign Country Ids**, **Assign Regions** (puts a
  `Region` on each continent parent and links its countries) and **Validate Map**.
- The `GameController` inspector (`Editor/GameControllerEditor`, play mode, match running) drives
  `Editor/MatchSimulator` (editor-only, plays matches without the view, every seat through an
  `AiBrain`, turns back to back in one call):
  - **Play Out Match** — finishes the running match, humans played as Normal AIs; the end goes
    through `MatchWon`/`MatchLost` as usual, so `GameOver` follows. Hitting the round limit logs a
    warning and returns to the main menu.
  - **AI Simulation** foldout — matches, players (2–6), difficulty or mixed (seats cycle
    Easy/Normal/Hard), battle samples (Monte-Carlo samples per attack option — fewer than the
    profile's makes batches faster, via `AiProfile.WithSimulations`), round limit. Runs all-AI
    matches (`MatchSettings.AllAI`) with `GameController.Simulating` set, cancellable from a progress
    bar, logs statistics to the console, then returns to the main menu. Statistics: won/stuck, rounds
    to a win, wins by turn order and by difficulty, dice rounds/conquests/razes per match, income per
    player turn per resource, buildings built and soldiers trained per match, region bonuses paid per
    match. Meant for balancing the numbers. The batch settings and the foldout are kept in
    EditorPrefs (`BorderInquisition.Simulation.*`, per machine), so they survive an editor restart.
  - The old random-march play-out (`GameController.PlayOutMatch` and its helpers) is gone.

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
  `Completed` handler (`MainMenuState`, `LobbyState`, `InGameState`, `GameOverState`).

**Outer flow** (`GameStateMachine`, Singleton in the `Bootstrap` scene):
MainMenu → Lobby (or Quit, or Continue → InGame from the save); Lobby → InGame (or back to MainMenu); InGame → GameOver (or
MainMenu when the match is left from the pause menu); GameOver → MainMenu (or rematch → Lobby).
`GameStateMachine.StageChanged` publishes the outer stage as a `FlowStage`
(MainMenu/Lobby/InGame/GameOver) — the music follows it.

**Scenes are owned by states.** Outer states derive from `SceneState`, which additively loads the
state's scene on enter, makes it the active scene, and unloads it on exit. Subclasses override
`OnSceneLoaded` / `OnSceneUpdate` / `OnSceneExit`; `IsSceneLoaded` is false until the scene is ready
and after exit, and every UI command checks it so a stray click on an inactive state is ignored.

**In-game phases.** `InGameState` owns a nested machine with three private phases deriving from
`Phase`. A turn is:
1. `FirstPhase` (`TurnPhase.Income`) — not a phase the player waits in: queues are processed, the d9
   is rolled and paid out, and the phase ends itself from `OnEnter`. The HUD shows the roll.
2. `SecondPhase` (`Attack`) — unlimited attacks. After a **conquest** (only then) the player may move
   units between exactly those two countries, any amount.
3. `ThirdPhase` (`BuildAndMove`, shown as "Build & Move") — queue buildings and soldiers per country,
   trade, and **one** move per turn between two neighbouring own countries, any amount, in any order.
   Build and Move were one phase each until the user merged them (2026-09-25).

Phases 2-3 wait for End Phase; the 3 → 1 transition calls `GameController.NextPlayer()`. A match starts in `InGameState.OnSceneLoaded` via
`GameController.StartNewMatch(settings)`, and ends when `GameController.MatchWon` fires — or
`MatchLost`, when no human is left (GameOver then shows no winner). `GameController.IsOver` is set by
either and blocks further attacks.

**UI talks to the flow only through `GameStateMachine`.** Views in `UI` (`MainMenuView`,
`LobbyView`, `InGameHud`, `GameOverView`) call its command forwarders (`Play`, `Quit`,
`StartMatch(MatchSettings)`, `LeaveLobby`, `EndPhase`, `LeaveMatch`, `Rematch`, `ReturnToMainMenu`) and read `PhaseChanged` /
`CurrentPhase` / `Winner`; they never touch the state classes. `InGameHud` builds the in-game
panels in code at `Awake` (`UiFactory`, layout groups — no scene wiring): the income die box (top
right), `CombatPanel` (top centre, fed by `GameController.AttackResolved` for every attack, human or AI: the last attack's dice sorted and paired, tinted in seat colours; they
tumble, then each pair's loser dims and the outcome appears; never blocks map clicks, hides on phase
change),
`MovePanel` (pick knights/horsemen/archers with +/-/All; the caller performs the move in the confirm
callback) and `CountryPanel` (build phase: queue/unqueue buildings from `GameRules`
and soldiers, costs from the region). `View.CountryPicker` opens them through the HUD. A bottom-left
action bar (hidden in the income phase) opens `DiplomacyPanel` (any phase of the turn; opens by
itself at the start of a turn with offers waiting) and `TradePanel` (its button only exists in the
build phase); both sit on the left edge, one at a time, and close on phase change. The bar's Menu
button and Esc open `PauseMenu` (resume, leave the match — asked twice, the "AI speed: …" button and
the volume sliders). The turn label
gains a "Traitors:" line while anyone is marked. `CountryTooltip` follows the cursor over the map
and describes the country as the current player sees it (fog-aware; queues on own countries only).
**`UiTheme`** (ScriptableObject, loaded from `Resources/UiTheme` by `UiTheme.Current`; null → the
old plain look): the font and the button ribbons. `UiFactory.Label` and the world-space labels
(`CountryMarker`, `IncomePopup`) call `UiFactory.ApplyFont`; `UiFactory.Button` picks the large
sprite for buttons at least `_largeButtonMinHeight` (50) tall — panel actions, action bar — and the
small one for row buttons and +/-, 9-sliced when the sprite has borders (scaled by
`_slicedPixelScale`), label auto-sized 10-24 with side padding. `_fontScale` (0.3-2) multiplies every text size written in code (`UiFactory.FontSize`, markers and popups included); scene texts keep their own. Scene-placed buttons keep whatever
was set on them by hand.
`CombatPanel` shows a battle only when `Fog.SeesBattle` holds for the viewer (`OnAttackResolved`).
The HUD also creates `RazePanel` (see Gameplay rules > Buildings on conquest) and has
`RefreshStatus()` for changes it does not hear of (loot). `UI.Format.Effects(building)` gives
"income Food 1, soldiers -20%" for panels.

**Turn start.** When Income is followed by Attack, the HUD runs `InGameHud.BeginTurn` (there is no
handover screen between turns - removed by the user's decision): the income die tumbles, `TurnReportPanel` opens (`Player.Report`: every roll
since the player's last turn and what it paid, queue deliveries, what is still queued, attacks on
their countries), diplomacy opens if offers wait, and `TurnBegan` fires — `MapView` spawns
`IncomePopup`s over the countries this turn's roll paid. `PauseMenu` is a `UiFactory.Overlay`: a
nested canvas with its own sorting order, above every panel.

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

**AI** (`AI`, `Assets/Scripts/AI`). A seat is Human or AI with a `Difficulty` (Easy/Normal/Hard),
picked per seat in the lobby (`MatchSettings.Seats`, at least one human); `Player.IsAI` /
`Player.Difficulty` carry it. `AiDriver` (MonoBehaviour; `GameController.Awake` adds one if WorldMap
has none) listens to `PhaseChanged` and plays AI turns as coroutines: answer offers, attack step by
step, then Build & Move, and ends each phase through `GameStateMachine.EndPhase` like the button.
- The driver presents only what the viewer's fog shows: before a battle or the turn's move that
  `GameController.Viewer` can see, both sides light up through `MapView` and the camera glides to it
  with `MapCamera.Focus`, then `AimDelay`; `ActionDelay` after every dice round
  (`FogOfWar.SeesBattle(viewer, from, to, defender)` for battles — either country visible, or fought
  against the viewer's side; either end visible for the move). Unseen steps happen without a pause
  (one frame). Every wait is held while the pause menu is open.
- Build & Move is split: the move first (`AiBrain.TryChooseMove`, shown as above, with
  `InGameHud.ShowActivity("Moves N units from X to Y")` replacing the "is playing..." line until the
  phase changes), then `AiBrain.PlayEconomy` (trade, queues, offers — private, not shown).
- **AI pace** (`AI/AiSettings`): Normal / Fast (pauses × 0.35) / Instant (no camera, no pauses, one
  frame per step). Set from the pause menu, stored in `PlayerPrefs` key `AiPace` and saved at once.
- `AiDriver.ProfileFor` is public (the simulator uses it); `AiDriver.Halt()` stops the running turn;
  the driver does nothing while `GameController.Simulating` is set.

`AiBrain` (plain C#) decides: attacks by simulated win chance (`Combat.AttemptAttack(Army, Army)` on
copies) times a target value (income, region completion, breaking a region); after a conquest
`AiBrain.Occupy(origin, conquered)` sends units back by threat, then razes every building if the
conquered country looks likely to be lost again (`Threat > army power`), otherwise keeps them; it
makes the turn's one move towards the most pressed front, trades away whichever resource is highest
after paying its ratio (so a region's cheap resource goes first),
queues soldiers on outnumbered fronts and one building a turn in a safe country, accepts treaties
from stronger players and now and then proposes one; it never breaks a treaty. `AiProfile`
(ScriptableObject, *Create > Create AI Profile*) holds the numbers per difficulty; the `AiDriver`
slots are empty by default and fall back to `AiProfile.Preset` — Easy/Normal read only what their
fog shows, Hard sees everything (user's decision). The AI keeps the view-enforced rules (one move a
turn, garrison) itself. During an AI turn `CountryPicker` ignores clicks and highlights the AI's
attacks, the HUD hides End Phase and the action bar, and no turn report opens.

**Audio** (`Assets/Scripts/Audio`, namespace `Audio`): `AudioManager` (Singleton) sits next to
`GameStateMachine` in Bootstrap (put there by **Set Up Audio**). Music follows
`GameStateMachine.StageChanged` (GameOver plays Victory when a human won, Defeat otherwise) and
crossfades over two sources; effects round-robin over 6 voices, UI sounds have their own source.
`AudioManager.Play(Sound)` / `PlayUi(Sound)` are static and do nothing without a manager.
- `AudioLibrary` (ScriptableObject, `Scripts/ScriptableObjects/Audio/AudioLibrary.asset`): one slot
  per `Sound` and `Music` enum value, filled automatically (`FillSlots` on Reset/OnValidate/setup);
  per sound several clips (random pick), volume, pitch variance, cooldown (many requests at once,
  e.g. six dice, play once). An empty slot is silent. No AudioMixer yet.
- `SoundSettings`: Master/Music/Effects 0–1 in PlayerPrefs, sliders in the pause menu
  (`UiFactory.Slider`), written to disk when the pause menu closes.
- **Gameplay code never calls audio.** `View/SoundCues` (added by `MapView`) turns match events into
  sounds from `Viewer`'s side and through the fog: visible battles (after the dice land: Conquest, or
  BattleWon/BattleLost when the viewer's side fought), visible moves and razes, treaties involving
  the viewer, every betrayal, and at turn start TurnStart (human turns), Income, Built/Trained and
  Offer. Muted while `Simulating`. The UI plays its own: every `UiFactory.Button` and scene button
  (`UiFactory.ClickSound`) clicks, panels call `UiFactory.PanelOpened/PanelClosed` in Open/Close,
  `DieRoll` plays DiceRoll/DiceLand, the trade panel Trade/Error.

**Save** (`Assets/Scripts/Save`, namespace `Save`) — the simplest version, built 2026-09-27 on the
user's request ("just so a save exists"; to be revisited). One autosave per turn: when `FirstPhase`
completes (income paid) `InGameState` writes `MatchSnapshot.Capture` through `SaveSystem.Write`
(`persistentDataPath/match.json`, JsonUtility, via a `.tmp` file, synchronous). `MatchSave` is plain
`[Serializable]` data with a header (`FormatVersion` — another version is ignored, `GameVersion`,
`SavedAtUtc`): players (name, AI, difficulty, resources, turn report), countries by id (owner seat,
dice number, base gain, army, built / queued buildings by asset name, training queue), treaties and
offers by seat, current player, last human, last roll. **Continue** in the main menu (made in code
as a copy of Play, one step above it, unless `MainMenuView._continueButton` is set) shows only when
`SaveSystem.HasSave`; `MainMenuState.Continue` reads it, `InGameState.Resume` carries it,
`MatchSnapshot.TryRestore` → `GameController.ResumeMatch` + `Country.RestoreQueues` +
`DiplomacySystem.Restore`, and the phase machine starts at `SecondPhase` (Attack) — the HUD's
turn start still runs, since its `_phase` starts as Income. A save that does not fit the map is
deleted and the flow returns to the main menu. The save is deleted when the match is won or lost;
leaving from the pause menu keeps it ("Sure? This turn is lost"). Not stored: anything of a turn in
progress (moves, attacks, trades, conquests of this turn) and the random state — nothing needs it,
since a load never re-rolls.

**Match events** for presentation and future statistics — they fire in simulations too, scene
listeners ignore them while `Simulating`: `GameController.CountryConquered(country, newOwner,
oldOwner)`, `BuildingRazed(country, building)`, `ArmyMoved(from, to, units)`;
`DiplomacySystem.TreatySigned(treaty)`, `TreatyBroken(treaty)`. `GameController.Simulating` silences
`MatchWon`, `MatchLost` and `AttackResolved`.

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
`Focus(a, b)` glides (SmoothDamp) to the middle of two points and zooms in to at most `_focusZoom` of
the widest view; any pan or zoom by hand cancels it.

**Regions** are the six continents. A `Region` component sits on each continent's parent object and
holds the region's profile — `_richIn` and `_poorIn` (a `ResourceType` each) — and its
`_completionBonus`; `Country._region` points at it, and a country without one warns in `Awake` and
cannot train.

**Players** are plain C# objects (`Gameplay.Player`), created per match from `MatchSettings`
(lobby output, **2–6 seats** — `MatchSettings.MinPlayers` is 2, user's decision; was 4 — named by
seat: "Player 3", "Player 3 (AI)"). Presentation data (colour etc.) belongs to the view layer.
`LobbyView` remembers the seats (human/AI, difficulty) in PlayerPrefs key `LastLobby`, one JSON value
(`JsonUtility`, nested `SavedLobby`/`SavedSeat`), written in `OnDestroy` — so whenever the lobby is
left, by Start or Back alike (user's decision) — and saved to disk at once. Missing, broken or
too-short JSON → the default seats (seat 1 human, three AIs); more than 6 are cut; an unknown
difficulty becomes Normal. The stored seats come back exactly, even all-AI (Start then waits for a
human, as always). Ownership is one-way: `Country.Owner` is the source of truth, `Player.OwnedCountries`
is derived from it; change it with `Country.SetOwner`.

**Structure is separate from presentation.** Gameplay code reads country state only; world
positions, sprites and UI are a separate layer on top. Do not put rendering decisions in `Gameplay`.

## Gameplay rules worth knowing

- Match setup (`StartNewMatch`): countries are shuffled and dealt round-robin (equal share ±1, not by
  region);
  every country gets a random army (`GameRules.RandomUnitsPerType`, never empty) and a random
  `_baseResourceGain` (`GameRules.RandomResourceGain`, 0-3) — random stays for now (user's decision,
  2026-09-26), so a hand-set gain in the inspector is overwritten; then
  income dice numbers are assigned; the starting player is decided by roll-offs
  (`DetermineStartingPlayer`).
- Income dice numbers are dealt per match, not stored in the scene: every number 1-9 lands on at least
  `GameRules.MinCountriesPerDiceNumber` (2) and at most `MaxCountriesPerDiceNumber` (5) countries; the rest is
  random.
- Win condition: one player owns every country (checked after each conquest).
- `GameResources` is a struct (food/wood/gold/stone) with `+`, `-`, `>=`, `<=` operators. It is passed
  `ref` into countries, which spend from and add to the owning player's pool.
- Income is the Catan model: at the start of each player's turn one d9 is rolled, and every country
  whose `_nationDiceNumber` matches pays out `Country.Income` (`_baseResourceGain` plus its built
  buildings' `ProductionBoost`), for all players at once.
- `Player.Report` (`TurnReport`) collects what happens to a player between turns — rolls and payouts,
  queue deliveries (`Country.StartPhaseOne`), attacks on them (`TryAttack`) — and is cleared in
  `NextPlayer` when their turn ends. Gameplay fills it; only the HUD reads it.
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
- **Prices** (`Country.GetSoldierCost` / `GetBuildingCost`): the base price (`GameRules` knight/horseman/
  archer cost, `Building._buildingCost`) goes through the country's `Region.Price` — the region's rich
  resource costs `GameRules.RichPricePercent` (50) of base, its poor one `PoorPricePercent` (150) —
  and then the owner's best discount. Every scaling rounds up, so nothing becomes free.
- **Building effects**: `_productionBoost` adds to the country's income; `_soldierDiscount`
  (Barracks) and `_buildingDiscount` (Tavern) are
  percent off every soldier / building the owner pays for anywhere, while they hold the country the
  building stands in. Discounts do not stack — the best one counts
  (`Player.SoldierDiscount` / `BuildingDiscount`). Costs are recomputed when a queue entry is paid.
- **Region bonus**: at the start of a player's turn (`GameController.PhaseOne`, before the queues)
  every region they own whole pays its `Region._completionBonus`; it shows in the turn report.
- A country that changes hands drops both queues (`Country.SetOwner`); built buildings stay unless
  razed.
- **Buildings on conquest** (direction was the user's; the details were picked by Claude on the
  laptop, 2026-09-27, and the user may change them): after taking a country the conqueror chooses per
  building to **keep** it (it works for them from now on) or **raze** it. Razing pays
  `GameRules.RazeLootPercent` (50, 0 = razing only denies) of the building's **base** cost, rounded
  up. Only allowed in a country taken this turn and still held (`GameController.CanRaze` / `TryRaze` /
  `RazeLoot`, tracked in `_conqueredThisTurn`, cleared in `NextPlayer` and `StartNewMatch`); the rule
  and the payout live in `GameController`, `Country.Raze(building)` only removes it (it replaced
  `Country.DestroyBuildings`). The former owner's `TurnReport` gets it (`AddRazed` →
  `Defence.Razed`, "They razed …" in the report panel).
- **Fog of war is always on**, hotseat or online (user's decision): a player sees their side's
  countries (their own and their allies') and the direct neighbours of those (`GameController.Fog`,
  `FogOfWar.IsVisible`, depth 1 on the graph). Every other country still shows on the map with its
  dice number and nothing else — grey disc, no owner, no army (confirmed by the user). The map is seen
  through `GameController.Viewer`: the current player on a human turn, the last human who played
  during an AI turn (markers, tooltip, income popups, the resources in the turn label).
- **Market** (`Gameplay.Market`), build & move phase only: bank trade, give N of one resource for 1
  of another. N is per player **and per resource given**: `GameRules.DefaultTradeRatio` (4), or
  `GameRules.RegionTradeRatio` (2) when the player holds a whole region rich in the resource given
  (user's decision, 2026-09-27: the region has it to spare). Regions rich in the same resource do not
  stack. `Market.RatioFor(player, give)`, `Market.RegionsRichIn(player, resource)`; `Market` takes the
  country list. The trade panel shows every rate ("Rates: Food 4:1, …, **Stone 2:1** (Aureliana)").
  There is no Market building any more (removed with `Building._tradeRatio`: a building is per
  country, the ratio per player). `GameRules.asset` lists `_razeLootPercent: 50` and
  `_regionTradeRatio: 2` explicitly.
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
  - An eliminated player's treaties and offers are dropped on the conquest that eliminates them
    (`DiplomacySystem.OnEliminated`), traitor mark included.
- There is no unit cap per country, by decision.

## Conventions

- Private fields `_camelCase`, serialized with `[SerializeField]` rather than public fields.
- Expression-bodied members for one-line accessors and forwarders; `#region` blocks to group
  Buildings/Units/Queries inside a large class.
- Namespaces mirror folders: `Gameplay`, `Gameplay.Managers`, `Gameplay.Helpers`, `GameStates`,
  `UI`, `View`, `Diplomacy`, `AI`, `Audio`, `Save`, `Editor`. No assembly definitions for game code (only the Better
  Hierarchy plugin has one); `Assets/Scripts/Editor` is editor-only by folder name as well as by
  `#if UNITY_EDITOR`.
- Editor-only helpers go behind `#if UNITY_EDITOR`.
- Keep code and comments in English; discussion with the user may be in Croatian.
- Do not edit `.meta` files or anything under `Library/`, `Temp/`, `obj/`, or `Assets/Plugins/`.

## Handoff — resume here (after 2026-09-27)

State at the end of 2026-09-27 (the work of that day was done on the laptop, commits `c40f978`,
`fc19910`, `0f7b812`; CLAUDE.md was merged back on the desktop):
- Map wired (35 countries, one connected group), shapes traced, soldier prices / building costs /
  region completion bonuses set by the user. Random per-country income stays for now.
- Merged Build & Move phase, regional prices, discounts and region bonus (commit `95cc1a4`) — not
  play-tested in depth yet.
- **AI** built (see Architecture > AI). Decided with the user: three difficulties per seat, Hard
  sees through fog, paced turns (now with an AI pace setting), diplomacy only answers offers and
  proposes now and then, the map follows the last human during AI turns, no spectate (at least one
  human), the match ends at once when the last human falls.
- 2026-09-27: raze on conquest, regional trade ratio (Market building removed), AI simulation /
  Play Out Match through `MatchSimulator`, fog-aware AI presentation, AI pace, 2–6 players, the lobby
  remembers its seats, the audio system with empty slots. Compiles (Unity's bundled csc) but **was
  not yet tested in Unity by the user**.

User's decisions on 2026-09-27:
- UI polish: **postponed** — the user is adding more assets first.
- Audio: done as a system with empty slots; the user adds the clips. Volume sliders live in the
  pause menu (this replaces the older "one slider on the main menu" idea).
- Hotseat without a handover screen: stays (confirmed again).
- Starting armies/incomes, raze loot percent and the rest of the balancing: the user will come back
  to it, with the AI simulation to help.

Next session, unless the user says otherwise:
1. Ask whether the laptop work (raze, trade rates, simulation, audio, lobby memory) was tested in
   Unity and fix what comes up.
2. Test the simple save (Continue after leaving mid-match, after quitting Play mode, a human and an
   AI turn) and fix what comes up.
3. UI polish — started 2026-09-27 with the user's own assets: `UiTheme` (Daydream font everywhere,
   ribbon buttons for everything built in code). Ask how it looks and what comes next (panels,
   frames, scene buttons, marker icons).

**Save system.** On 2026-09-27 the user stopped the step-by-step design and asked for the simplest
save that works, to think over later what to change, how and why — that is what is built (see
Architecture > Save). Earlier design notes, still open for that later pass:
- Autosave after every player choice (and after an AI turn) instead of once per turn — would need
  the turn-in-progress rules (move used, post-conquest pair, conquered this turn, phase) moved out of
  `CountryPicker` into saved state, and the random state or "generated" flags so nothing re-rolls.
  Background writing only if a write ever shows up as an FPS drop.
- **Match state out of MonoBehaviours** (user's decision): today the save is a mapping
  (`MatchSnapshot`) over the live objects; the plain C# state classes (Claude's proposal over
  ScriptableObjects, **not yet confirmed**) are still to come, and would serve online play too.
- Buildings are saved by asset name — renaming a building asset breaks them in old saves (skipped
  with a warning); an explicit id on `Building` was proposed, not decided.
- **Save header** as decided: format version, game version, save time (UTC).
- **PlayerPrefs = the person's settings on this machine:** `AiPace`, `VolumeMaster/Music/Effects`,
  `LastLobby`. Nothing else for now: no display, camera, UI-behaviour, language, key-binding or
  tutorial settings (the user does not want them yet). The player's name comes only with online play.
- Statistics are for the user (developer) only — nothing for players to keep.
- Editor-only settings go to EditorPrefs (done for the simulation).
- The **Continue** button is built (shown only when a save exists).

Working mode: fast for now (the user is in a hurry) — build agreed features whole, then list the
changed files and give a commit message. When the user says so, slow down: they want to learn, so
explain as you go and ask them questions.

Starting armies (Risk-style placement turn): the user is still thinking it over; do not start it.

Setup in `WorldMap` the code expects:
- `GameController._rules` → a `GameRules` asset (*Create > Create Game Rules*) holding the building
  catalogue (only listed buildings can be built) and the rule numbers. Without it the game logs an error
  and runs on defaults with no buildings.
- `InGameHud._diceFaces` → the nine faces `Assets/Assets/Dice/Sprite-001..009.png` (128×128, white
  body, black pips, point filter — white so the UI tints them), face n at index n-1.
- Countries have `PolygonCollider2D`s (converted with **Convert Country Colliders To Polygon**); the
  shapes are traced. Nothing in code depends on the collider type (`Physics2D.OverlapPoint`).

`CountryPicker` (added at runtime by `MapView`) enforces the turn rules the view owns — left click
only, clicks over UI and while `MovePanel` is open are ignored, everything resets on phase change:
- Attack: own country → white halo + red halo on `AttackTargets`; red one → `TryAttack`, dice in
  `CombatPanel`. On conquest → `RazePanel` (only if the country has buildings; rows Keep/Raze with
  effects and loot, Keep all / Raze all / Done; everything starts kept; map clicks are ignored while
  it is open; it closes on phase change = keep all) → `MovePanel` offers to send units back;
  selection then follows the army.
- Build & Move: own country → `CountryPanel`, plus, while the turn's move is unused and the country
  has more than the garrison, green halos on own neighbours; green one → `MovePanel` (the country
  panel closes and comes back after); a confirmed non-empty move uses up the turn's move.

**The user's to decide or make** (do not pick these yourself):
- The numbers: soldier prices, completion bonuses, building costs and effects (all set, tuned by
  play-testing), per-country incomes. Read the current values from the assets.
- Starting armies and incomes — random stand-ins from `GameRules` until designed.
- Buildings on conquest: built as per-building keep/raze with 50 % loot of the base cost — those
  details were Claude's pick and are the user's to change.
- More ScriptableObjects besides `GameRules` will be needed; they are made when a feature asks.
- The building assets are in `Scripts/ScriptableObjects/Buildings` (House, Tavern, Church,
  Barracks, LumberMill, Quarry); AI profiles in `Scripts/ScriptableObjects/AI`.

**Order of work:** polish local hotseat first. The online layer comes only once local play is
polished — do not start it earlier.

## Known open threads

- `GameController` is deliberately phase-blind: the phase gates, the post-conquest move pair and the
  one-move-per-turn limit all live in `CountryPicker` (View), not in gameplay code.
- **Play Out Match** plays every seat with an `AiBrain` (see Editor tools); it still ignores phases
  and the view-side turn rules the brain does not keep itself — it is a debug shortcut.
- A move (including the post-conquest one) must leave `GameRules.MinimumGarrison` (1) units
  behind: `TryMoveArmy` refuses otherwise, `MovePanel` caps the picks, and `CountryPicker` only
  offers countries with more than that. Conquest still empties the attacking country by design.
- Dice animations (`UI.DieRoll`: ~0.6 s tumble through random faces, then a pop) and income popups
  (`View.IncomePopup`, rising once the die lands) are presentation only — the roll is already
  applied and nothing waits for them, so markers and resources update before the dice land.
- Country state that changes during a match (owner, army, built buildings, queues) lives in
  `Country` fields; `_trainingQueue` and `_army` are serialized, the rest is runtime-only. Saving or
  networking will want it pulled out into plain data.
- **Planned for online play, not before** (user's decision): player-to-player trade offers (the
  other player accepts or declines on their turn), and tribute/gifts between players (one-off or
  "X per turn for N turns", e.g. to buy peace). Everything so far is built for local hotseat.

The map is fixed and hand-authored: 35 territories across six
continents (Vargmark, Zlatokraj, Higanshu, Aureliana, Kanembara, Ashqaran — each named from a
different real-world tradition, listed in `Editor/MapSetup.cs`), not procedural.
