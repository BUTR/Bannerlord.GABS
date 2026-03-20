# Bannerlord.GABS

A Mount & Blade II: Bannerlord mod that exposes game functionality as GABP (Game Agent Bridge Protocol) tools, allowing AI agents to observe and interact with the game.

```
AI Agent <-- MCP --> GABS (Go binary) <-- GABP (TCP/JSON-RPC) --> Bannerlord.GABS (this mod) <-- Game API --> Bannerlord
```

- **GABS** — Go binary, MCP server for AI agents. Manages game lifecycle (start/stop/connect).
- **GABP** — JSON-RPC 2.0 over TCP (LSP-style framing). Protocol between GABS and game mods.
- **Lib.GAB** — C# NuGet library handling GABP protocol, tool registry, and event channels.
- **Bannerlord.GABS** — This mod. Acts as a GABP **server** (listens on a port); GABS connects as a **client**.

## Build

- Configurations: `Beta_Debug`/`Beta_Release` (beta branch) and `Stable_Debug`/`Stable_Release` (stable branch)
- Game path resolved via env variables (set in `Advanced.targets` from [Bannerlord.BuildResources](https://github.com/BUTR/Bannerlord.BuildResources)):
  - `BANNERLORD_STABLE_DIR` — for `Stable_Debug`/`Stable_Release`
  - `BANNERLORD_BETA_DIR` — for `Beta_Debug`/`Beta_Release`
  - `BANNERLORD_GAME_DIR` — generic fallback when the config-specific var is empty
- `supported-game-versions.txt` in the project/solution root controls which game version's reference assemblies are used:
  - Line 1 = Beta version, Line 2 = Stable version, Last line = Minimal version (format: `v1.2.3`)
  - The SDK auto-resolves `Bannerlord.ReferenceAssemblies` NuGet based on this + configuration
- Assembly name is auto-set to `{ModuleId}.{GameVersion}` (e.g. `Bannerlord.GABS.v1.3.15`)
- Solution: `src/Bannerlord.GABS.sln`
- Target: net472 (default netstandard2.0 overridden by project)
- Source Link is enabled by default (GitHub). `EmbedUntrackedSources` is on for deterministic builds

## Architecture

- **Lib.GAB** — NuGet package (`Lib.GAB 0.1.0`) providing the GABP server, tool registry, and protocol types. Source lives at https://github.com/BUTR/Lib.GAB
- **Bannerlord.GABS.Generators** — Source generator that produces `partial` method declarations and response schema code for tools
- **Bannerlord.GABS** — The mod itself; tool implementations live in `src/Bannerlord.GABS/Tools/`

### Tool Implementation Pattern

Tools are C# methods annotated with `[Tool("category/name")]` and `[ToolParameter]` attributes. The source generator creates partial method stubs. Parameters are matched by **exact name** against incoming JSON keys (case-insensitive fallback in Lib.GAB 0.1.0, with trace warnings on mismatch).

### Key Components

- `MainThreadDispatcher` — Queues work from GABP handler threads onto Bannerlord's main thread
- `SubModule.cs` — Mod entry point, starts the GABP server and registers tool instances
- `Tools/*.cs` — One file per tool category (Party, Settlement, UI, Battle, etc.)

## Async Tools with Blocking Awaiters

Tools that trigger game state transitions are **async** — they return immediately with a confirmation message while the game processes the action in the background. Each async tool has a corresponding **blocking awaiter** that polls until the transition completes. This means callers **never need `sleep`, `delay`, or manual polling loops**.

### Pattern: Action → Await → Continue

```
action tool     →  returns immediately ("Moving to X", "Loading save: X")
awaiter tool    →  blocks until expected state is reached, returns final state
next action     →  safe to proceed
```

### Async Tools and Their Awaiters

| Async Tool | Awaiter | Waits for |
|---|---|---|
| `core/load_save` | `core/wait_for_state` | `expectedState="campaign_map"` |
| `core/save_game` | `core/wait_for_save` | Save file to appear on disk |
| `party/move_to_settlement` | `party/wait_for_arrival` | Party arrives or is interrupted |
| `party/move_to_point` | `party/wait_for_arrival` | Party arrives or is interrupted |
| `party/engage_party` | `party/wait_for_arrival` | Encounter triggers |
| `party/enter_settlement` | `core/wait_for_state` | `expectedState="game_menu"` |
| `conversation/start` | `conversation/wait_for_state` | Dialogue becomes active |
| `mission/talk_to_agent` | `conversation/wait_for_state` | Dialogue becomes active |
| `menu/select_option` (scene entry) | `core/wait_for_state` | `expectedState="mission"` |

### `wait_for_arrival` Interrupt Types

`party/wait_for_arrival` blocks until the party arrives or is interrupted. Always check the `reason` field to determine what happened:

| `reason` | `interruptDetail` example | What happened | Agent should |
|---|---|---|---|
| `menu` | `Game menu: encounter_meeting` | Hostile party caught you | Check conversation, flee/bribe/fight |
| `menu` | `Game menu: town` | Arrived at a town | Proceed with settlement actions |
| `menu` | `Game menu: village` | Arrived at a village | Proceed or leave |
| `conversation` | `Conversation with Looters` | NPC initiated dialogue mid-travel | Read `conversation/get_state`, respond |
| `incident` | `Random event: Abundance of troublemakers` | Popup event | Answer via `ui/answer_inquiry` with `selectedIndices` |
| `inquiry` | `Inquiry: ...` | Decision popup | Answer via `ui/answer_inquiry` |
| `scene_notification` | `Scene notification: ...` | Cutscene/notification | Dismiss and continue |
| `screen_change` | `Screen changed to GauntletCraftingScreen` | UI screen opened | Handle the screen, then resume |
| `arrived` | — | Reached destination | Continue with next action |

### Blocking Tools (no awaiter needed)

Most read tools and direct-action tools block until complete and return the result:
- All `get_*` tools (get_state, get_hero, get_screen, etc.)
- `conversation/select_option`, `conversation/continue`
- `menu/select_option` (non-scene transitions)
- `barter/*`, `inventory/*`, `diplomacy/*`
- `ui/click_widget`, `ui/answer_inquiry`

### Ongoing Tools (no completion)

| Tool | Nature | Monitor with |
|---|---|---|
| `party/follow_party` | Continuous escort, no end | `party/get_player_party` |
| `core/set_time_speed` | Changes speed, runs until paused | `core/get_campaign_time` |

### Diagnostic: `core/check_blockers`

Before time-sensitive actions, call `core/check_blockers` to detect anything blocking gameplay:
- `conversation_active` — a conversation is in progress
- `map_conversation_overlay` — conversation UI overlay persists after dialogue ended (click `ContinueButton` to dismiss)
- `inquiry_active` — a popup inquiry needs answering
- `mission_active:<scene>` — inside a 3D mission scene
- `menu_active:<id>` — a game menu is open
- `paused` — time is stopped

## Interaction Modes

Two modes for gameplay interaction:

1. **Pure UI** — emulate the player as closely as possible. Navigate menus, click buttons, use `get_screen` + `click_widget` for everything. Only use raw API for things a player literally cannot do (cheats like `change_relation`, `add_gold`). Use this for demos and showcases.
2. **Mixed** — use raw API calls freely alongside UI for speed. `conversation/start` instead of walking to NPCs, direct `enter_settlement` instead of clicking through menus. Use this for testing functionality.

Default to Mixed unless Pure UI is requested.

### Settlement NPCs

Never enter 3D mission scenes (lord's hall, arena, tavern) to talk to NPCs. When at a settlement, NPCs are available via `conversation/start` from the settlement menu. The 3D scenes have no agent control tools — the player character just stands idle.

## Safe Travel Protocol

**CRITICAL:** Never travel on the campaign map without following this protocol. Bandits attack constantly and will capture you if you're unprepared.

### Before moving
1. Check troop count: `party/get_player_party` — if `troopCount` is 0-2, recruit first
2. Scan for threats: `party/detect_threats { range: 20 }` — check the route is clear
3. If threats exist with `canOutrun: false`, do NOT travel — recruit troops or wait

### During travel
1. Use `party/wait_for_arrival` — it blocks until arrival or interruption
2. If interrupted by `reason: "menu"` with `encounter_meeting`, you've been caught:
   - Check conversation — if bandits demand payment, evaluate options
   - "Leave" from the encounter menu (index 9) if available
   - Or bribe via barter if "Leave" is disabled
   - **NEVER auto-resolve ("Send troops") with fewer than 10 trained troops** — you will lose

### Emergency flee
1. `party/flee_to_safety` — direction-aware, picks a settlement AWAY from the threat
2. Returns `isSafeDirection: true/false` — if false, the only safe settlement is behind the threat (consider fleeing to a map point instead)
3. After fleeing, resume travel only when `detect_threats` shows the route is clear

### Speed tips
- Extra horses in inventory increase party speed (critical for outrunning bandits)
- Mounted troops are faster than infantry
- Roads give speed bonuses — prefer road-connected routes

## MCP Tool Discovery

Claude Code does not handle MCP `notifications/tools/list_changed`. Game-specific tools registered at runtime by GABP are not directly visible. Use the static proxy tools instead:

- `games.tool_names` — compact discovery with filtering and pagination
- `games.tool_detail` — full schema for a single tool (parameters, types, output)
- `games.call_tool` — invoke any game tool by name with arguments

## Documentation

### Setup
- `setup-guide.md` — Install GABS, configure Bannerlord, add MCP server, build and deploy the mod

### Gameplay Guides (`docs/gameplay/`)
- `getting-started.md` — Quick start, common workflows, tool categories, and key gotchas
- `new-game.md` — Starting a new game: character creation, culture selection, age, first steps
- `strategy-early-game.md` — Tier 0-2: wealth building, recruiting, mercenary service, smithing, flee tactics
- `strategy-mid-game.md` — Tier 2-4: vassalage, workshops, caravans, army composition, diplomacy
- `strategy-late-game.md` — Tier 4+: kingdom creation, conquest, vassal management, endgame
- `smithing.md` — Complete smithing guide: refine, smelt, forge with UI tool calls
- `courtship-and-marriage.md` — Full 8-step marriage walkthrough with dialogue option IDs
- `mercenary-contract.md` — Becoming a mercenary: requirements, conversation flow

### Reference (`docs/`)
- `console-commands.md` — 88 campaign console commands with signatures
- `campaign-actions.md` — 61 campaign action classes (MarriageAction, KillCharacterAction, etc.)
- `gauntlet-ui.md` — GauntletUI framework: screens, layers, widgets, ViewModels
- `barter-system.md` — Barter API: BarterManager, barterable types, integration
- `campaign-events.md` — Event subscription analysis and expansion proposals

## Key Game Singletons

```csharp
Campaign.Current                          // Campaign instance (null outside campaign)
Hero.MainHero                             // Player hero
Clan.PlayerClan                           // Player clan
MobileParty.MainParty                     // Player's party
Kingdom.All                               // All kingdoms
Settlement.All                            // All settlements
Hero.AllAliveHeroes                       // All living heroes
Mission.Current                           // Active battle/scene (null on campaign map)
Campaign.Current.ConversationManager      // Dialogue system
Campaign.Current.GameMenuManager          // Game menus
Campaign.Current.LogEntryHistory          // Event history
Campaign.Current.QuestManager             // Active quests
CampaignEvents.*                          // Static event bus (276 events)
```

## Dependencies

- **Lib.GAB** (NuGet) — GABP protocol library
- **Lib.Harmony** — Runtime patching (for inquiry/incident interception)
- **Bannerlord.ButterLib** — DI container, DistanceMatrix, logging, extended lifecycle hooks
- **Bannerlord.MCM** — In-game settings UI (port, auto-start, event toggles)
- **Bannerlord.BUTR.Shared** — Module info helpers

MCM and ButterLib are near-universal in the Bannerlord modding community — treating them as required dependencies is the pragmatic choice.

## Other Notes

- `ui/click_widget` HandleClick doesn't trigger XAML command bindings on some screens (e.g. inventory)
- Player party tracking uses `SetMoveGoToPoint` loop, not `SetMoveEngageParty`
- All game API calls must run on the main thread via `MainThreadDispatcher`
- Check `Campaign.Current != null` before campaign tools, `Mission.Current != null` before battle tools
