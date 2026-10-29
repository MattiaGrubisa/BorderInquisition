# Code walkthrough

Started 2026-10-29. The earlier work was built fast for a deadline; now there is time. Before any
polish, go through all the code together: how each part works, why it is built that way, and what
should change. Then discuss and decide. Assets (UI, audio) come only after the walkthrough and polish.

## How each block is done

- Working mode is **slow**: explain as you go, ask the user questions. Do not change code during
  the walkthrough unless the user asks.
- For each block: what it does and where it sits in the flow; then through the code, explaining why
  it is built this way and the C#/Unity concepts it uses; then what should change and why, with
  questions for the user. The user reads along in Rider and interrupts.
- Changes are made as needed, by opinion or necessity. Findings go into the two lists below:
  **refactors** (code quality, architecture) and **fixes** (bugs and gaps in the rules).
- Blocks of roughly 300 to 800 lines, one per sitting.

## Order (top-down along the game flow)

Mark a block `[x]` when done.

- [ ] 1. **Foundations** (`Helpers/`): `StateMachine`, `IState`, `Singleton`.
- [ ] 2. **Game flow** (`GameStates/`): `GameStateMachine`, `SceneState`, MainMenu, Lobby, InGame
      (the three turn phases), GameOver.
- [ ] 3. **Data model** (`Gameplay/`): `Country`, `Player`, `Army`, `Region`, `GameResources`,
      `MapGraph`, `ResourceType`, `SoldierType`, `MatchSettings`, ScriptableObjects (`GameRules`,
      `Building`).
- [ ] 4. **Match core**: `GameController`, `Combat`, `Dice`, `Market`, `DiplomacySystem`,
      `FogOfWar`, `TurnReport`.
- [ ] 5. **View-side rules and map**: `CountryPicker`, `MapView`, `CountryMarker`, `MapCamera`,
      `PlayerPalette`, `IncomePopup`. Revisit: turn rules live in the view, not in `Gameplay`.
- [ ] 6. **UI**: `UiFactory`, `UiTheme`, `ThemedText`, `Format`, `InGameHud`, the panels, menus.
- [ ] 7. **AI**: `AiBrain`, `AiDriver`, `AiProfile`, `AiSettings`.
- [ ] 8. **Save**: `MatchSnapshot`, `MatchSave`, `SaveSystem` (a redesign is already expected, see
      `Architecture.md` > Save).
- [ ] 9. **Audio and editor tools**: `Audio/`, `View/SoundCues`, `Editor/`.

## Planned work after the walkthrough (already agreed)

- In-game developer console, per the rules in `CLAUDE.md`. Command handlers call the existing public
  API (`GameController`, `GameStateMachine`) as the single entry point, so that layer can later move
  server-side; do not build a fake server now.

## Findings: refactors

(none yet)

## Findings: fixes (bugs, gaps in the rules)

(none yet)
