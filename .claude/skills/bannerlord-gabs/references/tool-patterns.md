# Bannerlord GABS Tool Patterns

## Async Tools and Their Awaiters

Never use sleep. Every async tool has a blocking awaiter.

| Async Tool | Awaiter | Waits for |
|---|---|---|
| `core/load_save` | `core/wait_for_state` | `expectedState="campaign_map"` |
| `core/save_game` | `core/wait_for_save` | Save file appears on disk |
| `party/move_to_settlement` | `party/wait_for_arrival` | Party arrives or interrupted |
| `party/move_to_point` | `party/wait_for_arrival` | Party arrives or interrupted |
| `party/engage_party` | `party/wait_for_arrival` | Encounter triggers |
| `party/enter_settlement` | `core/wait_for_state` | `expectedState="game_menu"` |
| `conversation/start` | `conversation/wait_for_state` | Dialogue becomes active |
| `mission/talk_to_agent` | `conversation/wait_for_state` | Dialogue becomes active |
| `menu/select_option` (scene entry) | `core/wait_for_state` | `expectedState="mission"` |
| `mission/leave` | `core/wait_for_state` | `expectedState="campaign_map"` or `"game_menu"` |

## Travel Pattern

```
party/move_to_settlement { settlementNameOrId: "X" }
party/wait_for_arrival {}     ← may return interrupted
```

If `interrupted: true`, check `reason`:
- `menu` → arrived at settlement or encounter menu. Check `menu/get_current`.
- `incident` → random event popup. Handle with `ui/get_inquiry` + `ui/answer_inquiry`.
- `scene_notification` → death/event banner. Dismiss with `ui/answer_inquiry { affirmative: true }`.
- `encounter` → met party on road. Handle conversation, then retry travel.

After handling, call `party/move_to_settlement` + `party/wait_for_arrival` again.

## Conversation Pattern

```
conversation/start { nameOrId: "X" }
conversation/wait_for_state {}              ← blocks until dialogue appears
conversation/get_state {}                    ← read text + options
conversation/select_option { index: N }      ← pick option
conversation/continue {}                     ← advance NPC response
conversation/get_state {}                    ← read next options
```

Repeat `get_state` → `select_option` → `continue` until `isActive: false`.

After conversation ends, check for lingering MapConversation overlay:
```
core/check_blockers {}
```
If `map_conversation_overlay` is present: `ui/click_widget { widgetId: "ContinueButton" }`.

## Blocker Check

Before time-sensitive actions (set_time_speed, move_to_settlement), call:
```
core/check_blockers {}
```

Returns `clear: true` when safe, or lists blockers:
- `conversation_active`, `map_conversation_overlay`, `inquiry_active`
- `mission_active:<scene>`, `menu_active:<id>`, `paused`

## Persuasion

During persuasion dialogues, `get_state` returns `SuccessChance` for each option.
Always pick the highest `SuccessChance`. Do NOT guess from text — cynical options often outperform noble ones depending on NPC traits.

## Tool Discovery

```
games.tool_names { gameId: "bannerlord", query: "keyword" }    ← search tools
games.tool_detail { tool: "bannerlord.tool.name" }             ← get exact parameter names
```

Always use `tool_detail` before calling an unfamiliar tool — parameter names must match exactly.
