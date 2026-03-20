# Mercenary Contract

## Overview

Becoming a mercenary for a kingdom lets you fight alongside that faction in exchange for gold rewards per deed (battles won, enemies defeated). Unlike vassalage, you retain independence — no obligation to attend kingdom votes or answer army summons.

**Requirements:**
- Clan Tier 1 (50 renown)
- Talk to any lord of the target faction

## Step 1: Preparation

### Check Clan Tier

```
bannerlord.kingdom/get_clan { "nameOrId": "PlayerClanName" }  → check tier, renown, renownForNextTier
```

If Clan Tier is 0, you need to gain renown. Options:
- Win battles (natural gameplay)
- Use cheat: enable cheat mode first, then use console command:
  ```
  bannerlord.core/set_cheat_mode { "enabled": true }
  bannerlord.core/run_command { "command": "campaign.add_renown_to_clan 100" }
  ```

### Choose a Faction

Use `bannerlord.kingdom/list_kingdoms` to see available factions, their strength, and current wars. Consider:
- Who are they at war with? You'll be fighting those enemies.
- Where are their territories? You'll be operating in that region.
- Faction strength — stronger factions offer more protection but may need you less.

## Step 2: Find a Lord

You can talk to **any lord** of the target faction — you don't need the faction leader for a mercenary contract (unlike vassalage which requires the leader).

```
bannerlord.hero/get_hero { "nameOrId": "LordName" }  → check currentSettlement
bannerlord.party/move_to_settlement { "settlementNameOrId": "Settlement" }
bannerlord.party/wait_for_arrival  (timeout: 120)
```

Once at the settlement:

```
bannerlord.conversation/start { "nameOrId": "LordName" }
bannerlord.conversation/wait_for_state  → blocks until dialogue appears
```

## Step 3: The Conversation

Navigate the dialogue tree. After each `select_option`, the NPC responds — call `continue` to advance past their line, then `get_state` to see the next options.

1. Greeting → `select_option` to introduce yourself → `continue`
2. "There is something I'd like to discuss." → `main_option_discussions_3`
3. "I would like to enter the service of [Ruler]." → `player_want_to_join_faction_as_mercenary_or_vassal`
4. "My sword is yours. For the right sum." → `player_is_offering_mercenary`
   - **Not clickable?** Check the hint — likely "Your Clan Tier needs to be 1"
5. The lord offers a rate (e.g., "Your reward will be 170 denars for every group of enemies you vanquish")
6. "All right. I accept" → `mercenary_player_accepts`
   - Or reject: "That is lower than what I had in mind." → `mercenary_player_rejects`
7. Confirmation — choose your response style:
   - `player_joined_2` — "Your enemies are my enemies and your honor is my honor." (honorable)
   - `player_joined_3` — "So long as the denars keep flowing, so will the blood of your enemies." (pragmatic)
8. Exit: "Is there anything else?" → `continue` → "I must leave now." → `continue` → farewell → `continue`

## Step 4: Verification

After the conversation, verify the contract:

```
bannerlord.kingdom/get_clan { "nameOrId": "PlayerClanName" }
```

Check:
- `isUnderMercenaryService: true`
- `kingdom` — should show the faction name (e.g., "Northern Empire")

The player's `faction` field in `get_player` will also change to the kingdom name.

## Mercenary vs Vassal

| Aspect | Mercenary | Vassal |
|--------|-----------|--------|
| Clan Tier required | 1 | 2 |
| Who to talk to | Any lord | Faction leader only |
| Relation requirement | None | -10+ with faction leader |
| Rewards | Gold per deed | Fiefs, influence, kingdom votes |
| Obligations | None (fight when you want) | Expected to join armies, attend votes |
| Leave freely | Yes | Costs relation/influence |

## Key Conversation Option IDs

| ID | Meaning |
|----|---------|
| `player_want_to_join_faction_as_mercenary_or_vassal` | "I would like to enter the service of [Ruler]" |
| `player_is_offering_mercenary` | "My sword is yours. For the right sum." (requires Clan Tier 1) |
| `player_is_offering_vassalage` | "I would pledge allegiance..." (requires Clan Tier 2 + faction leader) |
| `mercenary_player_accepts` | Accept the offered rate |
| `mercenary_player_rejects` | Decline the rate |
| `player_joined_2` | Honorable acceptance response |
| `player_joined_3` | Pragmatic acceptance response |
| `player_is_offering_join_cancel` | "Actually, I was going to talk about something else" (back out) |

## Tips

- **The dialogue says the ruler's name wrong sometimes.** When talking to a non-leader lord, the text may say "Emperor [LordName]" instead of the actual ruler. This is a game display quirk — the contract still applies to the correct kingdom.
- **Mercenary pay scales with your party size and renown.** The 170 denars/deed rate is for a small party; larger parties earn more.
- **You keep your clan identity.** Your clan stays independent — you're just under contract. You can leave the mercenary contract later through the kingdom menu.
