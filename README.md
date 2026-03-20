# Bannerlord.GABS

Let AI agents play Mount & Blade II: Bannerlord.

Bannerlord.GABS is a mod that exposes 90+ game tools via the [GABS](https://github.com/pardeike/GABS) ecosystem, enabling AI agents (Claude, GPT, etc.) to observe game state, navigate menus, trade, recruit, negotiate, craft weapons, manage diplomacy, and more — all through tool calls over MCP.

```
AI Agent ←— MCP —→ GABS (Go binary) ←— GABP (TCP) —→ Bannerlord.GABS (this mod) ←— Game API —→ Bannerlord
```

## What Can It Do?

| Category             | Examples                                                                     |
| -------------------- | ---------------------------------------------------------------------------- |
| **Campaign Map**     | Travel between settlements, recruit troops, buy/sell goods, manage inventory |
| **Conversations**    | Talk to NPCs, navigate dialogue trees, handle persuasion checks              |
| **Diplomacy**        | Become a mercenary, declare war, make peace, change relations                |
| **Trading**          | Check market prices, buy low / sell high, manage caravans                    |
| **Smithing**         | Refine materials, smelt weapons, forge custom weapons — all via UI tools     |
| **UI Interaction**   | Click buttons, read ViewModels, navigate any Gauntlet UI screen              |
| **Combat**           | Auto-resolve battles (Send Troops), manage formations, retreat               |
| **Threat Detection** | Scan for hostiles, direction-aware flee to safety                            |
| **Game Management**  | Start new games, load/save, skip videos, advance time                        |

## Quick Start

### 1. Install Prerequisites

- [GABS](https://github.com/pardeike/GABS/releases) (v0.2.0+) — the MCP server
- [BLSE](https://www.nexusmods.com/mountandblade2bannerlord/mods/1) — Bannerlord Software Extender (recommended launcher)
- [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006), [ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018), [MCM](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) — required BUTR modules

### 2. Install the Mod

Download from [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/10419) or [GitHub Releases](https://github.com/BUTR/Bannerlord.GABS/releases) and extract to:

```
<Game>/Modules/Bannerlord.GABS/
```

### 3. Configure GABS

```bash
gabs games add bannerlord
```

Set the launch command to use the PowerShell script (handles Safe Mode bypass):

```json
{
  "command": "powershell.exe",
  "args": [
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    "path/to/launch-bannerlord.ps1"
  ],
  "workingDirectory": "<Game>/bin/Win64_Shipping_Client",
  "stopProcessName": "Bannerlord.BLSE.Standalone.exe"
}
```

### 4. Add MCP Server

Add GABS as an MCP server in your AI tool's configuration:

**Claude Code** (`~/.claude/settings.json`):

```json
{
  "mcpServers": {
    "gabs": {
      "command": "gabs",
      "args": ["server"]
    }
  }
}
```

### 5. Play

```
games.start { gameId: "bannerlord" }
games.connect { gameId: "bannerlord" }
bannerlord.core.load_save { saveName: "your_save" }
bannerlord.core.wait_for_state { expectedState: "campaign_map" }
```

See [docs/setup-guide.md](docs/setup-guide.md) for detailed setup instructions.

## Tool Categories

| Category         | Tools                                                                                                    | Description                   |
| ---------------- | -------------------------------------------------------------------------------------------------------- | ----------------------------- |
| `core/*`         | ping, get_game_state, load_save, save_game, set_time_speed, check_blockers, new_game, ...                | Game lifecycle and state      |
| `hero/*`         | get_player, get_hero, list_heroes, get_skills, get_traits, get_relationships, kill_hero                  | Hero information and actions  |
| `party/*`        | get_player_party, move_to_settlement, recruit_all, detect_threats, flee_to_safety, wait_for_arrival, ... | Party movement and management |
| `settlement/*`   | list_settlements, get_settlement, get_market_prices, get_workshops                                       | Settlement queries            |
| `kingdom/*`      | list_kingdoms, get_kingdom, get_clan, list_wars                                                          | Kingdom and diplomacy info    |
| `inventory/*`    | get_inventory, buy_item, sell_item, add_gold, give_gold                                                  | Economy and items             |
| `diplomacy/*`    | declare_war, make_peace, change_relation                                                                 | Diplomatic actions            |
| `conversation/*` | start, get_state, select_option, continue, get_persuasion, wait_for_state                                | NPC dialogue                  |
| `menu/*`         | get_current, select_option                                                                               | Game menu interaction         |
| `barter/*`       | get_state, offer_item, accept, cancel                                                                    | Trading with NPCs             |
| `ui/*`           | get_screen, click_widget, get_viewmodel_property, call_viewmodel_method, answer_inquiry, ...             | Universal UI interaction      |
| `battle/*`       | get_state, get_formations, order_charge, order_hold, order_retreat, ...                                  | Battle commands               |
| `quest/*`        | list_quests, get_quest                                                                                   | Quest tracking                |
| `history/*`      | get_recent_events, get_events_by_type                                                                    | Campaign event history        |

## Key Design Principles

### Async Tools with Blocking Awaiters

Action tools return immediately. Use companion awaiter tools to block until completion — **never use `sleep`**.

```
party/move_to_settlement  →  party/wait_for_arrival  →  next action
core/load_save            →  core/wait_for_state     →  next action
conversation/start        →  conversation/wait_for_state  →  read dialogue
```

### UI-First Interaction

Most game screens are accessible through `ui/get_screen`, `ui/click_widget`, and `ui/get_viewmodel_property`. The ViewModel property reader supports:

- **Dot-notation**: `Smelting.SmeltableItemList`
- **Array indexing**: `WeaponDesign.PieceLists[0].Pieces`
- **Deep paths**: `WeaponDesign.PieceLists[0].SelectedPiece.TierText`

## Documentation

### Setup

- [Setup Guide](docs/setup-guide.md) — Install GABS, configure Bannerlord, add MCP, deploy the mod

### Gameplay Guides

- [Getting Started](docs/gameplay/getting-started.md) — Quick start, workflows, safe travel protocol
- [Starting a New Game](docs/gameplay/new-game.md) — Character creation, culture selection
- [Early Game Strategy](docs/gameplay/strategy-early-game.md) — Wealth, recruiting, mercenary service, flee tactics
- [Mid Game Strategy](docs/gameplay/strategy-mid-game.md) — Vassalage, workshops, army composition
- [Late Game Strategy](docs/gameplay/strategy-late-game.md) — Kingdom creation, conquest, endgame
- [Smithing](docs/gameplay/smithing.md) — Refine, smelt, forge with full UI tool workflows
- [Courtship & Marriage](docs/gameplay/courtship-and-marriage.md) — 8-step marriage walkthrough
- [Mercenary Contract](docs/gameplay/mercenary-contract.md) — Becoming a mercenary

### Reference

- [Console Commands](docs/console-commands.md) — 88 campaign console commands
- [Campaign Actions](docs/campaign-actions.md) — 61 action classes
- [Gauntlet UI](docs/gauntlet-ui.md) — Screen/layer/widget architecture
- [Barter System](docs/barter-system.md) — Barter API reference

## Architecture

```
Bannerlord.GABS/
├── AGENTS.md                          # Agent instructions (read by AI tools)
├── README.md                          # This file
├── launch-bannerlord.ps1              # Game launcher with Safe Mode bypass
├── docs/
│   ├── setup-guide.md
│   ├── gameplay/                      # Strategy and workflow guides
│   └── *.md                           # API reference docs
└── src/
    ├── Bannerlord.GABS.sln
    └── Bannerlord.GABS/
        ├── SubModule.cs               # Mod entry point, GABP server setup
        ├── MainThreadDispatcher.cs    # Thread-safe game API access
        └── Tools/
            ├── CoreTools.cs           # Game state, saves, time, commands
            ├── HeroTools.cs           # Hero queries and actions
            ├── PartyTools.cs          # Party movement, combat, flee
            ├── SettlementTools.cs     # Settlement and market queries
            ├── KingdomTools.cs        # Kingdom and diplomacy
            ├── InventoryTools.cs      # Economy and items
            ├── ConversationTools.cs   # NPC dialogue system
            ├── MenuTools.cs           # Game menu interaction
            ├── BarterTools.cs         # Barter/trade with NPCs
            ├── BattleTools.cs         # Battle commands
            ├── GauntletUITools.cs     # Universal UI interaction
            ├── QuestTools.cs          # Quest tracking
            ├── HistoryTools.cs        # Campaign event history
            └── DiplomacyTools.cs      # War, peace, relations
```

## Building from Source

Requires:

- .NET SDK 8.0+
- `BANNERLORD_BETA_DIR` or `BANNERLORD_GAME_DIR` environment variable pointing to your game install
- `supported-game-versions.txt` in the project root (e.g. `v1.3.15`)

```bash
dotnet build src/Bannerlord.GABS/Bannerlord.GABS.csproj -c Beta_Debug
```

The SDK auto-deploys the built mod to `<Game>/Modules/Bannerlord.GABS/`.

## Dependencies

- [Lib.GAB](https://github.com/pardeike/Lib.GAB) — GABP protocol library (NuGet)
- [GABS](https://github.com/pardeike/GABS) — Game Agent Bridge Server (Go binary)
- [BLSE](https://www.nexusmods.com/mountandblade2bannerlord/mods/1) — Bannerlord Software Extender
- [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006), [ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018), [MCM](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) — BUTR community modules

## License

[MIT](LICENSE)
