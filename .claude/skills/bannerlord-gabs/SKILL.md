---
name: bannerlord-gabs
description: This skill should be used when the user wants to play, test, or interact with Mount & Blade II Bannerlord through the GABS MCP server. Activates for game control (start/stop/load), campaign map actions (travel, trade, recruit, diplomacy), NPC conversations, barter, battles, and any mention of Bannerlord gameplay via GABS/GABP tools.
---

# Bannerlord GABS

Play Mount & Blade II: Bannerlord through the GABS (Game Agent Bridge Server) MCP tools. All game interaction happens via `games.*` management tools and `bannerlord.*` game-specific tools called through `games.call_tool`.

## Game Lifecycle

```
games.start { gameId: "bannerlord" }
games.connect { gameId: "bannerlord" }
bannerlord.core.load_save { saveName: "save_name" }
bannerlord.core.wait_for_state { expectedState: "campaign_map" }
```

To stop: `games.stop` (graceful) or `games.kill` (force terminate).

## Core Principle: Never Sleep

All async tools have blocking awaiters. Never use sleep, delay, or polling loops. The pattern is always:

1. Call the action tool (returns immediately)
2. Call the awaiter tool (blocks until done)
3. Proceed

Read `references/tool-patterns.md` for the full async-awaiter table and all interaction patterns.

## Before Any Time-Sensitive Action

Call `bannerlord.core.check_blockers` to verify the game is in a clean state. Common blockers that prevent time from advancing:
- `map_conversation_overlay` — dismiss with `ui/click_widget { widgetId: "ContinueButton" }`
- `inquiry_active` — handle with `ui/get_inquiry` + `ui/answer_inquiry`
- `mission_active` — exit with `mission/leave`

## Conversations

To talk to NPCs at a settlement, use `conversation/start` (not 3D scenes). The player character has no movement or combat controls in 3D missions — always prefer map conversations.

After each `select_option`, the NPC responds. Call `continue` to advance past their line, then `get_state` to see next options. Do NOT call `get_state` immediately after `select_option`.

For persuasion: always pick the option with the highest `SuccessChance` from `get_state`. Text is misleading — cynical options often outperform noble ones.

## Travel

Use `party/move_to_settlement` + `party/wait_for_arrival`. Handle interruptions (bandits, random events, inquiries) by checking the `reason` field, resolving the interruption, then retrying travel.

Recruit troops before long journeys — solo players get intercepted by bandits constantly.

## Tool Discovery

To find tools: `games.tool_names { gameId: "bannerlord", query: "keyword", brief: true }`
To check parameters: `games.tool_detail { tool: "bannerlord.tool.name" }`

Parameter names must match exactly. Always check `tool_detail` before calling an unfamiliar tool.

## Project Documentation

For detailed reference, read these files from the project root:
- `AGENTS.md` — Architecture, full async-awaiter tables, interaction modes, dependencies
- `docs/gameplay/getting-started.md` — Quick start, common workflows, tool categories, all gotchas
- `docs/gameplay/courtship-and-marriage.md` — Marriage guide with dialogue IDs and persuasion tips
- `docs/gameplay/mercenary-contract.md` — Mercenary contract conversation flow
- `docs/console-commands.md` — 88 campaign console commands
- `docs/campaign-actions.md` — 61 campaign action classes
- `docs/barter-system.md` — Barter API reference
