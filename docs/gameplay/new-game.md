# Starting a New Game

## Quick Flow

```
bannerlord.core.new_game {}
bannerlord.core.skip_video {}
```

Then navigate character creation with `ui/get_screen` and `ui/click_widget`:

1. Culture → pick one, "Next"
2. Face → "Next" (defaults are fine)
3. Family background → pick one, "Next"
4. Childhood → pick one, "Next"
5. Adolescence → pick one, "Next"
6. Youth → pick one, "Next"
7. Young adulthood → pick one, "Next"
8. Age → pick one, "Next"
9. Banner → "Next"
10. Clan naming → "Next"
11. Review → "Next"
12. Options → "Start Game"

## Culture Selection

Culture determines your starting location on the map, available troop types, and a passive bonus. All cultures are viable — pick based on playstyle or geographic goals.

| Culture | Region | Troop Strength | Style |
|---------|--------|---------------|-------|
| **Vlandians** | West | Heavy cavalry, crossbowmen | Feudal knights, lance charges |
| **Sturgians** | North | Heavy infantry, axemen | Norse-inspired, foot soldiers |
| **Empire** | Center | Balanced (legionaries, cataphracts, archers) | Roman-inspired, versatile |
| **Aserai** | South | Fast cavalry, camel riders | Desert traders, mobile warfare |
| **Khuzaits** | East | Horse archers | Mongol-inspired, mounted skirmishing |
| **Battanians** | Northwest | Archers, two-handed infantry | Celtic-inspired, forest fighters |

**For a first game:** Empire is the most versatile. Vlandians are strong for cavalry-focused play. Battanians have excellent archers.

## Background Stages

Each background stage (family, childhood, adolescence, youth, young adulthood) gives skill points in different areas. The button text hints at what skills you'll get:

**Family background:**
| Choice | Skill Focus |
|--------|-------------|
| A landlord's retainers | Leadership, social |
| Urban merchants | Trade, charm |
| Freeholders | Athletics, farming |
| Urban artisans | Crafting, engineering |
| Foresters | Scouting, bow |
| Urban vagabonds | Roguery, throwing |

**General pattern across all stages:**
- Military/commander options → Leadership, Tactics
- Trade/merchant options → Trade, Charm
- Physical/labor options → Athletics, Smithing
- Scholarly/tutor options → Medicine, Engineering, Steward
- Horse/riding options → Riding
- Criminal/gang options → Roguery

**Tip:** The specific skill bonuses from background choices are small. Don't overthink it — focus points (from leveling up) matter much more in the long run.

## Age Selection

| Age | Trade-off |
|-----|-----------|
| **20** | Most focus points to spend, lowest starting skills. Best for min-maxing a specialized build. |
| **30** | Balanced. Good starting skills with room to grow. Recommended for most playthroughs. |
| **40** | High starting skills, fewer focus points. Strong early game but less flexibility. |
| **50** | Highest starting skills, fewest focus points. Powerful immediately but limited growth. Also closer to natural death age. |

**For a first game:** 30 is the sweet spot.

## After "Start Game"

You spawn on the campaign map near your culture's territory with:
- A small party (a few troops)
- ~1000 gold
- Basic equipment

**Immediate priorities:**
1. Check your position: `hero/get_player`, `party/get_player_party`
2. Find the nearest town: `settlement/list_settlements { type: "town", nearPlayer: true, limit: 3 }`
3. Travel there: `party/move_to_settlement`, `party/wait_for_arrival`
4. Recruit troops: `party/recruit_all` (inside the settlement)
5. Buy food: check the market for Grain
6. Start doing quests or fighting looters to build renown

**Note:** You start as an independent clan (Tier 0). You need renown to increase clan tier, which unlocks kingdom membership, armies, and marriage.
