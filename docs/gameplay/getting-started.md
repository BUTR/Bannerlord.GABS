# Getting Started with Bannerlord.GABS

## Quick Start

### 1a. Start the Game and Load a Save

```
games.start { gameId: "bannerlord" }
games.connect { gameId: "bannerlord" }
bannerlord.core.load_save { saveName: "your_save" }
bannerlord.core.wait_for_state { expectedState: "campaign_map" }       ← blocks until loaded
```

### 1b. Start a New Game

```
games.start { gameId: "bannerlord" }
games.connect { gameId: "bannerlord" }
bannerlord.core.wait_for_state { expectedState: "InitialState" }       ← wait for main menu
bannerlord.core.new_game {}                                             ← triggers sandbox campaign
```

After `new_game`, the intro video plays. Skip it immediately:

```
bannerlord.core.skip_video {}                                           ← skip the campaign intro video
```

Then character creation begins. Navigate each screen with `ui/get_screen` and `ui/click_widget`:

1. **Culture** — Pick a culture button (e.g. "Empire"), click "Next"
2. **Face** — Skip with "Next" (uses defaults)
3. **Background stages** (4 screens) — Pick one option per screen, click "Next" each time
4. **Age** — Pick "20", "30", "40", or "50", click "Next"
5. **Banner** — Skip with "Next"
6. **Clan naming** — Skip with "Next" (uses default name)
7. **Review** — Click "Next"
8. **Options** — Click "Start Game"

Then `wait_for_state { expectedState: "campaign_map" }` to confirm you're on the map.

### 2. Check Your Situation

```
bannerlord.core.check_blockers {}                                       ← ensure clean state
bannerlord.hero.get_player {}                                           ← who are you?
bannerlord.party.get_player_party {}                                    ← troops, food, position
bannerlord.kingdom.get_player_kingdom {}                                ← clan tier, kingdom, renown
bannerlord.quest.list_quests {}                                         ← active quests
```

### 3. Travel to a Settlement

```
bannerlord.settlement.list_settlements { type: "town", nearPlayer: true, limit: 5 }
bannerlord.party.move_to_settlement { settlementNameOrId: "Epicrotea" }
bannerlord.party.wait_for_arrival {}                                    ← blocks until arrived
```

`wait_for_arrival` returns early if interrupted (bandit encounter, random event, inquiry popup). Check the `reason` field — if interrupted, handle the interruption then retry travel.

### 4. Enter and Interact

```
bannerlord.party.enter_settlement { settlementNameOrId: "Epicrotea" }
bannerlord.core.wait_for_state { expectedState: "game_menu" }          ← blocks until menu appears
bannerlord.menu.get_current {}                                          ← see settlement options
```

### 5. Talk to NPCs

```
bannerlord.settlement.get_settlement { nameOrId: "Epicrotea" }         ← check heroesPresent
bannerlord.conversation.start { nameOrId: "HeroName" }
bannerlord.conversation.wait_for_state {}                               ← blocks until dialogue appears
bannerlord.conversation.get_state {}                                    ← read text + options
bannerlord.conversation.select_option { index: 0 }                     ← pick an option
bannerlord.conversation.continue {}                                     ← advance NPC's response
```

Repeat `get_state` → `select_option` → `continue` until the conversation ends (`isActive: false`).

### 6. Leave

```
bannerlord.party.leave_settlement {}
bannerlord.core.check_blockers {}                                       ← verify clean state before next action
```

## Common Workflows

### Recruit Troops and Buy Food

```
bannerlord.party.recruit_all { settlementNameOrId: "Epicrotea" }
bannerlord.menu.select_option { index: 11 }                            ← "Trade" (check index with get_current)
bannerlord.inventory.buy_item { itemName: "Grain", quantity: 20 }
bannerlord.ui.click_widget { widgetId: "ConfirmButton" }               ← close trade screen
```

### Advance Time (Wait 1 Day)

```
bannerlord.core.check_blockers {}                                       ← CRITICAL: ensure no overlays blocking time
bannerlord.party.leave_settlement {}                                    ← must be on campaign map
bannerlord.core.set_time_speed { speed: 4 }
bannerlord.core.get_campaign_time {}                                    ← poll until desired time
bannerlord.core.set_time_speed { speed: 0 }
```

**Warning:** Time will NOT advance if any of these are active:
- MapConversation overlay (dismiss with `ui/click_widget { widgetId: "ContinueButton" }`)
- Inquiry popup (handle with `ui/answer_inquiry`)
- Active conversation
- Mission scene

Always call `core/check_blockers` first.

### Handle Interruptions During Travel

`wait_for_arrival` returns early with `interrupted: true` when something happens:

| `reason` | `interruptDetail` | How to handle |
|---|---|---|
| `menu` + `encounter_meeting` | Hostile party caught you | Negotiate, bribe, or flee (see below) |
| `menu` + `town`/`village`/`castle` | Arrived at settlement | Proceed with settlement actions |
| `conversation` | NPC started dialogue | Read `conversation/get_state`, respond |
| `incident` | Random event popup | Answer with `ui/answer_inquiry { affirmative: true, selectedIndices: "0" }` |
| `inquiry` | Decision popup | Answer with `ui/answer_inquiry` |
| `scene_notification` | Death/event banner | Dismiss with `ui/answer_inquiry { affirmative: true }` |
| `screen_change` | UI screen opened unexpectedly | Handle the screen, then resume |
| `arrived` | Reached destination | Continue with next action |

After handling the interruption, call `party/move_to_settlement` + `party/wait_for_arrival` again.

### Safe Travel Protocol

**CRITICAL: Never travel without troops.** Bandits attack constantly and will capture you.

```
# 1. Before moving — scan for threats
party/detect_threats { range: 20 }
# If threats exist with canOutrun: false → recruit troops first, don't travel

# 2. Travel
party/move_to_settlement { settlementNameOrId: "TargetTown" }
party/wait_for_arrival {}

# 3. If interrupted by encounter_meeting:
#    Option A: "Leave" from encounter menu (index 9) if available
#    Option B: Bribe via barter conversation
#    Option C: flee_to_safety, then retry travel

# 4. If caught in a losing situation:
party/flee_to_safety {}                    ← direction-aware, considers ALL nearby threats
party/wait_for_arrival {}                  ← wait until safe at settlement
```

**Speed tips:** Extra horses in inventory increase party speed. Mounted troops are faster than infantry. These are critical for outrunning bandits.

### Use Cheats for Testing

```
bannerlord.core.set_cheat_mode { enabled: true }
bannerlord.core.run_command { command: "campaign.add_renown_to_clan 100" }
bannerlord.core.run_command { command: "campaign.add_gold_to_hero 10000" }
bannerlord.diplomacy.change_relation { heroNameOrId: "HeroName", amount: 100 }
bannerlord.inventory.add_gold { amount: 5000 }
bannerlord.hero.kill_hero { nameOrId: "HeroName" }
```

## Tool Discovery

Use GABS v0.2.0 tools for efficient discovery:

```
games.tool_names { gameId: "bannerlord", brief: true }                 ← list all tool names
games.tool_names { gameId: "bannerlord", query: "settlement" }         ← search by keyword
games.tool_names { gameId: "bannerlord", prefix: "bannerlord.party" }  ← filter by category
games.tool_detail { tool: "bannerlord.party.move_to_settlement" }      ← full schema for one tool
```

`tool_detail` shows exact parameter names, types, and descriptions — use it before calling unfamiliar tools.

## Tool Categories

| Category | Tools | Purpose |
|---|---|---|
| `core/*` | ping, get_game_state, wait_for_state, check_blockers, get/set_time_speed, set_cheat_mode, run_command, new_game, list_saves, load_save, save_game, wait_for_save | Game lifecycle and state |
| `hero/*` | get_player, get_hero, list_heroes, get_skills, get_traits, get_relationships, kill_hero | Character info and cheats |
| `party/*` | get_player_party, get_party, list_parties, get_troop_roster, get_available_recruits, recruit_troop, recruit_all, move_to_settlement, move_to_point, follow_party, engage_party, enter/leave_settlement, wait_for_arrival | Party management and travel |
| `settlement/*` | list_settlements, get_settlement, get_market_prices, get_workshops | Settlement info |
| `kingdom/*` | get_player_kingdom, list_kingdoms, get_kingdom, get_clan, list_wars | Kingdom and diplomacy info |
| `quest/*` | list_quests, get_quest | Quest tracking |
| `history/*` | get_recent_events, get_events_by_type | Event history |
| `inventory/*` | get_inventory, add_gold, give_gold, buy_item, sell_item | Economy |
| `diplomacy/*` | change_relation, declare_war, make_peace | Diplomacy actions |
| `conversation/*` | get_state, select_option, continue, start, wait_for_state, get_persuasion | Dialogue system |
| `menu/*` | get_current, select_option | Game menu interaction |
| `barter/*` | get_state, offer_item, accept, cancel | Barter/trade deals |
| `battle/*` | get_state, get_formations, order_charge/hold/advance/fallback/follow_me/retreat, delegate_to_ai, set_fire_order | Battle commands |
| `mission/*` | list_agents, talk_to_agent, leave | 3D scene interaction |
| `ui/*` | get_screen, click_widget, get_inquiry, answer_inquiry, wait_for_screen, get/call_viewmodel_property/method | UI layer |

## Key Gotchas

1. **MapConversation overlay blocks time** — After any `conversation/start` → dialogue → end, an invisible overlay may persist. Always call `core/check_blockers` before advancing time. If `map_conversation_overlay` is present, dismiss it with `ui/click_widget { widgetId: "ContinueButton" }`.

2. **Bandit encounters are frequent for small parties** — Recruit troops before long travel. Solo players get intercepted constantly.

3. **Castle entry requires a bribe** — Unlike towns, castles require paying guards. Check `menu/get_current` for the bribe option.

4. **Parameter names must match exactly** — Use `games.tool_detail` to check the exact parameter names. For example, `menu/select_option` takes `index` (not `optionIndex`), and `settlement/get_market_prices` takes `nameOrId` (not `settlementNameOrId`).

5. **No player control in 3D scenes** — The agent cannot move, attack, or interact in missions. The player character stands idle. Use `mission/leave` to exit, or avoid 3D scenes entirely by using `conversation/start` for NPC interaction.

6. **`continue` advances dialogue, `get_state` reads it** — After `select_option`, the NPC responds. Call `continue` to advance past their line, then `get_state` to see the next options. Don't call `get_state` immediately after `select_option` — you'll see stale data.

7. **`click_widget` searches by text or ID** — Most buttons have text but no ID. Pass the exact button text (e.g. `"New Campaign"`, `"Next"`, `"Start Game"`). If text search fails, the error lists all available buttons. As a last resort, use `"__index:N"` to click the Nth button (0-based).
