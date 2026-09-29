# Border Inquisition: architecture reference

How the code is built. Read the section a task touches; `CLAUDE.md` is the short core.

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

## Systems

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
`_slicedPixelScale`), label auto-sized 10-24 with side padding. `_fontScale` (0.3-2) multiplies every text size through `ThemedText` (a component per text holding its base size; `UiFactory.Label`/`Button`, markers and popups call `ThemedText.Set`, and each view's `Awake` calls `ThemedText.ThemeAll` for the scene's own texts, font included). Changing the scale in the inspector re-applies it to every open text (`UiTheme.OnValidate`), in Play mode too; label rows grow with a scale above 1 and never shrink. Scene-placed buttons keep whatever
was set on them by hand. Panels have no background since 2026-09-27 (`UiFactory.PanelColor` is clear;
the image stays so clicks on a panel never reach the map); the pause menu keeps its dimmed overlay.
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


## Scene setup the code expects (`WorldMap`)

- `GameController._rules` → a `GameRules` asset (*Create > Create Game Rules*) holding the building
  catalogue (only listed buildings can be built) and the rule numbers. Without it the game logs an error
  and runs on defaults with no buildings.
- `InGameHud._diceFaces` → the nine faces `Assets/Assets/Dice/Sprite-001..009.png` (128×128, white
  body, black pips, point filter — white so the UI tints them), face n at index n-1.
- Countries have `PolygonCollider2D`s (converted with **Convert Country Colliders To Polygon**); the
  shapes are traced. Nothing in code depends on the collider type (`Physics2D.OverlapPoint`).


## `CountryPicker` (view-side turn rules)

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


## Save: open design notes

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

