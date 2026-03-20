# Late Game Strategy (Clan Tier 4+, Kingdom Phase)

## Creating Your Kingdom

Prerequisites:
- Clan Tier 4 (900 renown)
- 3-5 million gold reserve recommended
- 200-300 elite troops (Fian Champions or equivalent)
- Own at least one settlement (captured or granted as vassal)

After leaving your current kingdom, you'll face immediate diplomatic pressure. Be prepared.

## Conquest Strategy

### Sequential Wars
- **Never fight multiple fronts simultaneously** if avoidable
- Pick the weakest neighbor and declare war: `diplomacy/declare_war`
- If attacked by others simultaneously, make peace immediately (pay tribute if needed): `diplomacy/make_peace`
- Focus all resources on one target until they're eliminated or reduced

### Target Priority
1. Capture **castles before towns** along enemy borders — castles act as defensive anchors
2. Garrison castles with 400+ troops to prevent raids and counterattacks
3. Then capture the towns that depend on those castles
4. High-value towns with multiple bound villages: **Sanala** (Aserai), **Jaculan** (Vlandia), **Seonon** and **Marunath** (Battania)

### Prisoner Management
- Keep captured lords **in your party** — do NOT ransom or deposit in dungeons
- While lords are captive, the enemy faction cannot rebuild armies
- Every peace deal frees prisoners, so refuse peace with your primary target
- Accept peace with secondary opponents to reduce multi-front pressure

### Key Military Perks
- **Mounted Patrols** (Riding 225) + **Keen Sight** (Scouting 225) = -100% lord escape chance
- This prevents enemy lords from rebuilding after capture — devastating combination
- Prioritize leveling these skills on your main character

## Vassal Recruitment

### Recruiting Enemy Clans
- During wartime, approach enemy clan leaders with low loyalty to their ruler
- High relationship increases persuasion critical success chance
- Successfully recruited clans bring **all their fiefs** with them — territory without battle
- Check clan relations: `hero/get_relationships`, `kingdom/get_clan`

### Managing Your Vassals
- Distribute fiefs fairly through kingdom votes to keep vassals happy
- Unhappy vassals may defect to other kingdoms
- Use influence to sway votes: earn influence from winning battles and completing kingdom quests

## Settlement Management

### Garrison Philosophy
- **Quality over quantity** — 100-200 tier 6 troops > 400 tier 2 troops
- Smaller garrisons consume less food → prosperity grows faster → more tax income
- High-tier troops provide better security per head

### Building Priority (Towns)
1. Orchards (food + prosperity)
2. Fairgrounds (prosperity + loyalty)
3. Aqueducts (prosperity)
4. Militia barracks (security)
5. Walls (siege defense)

### Governor Selection
- Cultural match is the single biggest loyalty bonus
- Steward skill on governor increases prosperity growth
- Engineering skill speeds construction

### Village Protection
- Position high-prosperity villages away from active front lines
- Raided villages lose prosperity and stop producing trade goods
- This cascades — town workshops lose supply, income drops

## Army Management

### Creating Armies
- Spend influence to summon vassal parties into your army
- Call vassals with: kingdom influence spending
- Army cohesion decreases over time — spend influence to maintain it
- Larger armies (1000+ troops) are possible but expensive in influence

### Delegation
- Once you have 5+ vassals, transition from personal offense to **vassal support**
- Let vassals handle offensive campaigns while you defend newly captured fiefs
- This prevents over-extension and garrison starvation

## Endgame Considerations

### Multi-Kingdom War
- Expect to fight every remaining kingdom simultaneously in the late game
- Use kingdom diplomacy to designate one **offensive** war and keep others **defensive**
- Micro-parties from zero-fief kingdoms raiding your backlines are the biggest nuisance

### Economic Sustainability
- At this point workshops and caravans may be destroyed frequently by war
- Fief income (town taxes) should be your primary revenue
- Keep a gold reserve of 1M+ for emergency garrison reinforcements

### Final Conquest
- The last 20% of map conquest is the most tedious
- Enemy kingdoms fragment into tiny factions that are hard to pin down
- Recruit remaining clans diplomatically when possible — faster than siege warfare
- Use the "execute lord" option sparingly — it permanently damages relations with all other lords
