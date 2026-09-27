# CLAUDE.md — changes to merge (laptop session, 2026-09-27)

The laptop had no current CLAUDE.md (it is untracked, `.git/info/exclude`), so this file lists what
changed after commit 6fce1be ("Add AI"), laid out by CLAUDE.md section. Merge it into the desktop
CLAUDE.md, then delete this file.

Status: code compiles (checked with Unity's bundled csc), **not yet tested in Unity by the user**.

---

## Build / run — add

- New scripts: `AI/AiSettings.cs`, `Editor/MatchSimulator.cs`, `UI/RazePanel.cs` (their `.meta`
  files were made by Unity on import - commit them too).

## Editor tools — replace the "Play Out Match" bullet

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
  - The old random-march play-out (`GameController.PlayOutMatch`, `TryBestAttack`,
    `AdvanceIdleArmies`, `NextStepTowardsEnemy`) is gone.

## Architecture — AI section, replace/extend

- `AiDriver` presents only what the viewer's fog shows: before a battle or the turn's move that
  `GameController.Viewer` can see, both sides light up and the camera glides to it
  (`FogOfWar.SeesBattle(viewer, from, to, defender)` for battles — either country visible, or fought
  against the viewer's side; either end visible for the move). Unseen steps happen without a pause
  (one frame, still held by the pause menu).
- Build & Move is split: the driver makes the move first (`AiBrain.TryChooseMove`, shown as
  above, with `InGameHud.ShowActivity("Moves N units from X to Y")` replacing the "is playing..."
  line until the phase changes), then `AiBrain.PlayEconomy` (trade, queues, offers — private, not
  shown).
- After a conquest the AI calls `AiBrain.Occupy(origin, conquered)`: sends part of the army back
  (as before), then razes every building if the conquered country looks likely to be lost again
  (`Threat > army power`), otherwise keeps them.
- `AiDriver.ProfileFor` is public (the simulator uses it); `AiDriver.Halt()` stops the running turn.
- **AI pace** (`AI/AiSettings`): Normal / Fast (pauses × 0.35) / Instant (no camera, no pauses, one
  frame per step). Set from the pause menu button "AI speed: …", stored in `PlayerPrefs` key
  `AiPace` and saved at once (`PlayerPrefs.Save`) — per machine, not in the project.
- `InGameHud` shows a battle in `CombatPanel` only when `Fog.SeesBattle` holds for the viewer
  (`OnAttackResolved`); it also creates `RazePanel` and has `RefreshStatus()` for changes it does not
  hear of (loot).
- `GameController.Simulating` silences `MatchWon`, `MatchLost` and `AttackResolved`; `AiDriver`
  does nothing while it is set.

## Gameplay rules — add

- **Buildings on conquest** (direction was the user's; details below picked by Claude, the user may
  change them): after taking a country the conqueror chooses per building to **keep** it (it works
  for them from now on) or **raze** it. Razing pays `GameRules.RazeLootPercent` (default 50, 0 =
  razing only denies) of the building's **base** cost, rounded up. Only allowed in a country taken
  this turn and still held (`GameController.CanRaze` / `TryRaze` / `RazeLoot`, tracked in
  `_conqueredThisTurn`, cleared in `NextPlayer` and `StartNewMatch`). The former owner's
  `TurnReport` gets it (`AddRazed` → `Defence.Razed`, "They razed …" in the report panel).
- Human flow in `CountryPicker`: conquest → `RazePanel` (only if the country has buildings; rows
  Keep/Raze with effects and loot, Keep all / Raze all / Done; everything starts kept) → the
  send-back `MovePanel` as before. Clicks on the map are ignored while `RazePanel` is open; it closes
  on phase change (= keep all).
- `Country.DestroyBuildings` is now `Country.Raze(building)`; the rule and the payout live in
  `GameController`.
- `UI.Format.Effects(building)` — "income Food 1, trade 3:1, soldiers -20%" for panels.

## Architecture — add an Audio section

- **Audio** (`Assets/Scripts/Audio`, namespace `Audio`): `AudioManager` (Singleton) sits next to
  `GameStateMachine` in Bootstrap — put there by **Border Inquisition > Set Up Audio**
  (`Editor/AudioSetup`), which also makes `Assets/Audio/AudioLibrary.asset` if none exists. Music
  follows `GameStateMachine.StageChanged` (new `FlowStage` enum: MainMenu/Lobby/InGame/GameOver;
  GameOver plays Victory when a human won, Defeat otherwise) and crossfades over two sources;
  effects round-robin over 6 voices, UI sounds have their own source. `AudioManager.Play(Sound)` /
  `PlayUi(Sound)` are static and do nothing without a manager.
- `AudioLibrary` (ScriptableObject): one slot per `Sound` and `Music` enum value, filled
  automatically (`FillSlots` on Reset/OnValidate/setup); per sound several clips (random pick),
  volume, pitch variance, cooldown (many requests at once, e.g. six dice, play once). An empty slot is
  silent. No AudioMixer yet.
- `SoundSettings`: Master/Music/Effects 0–1 in PlayerPrefs, sliders in the pause menu
  (`UiFactory.Slider`), written to disk when the pause menu closes.
- **Gameplay code never calls audio.** `View/SoundCues` (added by `MapView`) turns match events into
  sounds from `Viewer`'s side and through the fog: visible battles (after the dice land: Conquest, or
  BattleWon/BattleLost when the viewer's side fought), visible moves and razes, treaties involving
  the viewer, every betrayal, and at turn start TurnStart (human turns), Income, Built/Trained and
  Offer. Muted while `Simulating`. The UI plays its own: every `UiFactory.Button` and scene button
  (`UiFactory.ClickSound`) clicks, panels call `UiFactory.PanelOpened/PanelClosed` in Open/Close,
  `DieRoll` plays DiceRoll/DiceLand, the trade panel Trade/Error.
- New events for presentation and future statistics — they fire in simulations too, scene listeners
  ignore them while `Simulating`: `GameController.CountryConquered(country, newOwner, oldOwner)`,
  `BuildingRazed(country, building)`, `ArmyMoved(from, to, units)`;
  `DiplomacySystem.TreatySigned(treaty)`, `TreatyBroken(treaty)`.

## Handoff — user's decisions this session (2026-09-27)

- UI polish: **postponed** — the user is adding more assets first.
- Audio manager: done as a system with empty slots (see Audio); the user adds the clips.
- Hotseat without a handover screen: stays (confirmed again).
- Country shapes: done (already in the desktop CLAUDE.md).
- Starting armies/incomes, raze loot percent and the rest of the balancing: the user will come back
  to it, with the AI simulation to help.
- **Trade ratio — decided and done** (replaces the old "trade ratio, later" idea in Handoff): the
  Market building is removed (it was per country, the ratio is per player). See the Market rule below.

## Gameplay rules — replace the Market bullet and the `_tradeRatio` part of "Building effects"

- **Market** (`Gameplay.Market`), build & move phase only: bank trade, give N of one resource for 1
  of another. N is per player **and per resource given**: `GameRules.DefaultTradeRatio` (4), or
  `GameRules.RegionTradeRatio` (2) when the player holds a whole region rich in the resource given
  (user's decision: the region has it to spare). Regions rich in the same resource do not stack.
  `Market.RatioFor(player, give)`, `Market.RegionsRichIn(player, resource)`; `Market` now takes the
  country list. The trade panel shows every rate ("Rates: Food 4:1, …, **Stone 2:1** (Aureliana)").
- Removed: `Building._tradeRatio` / `TradeRatio`, the `Market.asset` building (and its entry in
  `GameRules._buildings`), the `_tradeRatio` lines of the other building assets. Buildings left:
  House, Tavern, Church, Barracks, LumberMill, Quarry (`ScriptableObjects/Buildings`).
- The AI trades away whichever resource is highest after paying its ratio, so a region's cheap
  resource goes first; building scores no longer count a trade ratio.
- `GameRules.asset` now lists `_razeLootPercent: 50` and `_regionTradeRatio: 2` explicitly.

## Known open threads — update

- Replace the **Play Out Match** bullet: it now plays every seat with an `AiBrain` (see Editor tools);
  it still ignores phases and the view-side turn rules the brain does not keep itself.

## Lobby — replace the Players / lobby description

- **2–6 players** (`MatchSettings.MinPlayers` is now 2, user's decision; was 4).
- `LobbyView` remembers the seats (human/AI, difficulty) in PlayerPrefs key `LastLobby`, one JSON
  value (`JsonUtility`, nested `SavedLobby`/`SavedSeat`), written in `OnDestroy` — so whenever the
  lobby is left, by Start or Back alike (user's decision) — and saved to disk at once. Missing,
  broken or too-short JSON → the default seats (seat 1 human, three AIs); more than 6 are cut;
  an unknown difficulty becomes Normal. The stored seats come back exactly, even all-AI (Start then
  waits for a human, as always).

## Save system — being designed with the user, step by step (nothing built yet)

Decided so far (2026-09-27):
- **No save button** (outdated). Autosave after every player choice, and when an AI player finishes
  its turn; the exact trigger is still to be refined (full snapshots every turn must not cause FPS
  drops — snapshot on the main thread, JSON + disk write in the background, debounced).
- **Match state moves out of MonoBehaviours** (user's decision). The user first suggested
  ScriptableObjects; Claude proposed plain C# data instead (SO assets keep play-mode changes in the
  editor and are not saved in builds), with definitions staying in ScriptableObjects / the scene and
  a runtime-only SO to inspect the state. **Not yet confirmed by the user.** The same data will serve
  online play.
- The gameplay random number state is to be saved, and "generated" flags added (setup steps done,
  this turn's income already rolled) so a load never re-rolls anything. Whether the game gets its own
  RNG (so saved state stays meaningful — `UnityEngine.Random` is shared with visuals, audio and AI)
  is still open.
- **Save header:** format version (decides whether a save can be read), game version
  (`Application.version`, informative), save time (UTC, shown local).
- **PlayerPrefs = the person's settings on this machine:** `AiPace`, `VolumeMaster/Music/Effects`,
  `LastLobby` (done — see Lobby below). Nothing else for now: no display, camera,
  UI-behaviour, language, key-binding or tutorial settings (none exist; the user does not want them
  yet). The player's name comes only with online play.
- Statistics are for the user (developer) only — nothing for players to keep.
- Editor-only settings go to EditorPrefs (done for the simulation).
- **Reminder for Claude:** remind the user about the **Continue** button in the main menu — shown only
  when an unfinished match save exists. Bring it up when the save system gets built.
- Next step in the design: the match state itself (what, how, why), one group at a time.
