# Border Inquisition: gameplay rules as built

The rules the code implements. The numbers live in the assets (`GameRules`, buildings, regions). `GDD.md` is the user's design
document and is not kept in step with this file.

The map is fixed and hand-authored: 35 territories across six
continents (Vargmark, Zlatokraj, Higanshu, Aureliana, Kanembara, Ashqaran — each named from a
different real-world tradition, listed in `Editor/MapSetup.cs`), not procedural.


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


## The user's to decide or make (do not pick these yourself)

- The numbers: soldier prices, completion bonuses, building costs and effects (all set, tuned by
  play-testing), per-country incomes. Read the current values from the assets.
- Starting armies and incomes — random stand-ins from `GameRules` until designed.
- Buildings on conquest: built as per-building keep/raze with 50 % loot of the base cost — those
  details were Claude's pick and are the user's to change.
- More ScriptableObjects besides `GameRules` will be needed; they are made when a feature asks.
- The building assets are in `Scripts/ScriptableObjects/Buildings` (House, Tavern, Church,
  Barracks, LumberMill, Quarry); AI profiles in `Scripts/ScriptableObjects/AI`.

