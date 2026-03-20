# Early Game Strategy (Clan Tier 0-2)

## First Steps After Spawning

1. Check position and nearest town: `settlement/list_settlements { type: "town", nearPlayer: true, limit: 5 }`
2. Travel to the nearest town: `party/move_to_settlement` + `party/wait_for_arrival`
3. Recruit troops from notables: `party/recruit_all`
4. Buy diverse food (grain, fish, meat, cheese) to maximize morale: `inventory/buy_item`
5. Check for available quests from NPCs in town

## Building Wealth

### Tournaments (Earliest Money)
> **Note:** Tournaments require manual 3D combat — AI agents cannot participate directly. This info is included for context when advising human players or when battle tools are extended.
- Check towns for active tournaments: `settlement/get_settlement` → `hasTournament`
- Tournaments award high-tier gear and renown with zero risk to your party
- Wager on yourself for bonus gold each round

### Livestock Trading (Fast Early Cash)
- Buy hogs and sheep from village markets
- Butcher them (inventory), sell meats and hides back to the town
- Can earn up to 4,000 denars in the first few days

### Trade Routes
- Use supply and demand — buy cheap goods where they're produced, sell where scarce
- Even grain can be sold at profit if a town is starving
- Check market prices: `settlement/get_market_prices`
- Keep pack animals (horses, mules) for carrying capacity

### Weapon Smelting
> **Smithing is available to AI agents** via the UI tools. Enter the smithy from town menu, use `get_viewmodel_property` with dot-notation paths to read item names and materials. Buttons respect disabled state — if you lack materials, the action button will refuse to click.

**Smithing workflow:**
1. Buy Hardwood and cheap weapons from the town market
2. Enter smithy: `menu.select_option` (index for "Enter smithy")
3. **Refine** (Hardwood → Charcoal): Click `RefinementCategoryButton`, select a recipe via `call_viewmodel_method_at_index` on `Refinement.AvailableRefinementActions`, click `MainActionButtonWidget`
4. **Smelt** (Weapon → Materials): Click `SmeltingCategoryButton`, read items via `Smelting.SmeltableItemList`, select with `ExecuteSelection`, click `MainActionButtonWidget`
5. **Forge** (Materials → Weapon): Click `CraftingCategoryButton`, configure parts, click `MainActionButtonWidget`
6. Check materials: `get_viewmodel_property { propertyName: "PlayerCurrentMaterials", subProperties: "ResourceName,ResourceAmount" }`

Smelting cheap weapons (daggers, hatchets) is a fast way to level smithing and accumulate crafting materials for profitable high-tier weapon forging.

## Recruiting and Army Composition

### Troop Quality Over Quantity
- A few high-tier troops beat many low-tier ones
- Focus on upgrading troops consistently — don't recruit more than you can feed
- Mounted troops increase party speed on the campaign map, critical for catching/escaping

### Early Targets
> **Note:** AI agents cannot play 3D battles. All combat is handled via auto-resolve. This means troop quality and numbers matter even more — you can't compensate with player skill.
- Fight looters (3-15 units) — safe XP and loot
- Fight deserters and other hostile non-kingdom parties for loot and renown
- Avoid sea raiders and mountain bandits early — they're much tougher
- Sell captured bandits for ransom or recruit them
- Sell looted equipment at towns for gold — battle loot is a major early income source
- As a mercenary, earn gold per enemy party defeated (see Mercenary Service below)

### Companion Recruitment
- Visit taverns in major towns to find companions
- Priority roles: **Surgeon** (medicine skill), **Scout** (scouting), **Quartermaster** (steward)
- Assign companion roles in party screen for skill-based bonuses

## Building Renown

Renown increases clan tier, which unlocks everything:

| Clan Tier | Renown | Unlocks |
|-----------|--------|---------|
| 0 | 0 | Starting tier |
| 1 | 50 | Mercenary contracts, marriage |
| 2 | 150 | Vassalage |
| 3 | 350 | Create armies |
| 4 | 900 | Kingdom creation |

**Renown sources:**
- Winning battles (proportional to enemy strength)
- Completing quests
- Tournament victories (requires manual combat — not available to AI agents)
- Ransoming/releasing prisoners

## Mercenary Service (Tier 1)

Becoming a mercenary is the safest early progression path:

1. Reach Clan Tier 1 (50 renown)
2. Find any lord of the target kingdom: `hero/list_heroes { faction: "Northern Empire", limit: 5 }`
3. Start conversation and choose: "I would like to enter the service of [ruler name]"
4. Select: "My sword is yours. For the right sum."
5. Accept the offer

**Benefits:** Daily gold income per enemy party defeated, access to kingdom territory, war participation for renown.

See [mercenary-contract.md](mercenary-contract.md) for full dialogue walkthrough.

## Quests from NPCs

Talk to notables in settlements to get quests:
1. Enter a settlement: `party/enter_settlement`
2. Start conversation with a notable: `conversation/start { nameOrId: "notable_name" }`
3. Ask: "Is there anything I can do for you?" or similar
4. Completing quests builds relation with notables, unlocking better recruits

## Fleeing from Threats

Traveling without troops is extremely dangerous — bandits attack constantly. Use these tools to avoid combat:

1. **Detect threats before they catch you**: `party/detect_threats { range: 20 }`
   - Returns nearby hostile parties with distance and speed comparison
   - If `canOutrun: true`, you can flee; if false, you'll be caught
2. **Flee to nearest safe settlement**: `party/flee_to_safety`
   - Considers ALL nearby threats — combines direction vectors so you don't flee from one bandit into another
   - Picks a friendly town/castle in the safest direction (away from combined threat vector)
   - Closer threats have more influence on the flee direction
   - Returns `isSafeDirection: true/false` — if false, you may be surrounded
3. **When caught**: If `wait_for_arrival` returns `reason: "menu"` with `encounter_meeting`:
   - Try "Leave" from the encounter menu (index 9) if enabled
   - Or select "Maybe we can work out something" → bribe via barter
   - **Never** auto-resolve ("Send troops") with fewer than 10 trained troops
4. **After escaping**: `detect_threats` again before resuming travel

**Horses increase map speed** — carry extra horses in inventory to outrun bandit parties.

## Key Mistakes to Avoid

- **Don't travel without troops** — bandits attack constantly, you WILL be captured
- **Don't over-recruit** — more troops = more food cost = bankruptcy
- **Don't pick fights with lords** — even small noble parties have elite troops
- **Don't ignore food diversity** — same food type reduces morale bonus
- **Don't sell your horses** — they increase map speed and carrying capacity
- **Don't rush vassalage** — mercenary service is safer for building wealth first
- **Don't auto-resolve with raw recruits** — you will lose and get captured
