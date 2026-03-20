# Mid Game Strategy (Clan Tier 2-4)

## Becoming a Vassal (Tier 2)

Vassalage gives voting rights, fief ownership, and army access:

1. Reach Clan Tier 2 (150 renown) with relation >= -10 with the faction leader
2. Find the **faction leader** (not just any lord)
3. Conversation: "I would like to enter the service of [ruler]" → "I would pledge allegiance"
4. After acceptance, you can vote on kingdom decisions and receive fiefs

**Tip:** Build relation with the ruler before requesting vassalage. Gift gold via barter, complete quests, or fight their wars as a mercenary first.

## Passive Income Streams

### Workshops (Stable Income)
- Buy workshops in towns: costs ~13,000-15,000 denars
- Match workshop type to nearby village production:
  - Villages producing grain → Brewery
  - Villages producing grapes → Wine Press
  - Villages producing hardwood → Wood Workshop
  - Villages producing iron ore → Smithy
- Wine production is consistently profitable across most cities
- Avoid placing workshops in war-zone towns (raids destroy profitability)
- Check available workshops: `settlement/get_workshops`

### Caravans (High Risk, High Reward)
- Assign companions to lead caravans for passive trade income
- Cost: ~15,000-22,000 denars to start (Aserai culture gets 30% discount)
- Vulnerable to enemy parties during war — peace-time income is best
- More profitable with higher Trade skill on the companion

### Fief Income (Vassal)
- Towns generate the most income, followed by castles, then villages
- Prosperity drives tax revenue — prioritize food-producing buildings:
  1. Orchards
  2. Fairgrounds
  3. Aqueducts
  4. Militia barracks
- Smaller garrisons (100-200 high-tier troops) consume less food, allowing prosperity to grow

## Military Growth

### Army Composition
- Prioritize troop quality over quantity
- A balanced army: ~40% infantry, ~30% ranged, ~30% cavalry works for most situations
- Culture-specific elite units are worth the upgrade cost:
  - Empire: Legionaries + Palatine Guard
  - Vlandia: Banner Knights + Sharpshooters
  - Battania: Fian Champions (best archers in the game)
  - Khuzait: Khan's Guard (horse archers)

### Upgrading Troops
- Don't upgrade one at a time — wait until a full group is ready, then batch upgrade
- Vlandian culture bonus (20% more upgrade XP from battles) is extremely valuable here

### Gang Confrontations (City Security)
> **Note:** Gang confrontations are 3D missions — not available to AI agents.
- Defeat criminal gang leaders in cities to boost security and gain renown
- First fight is solo combat, second is with your full party
- Improves the city's economy by reducing crime

## Diplomacy and Relations

### Building Relations
- Vote alongside specific lords in kingdom elections to gain their favor
- Complete quests for settlement notables
- Gift gold through barter
- Release captured lords (instead of ransoming) for relation boost
- Check relations: `hero/get_relationships`

### War Participation
- Participate in kingdom wars for renown and influence
- Influence is the currency for kingdom voting (fief distribution, war/peace decisions)
- Winning battles earns influence — spend it to vote for fiefs

## Companion Management

### Role Assignment
- **Surgeon**: Highest Medicine skill companion → reduces deaths after battle
- **Scout**: Highest Scouting skill → increases map visibility
- **Quartermaster**: Highest Steward skill → increases party size limit
- **Engineer**: Highest Engineering skill → faster siege equipment building

### Governor Assignment
- Assign companions as governors to settlements you own
- **Cultural match matters** — same-culture governor gives loyalty bonus
- Filter companions by culture when recruiting for governor roles

## Prison Breaks (High Risk, High Reward)
> **Note:** Prison breaks are 3D stealth missions — not available to AI agents.
- Free imprisoned nobles from enemy dungeons
- Grants massive Charm and Roguery experience
- Dramatically improves relations with the freed lord and their faction
- Dangerous — failure means imprisonment

## Preparing for Independence

If you plan to create your own kingdom (Tier 4):
- Accumulate 3-5 million gold before breaking away
- Build a strong party (200+ high-tier troops)
- Develop high relations with clans you want to recruit later
- Identify target settlements in defensible positions away from major wars
