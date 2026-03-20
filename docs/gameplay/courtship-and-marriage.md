# Courtship and Marriage

## Overview

Marriage in Bannerlord requires multiple steps over several in-game days:

1. **Set relations** (cheat) — Max out relation with the target and their clan leader
2. **Initial flirt** — Start the courtship
3. **Wait 1 day** — Courtship cooldown between stages
4. **Courtship persuasion (Part 1)** — 3 rounds of persuasion checks about your worldview
5. **Wait 1 day** — Another cooldown
6. **Courtship persuasion (Part 2)** — 3 rounds about marriage-specific topics
7. **Family approval** — Seek the clan leader's blessing (barter)
8. **Marriage** — Finalized

## Step 1: Preparation (Cheats)

Set maximum relations with both the target hero and their clan leader:

```
bannerlord.diplomacy/change_relation { "heroNameOrId": "TargetHero", "amount": 100 }
bannerlord.diplomacy/change_relation { "heroNameOrId": "ClanLeader", "amount": 100 }
```

First, check that the **player** is unmarried:
```
bannerlord.hero/get_player  → check spouse is null
```

Then use `bannerlord.hero/get_hero` to find a target:
- Whether the hero has a `spouse` (must be null)
- Their `clan` name
- Their `currentSettlement`

Use `bannerlord.hero/get_traits` to check their personality (helps with persuasion).

### Choosing a Target

- **Player** must be unmarried (`spouse` is null in `get_player`)
- Target must be unmarried (`spouse` is null)
- Must be age 18+ (younger heroes won't show the flirt option)
- Opposite gender from the player character
- Ideally at a settlement you can reach quickly (heroes travel, so they may move)

## Step 2: Find and Travel to the Hero

```
bannerlord.hero/get_hero { "nameOrId": "TargetHero" }  → check currentSettlement
bannerlord.party/move_to_settlement { "settlementNameOrId": "Settlement" }
bannerlord.party/wait_for_arrival  (timeout: 120)
```

**Warning:** Heroes travel between settlements. By the time you arrive, they may have moved. Check with `conversation/start` — if they're gone, it will tell you their new location.

## Step 3: Initial Courtship

Start a conversation and navigate to the flirt option. After each `select_option`, the NPC responds — call `continue` to advance past their line, then `get_state` to see the next options:

1. `bannerlord.conversation/start { "nameOrId": "HeroName" }`
2. `bannerlord.conversation/wait_for_state` — wait for dialogue to appear
3. Navigate: greeting → "There is something I'd like to discuss" → "My lady, I wish to profess myself your most ardent admirer."
3. Select "I wish to offer my hand in marriage."
4. She agrees to meet again — courtship initiated.

The dialogue option IDs to look for:
- `lord_special_request_flirt` — The flirt/courtship entry point
- `lord_start_courtship_response_player_offer` — "I wish to offer my hand in marriage"

## Step 4: Wait for Cooldown

The game requires time to pass between courtship stages. Use the town wait menu:

```
bannerlord.menu/get_current  → find the "Wait here for some time" option (usually town_wait)
bannerlord.menu/select_option { "index": <wait_option_index> }
bannerlord.core/set_time_speed { "speed": 4 }
# Wait ~1 in-game day, then pause. Poll bannerlord.core/get_campaign_time to check.
bannerlord.core/set_time_speed { "speed": 0 }
bannerlord.menu/select_option { "index": 0 }  → "Stop waiting"
```

**Important:** Before setting time speed, verify you are on the MapScreen with no active mission or conversation overlays. Use `bannerlord.core/get_game_state` to confirm `state: "campaign_map"`.

## Step 5: Courtship Persuasion

Start a new conversation. Look for `hero_romance_task_pt1` (first visit) or `hero_romance_task_pt2` (second visit) in the dialogue options.

Each courtship stage has **3 rounds of persuasion**. Each round offers 4-5 options with different skills and success rates.

### Reading Success Rates

**Critical: Use `get_state` to read the success rates!**

The `get_state` response includes `successChance`, `critSuccessChance`, `critFailChance`, and `failChance` (as percentages) for each persuasion option. **Always pick the option with the highest `successChance`.**

Example response:
```json
{
  "index": 0,
  "skillName": "Leadership",
  "successChance": 87.5,
  "failChance": 12.5,
  "critFailChance": 0,
  "critSuccessChance": 0,
  "text": "I feel lucky to live in a time where a valiant warrior can make a name for himself."
}
```

### Do NOT Guess — Use the Numbers

**Do NOT guess which option is best based on the text or skill name.** The success rates depend on:
- The NPC's hidden trait preferences (Valor, Mercy, Honor, Generosity, Calculating)
- Your character's skill levels
- The persuasion difficulty multiplier

Options that sound "noble" or "kind" can have 16% success while "cynical" or "dark" options can have 87%. The only reliable indicator is the `successChance` field.

### Persuasion Outcomes

After each round:
- **"Yes.. You might be correct"** or **"I am happy to hear that"** = Success
- **"I see..."** = Neutral (may succeed or fail)
- **"Hmm. Perhaps you and I have different priorities"** = Failed that round
- **"I... I think this will be difficult"** = Critical failure

After all 3 rounds:
- **"Well.. It seems we have a fair amount in common"** = Passed the stage
- **"In the end, I don't think we have that much in common"** = Failed the stage

## Step 6: Family Approval and Marriage Barter

After passing both courtship persuasion stages (Part 1 and Part 2), the conversation transitions to:

> "I think you should try to win my family's approval."

You must now find the **clan leader** (the target hero's parent/head of clan) and talk to them. This can be the most time-consuming step since clan leaders are often traveling with armies.

### Finding the Clan Leader

```
bannerlord.hero/get_hero { "nameOrId": "ClanLeader" }  → check currentSettlement
```

If `currentSettlement` is null, the clan leader is traveling. Check their party:

```
bannerlord.party/get_party { "nameOrId": "ClanLeader" }  → check army, targetSettlement
```

If the clan leader is in an army (e.g., `army: "Lucon's Army"`), they won't visit settlements on their own. Options:
1. **Wait** — Eventually armies disband and lords return to settlements
2. **Force entry** (cheat) — Use `bannerlord.party/enter_settlement` to place the NPC's party into a settlement:
   ```
   bannerlord.party/enter_settlement { "partyNameOrId": "ClanLeader", "settlementNameOrId": "Settlement" }
   ```
3. **Intercept on the map** — Use `bannerlord.party/engage_party` to meet them, then "Talk to other members" from the army encounter menu. However, army encounter conversations are brief and may not support the marriage dialogue.

### The Marriage Barter

Once at the same settlement as the clan leader, start a conversation. Look for option ID `hero_romance_task_pt3b`:

> "I wish to discuss the final terms of my marriage with [Name]."

This opens a **barter screen** for marriage terms (dowry/bride price).

Use the barter tools:
- `bannerlord.barter/get_state` — See available items and current offers. The marriage barterable (`MarriageBarterable`) will already be offered.
- `bannerlord.barter/offer_item` — Offer gold/items. **Important:** Include both `offer: true` and `amount` for gold:
  ```
  bannerlord.barter/offer_item { "index": 1, "offer": true, "amount": 5000 }
  ```
- `bannerlord.barter/accept` — Finalize the deal. Returns an error if the other party doesn't find the offer acceptable.

With max relations, the clan leader should accept a modest gold offer (e.g., 5,000 denars).

### Completion

After accepting the barter, the clan leader says:

> "Congratulations, and may the Heavens bless you."

The marriage is finalized:
- The target hero's `spouse` field changes to the player's name
- The target hero joins the player's **clan** and **faction**
- The player's `spouse` field updates accordingly

## Troubleshooting

| Problem | Cause | Solution |
|---------|-------|----------|
| No flirt option | Hero too young (<18), already married, or wrong gender | Check `get_hero` for spouse and age |
| "Perhaps we are not meant for each other" | Failed persuasion check | You picked a low-success option. Wait and retry |
| "It is practically not possible for us to be married" | Failed too many persuasion rounds | Courtship is over with this hero. Try someone else or reload |
| No `hero_romance_task_pt1` or `pt2` | Not enough time passed since last courtship interaction | Wait another in-game day |
| Hero moved to different settlement | NPCs travel with their parties | Chase them — `get_hero` shows their `currentSettlement` |
| Clan leader always traveling | Clan leader is in an army | Use `enter_settlement` cheat to place them in a town, or wait for the army to disband |
| "Cheat mode is disabled" for skill XP | Developer console cheats need cheat mode enabled | Use relation cheats instead (always work via `change_relation`) |
| Conversation overlay stuck after ending | MapConversation layer still showing | Click the `ContinueButton` widget to dismiss it |

## Tips and Tricks

### Persuasion: Trust the Numbers, Not the Text

Persuasion option text is **deeply misleading**. An option about "honor and duty" might have 16% success while a cynical remark about "power and ambition" has 87%. This is because success rates depend on the NPC's hidden personality traits — a calculating NPC responds better to pragmatic arguments, not idealistic ones. **Always use `get_state` or `get_persuasion` to read the `successChance` field** before picking an option.

### Use `get_party` to Understand NPC Movement

When an NPC's `currentSettlement` is null, they're on the move — but `get_hero` doesn't tell you *why*. Use `bannerlord.party/get_party` to check:
- `army` — If set (e.g., `"Lucon's Army"`), the NPC is marching with an army and won't stop at settlements voluntarily
- `targetSettlement` — Where they're heading (if any)
- `defaultBehavior` — `"EscortParty"` means following an army leader; `"PatrolAroundSettlement"` means they'll return to a settlement soon

This saves you from waiting endlessly for an NPC who will never arrive.

### Force NPCs into Settlements with `enter_settlement`

The `bannerlord.party/enter_settlement` tool works on **NPC parties**, not just the player. This is invaluable when an NPC is stuck in an army or wandering:

```
bannerlord.party/enter_settlement { "partyNameOrId": "HeroName", "settlementNameOrId": "Town" }
```

Prefer placing them in a town they own or govern — it feels more natural and they're less likely to leave immediately.

### Army Encounters: "Talk to Other Members" Is Non-Interactive

When you encounter a friendly army and select "Talk to other members", you get a list of lords to talk to. However, these conversations complete **instantly** — you cannot interact with dialogue options. The conversation starts and finishes in a single tick, returning you to the army encounter menu.

For any dialogue that requires interactive choices (courtship, persuasion, barter), you **must** talk to the NPC at a settlement using `conversation/start`.

### Always Check Screen State Before Time-Sensitive Actions

Before calling `set_time_speed`, `menu/select_option`, or `menu/get_current`:

```
bannerlord.core/get_game_state  → confirm state: "campaign_map"
bannerlord.ui/get_screen  → check for unexpected layers (MapConversation, MissionScreen)
```

Conversations opened via `conversation/start` create a MapConversation overlay that persists after the conversation ends (`isActive: false`). You must click the `ContinueButton` widget to dismiss it before the town menu becomes usable again. Setting time speed while this overlay is active will cause issues.

### Travel Safety: Recruit Troops Before Long Journeys

With a small party (< 10 troops), you'll be intercepted by every bandit group on the map. Looters, Sea Raiders, and Forest Bandits will all attack. Before traveling long distances:
- Recruit troops at settlements: `bannerlord.party/recruit_all`
- Or add gold and buy better equipment to survive solo fights
- If captured, you'll lose all troops and escape with nothing — then every subsequent bandit encounter becomes a chain of captures

### The `conversation/start` Advantage

**Always prefer `conversation/start` over entering 3D scenes.** It opens a dialogue overlay directly on the map — no loading screens, no walking through 3D environments, no pathfinding issues. Requirements:
- Player must be inside a settlement
- The hero must be at the same settlement
- No other conversation in progress

If the hero isn't at your settlement, the error message helpfully tells you their actual location — use this as a quick location check.

### Barter Parameter Gotcha

The `offer_item` tool requires the `offer` boolean parameter explicitly. Passing just `index` and `amount` will silently fail — the item won't be offered. Always include all three:

```
bannerlord.barter/offer_item { "index": 1, "offer": true, "amount": 5000 }
```

### Age 18 Is the Courtship Threshold — But Barely

Heroes at exactly age 18 may not show the flirt dialogue option even though they technically meet the age requirement. Heroes at age 19+ reliably show courtship options. If a hero at 18 doesn't have the flirt option, try someone a year older.

### Dismiss Battle Scoreboard via ViewModel

After "Leave it to the others" (auto-simulated battles), the scoreboard stays on screen. The "Done" button may not be clickable via `click_widget`. Use:

```
bannerlord.ui/call_viewmodel_method { "layerName": "MapBattleSimulation", "methodName": "ExecuteQuitAction" }
```

## Key Conversation Option IDs

| ID | Meaning |
|----|---------|
| `lord_special_request_flirt` | Start courtship / re-flirt |
| `lord_start_courtship_response_player_offer` | "I wish to offer my hand in marriage" |
| `lord_start_courtship_response_player_offer_2` | "Perhaps you and I..." (alternative) |
| `hero_romance_task_pt1` | First courtship meeting (worldview persuasion) |
| `hero_romance_task_pt2` | Second courtship meeting (marriage persuasion) |
| `hero_romance_task_pt3b` | "I wish to discuss the final terms of my marriage with [Name]" (clan leader conversation) |
| `hero_courtship_argument_1` through `_4` | Persuasion options within each round |
| `lord_ask_recruit_argument_no_answer_2` | Skip/exit persuasion round (safe retreat) |
