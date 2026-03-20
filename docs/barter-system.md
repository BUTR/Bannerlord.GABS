# Barter System Reference

> **Assembly:** `TaleWorlds.CampaignSystem.dll`
> **Namespace:** `TaleWorlds.CampaignSystem.BarterSystem` and `TaleWorlds.CampaignSystem.BarterSystem.Barterables`

## Overview

The barter system handles all trade negotiations in Bannerlord — bandit ransoms, peace deals, marriages, faction joining, etc. When a barter screen opens, a `BarterData` object is created containing all available `Barterable` items that can be offered by either side.

## Key Types

### BarterManager (Singleton)

Access via `BarterManager.Instance` (equivalent to `Campaign.Current.BarterManager`).

**Important fields/events:**
- `BarterBegin` — event delegate `BarterBeginEventDelegate(BarterData args)`. Fired when a player barter screen opens. **This is how you capture the active `BarterData`** — the manager does NOT store it as a field.
- `Closed` — event delegate fired when barter screen closes.
- `LastBarterIsAccepted` — bool, whether the last barter was accepted.

**Key methods:**
- `ApplyAndFinalizePlayerBarter(Hero offerer, Hero other, BarterData data)` — Accept the deal. Calls `Apply()` on all offered barterables, fires events, closes the barter screen, and continues the conversation.
- `CancelAndFinalizePlayerBarter(Hero offerer, Hero other, BarterData data)` — Cancel the deal. Closes barter, fires cancel event, continues conversation.
- `IsOfferAcceptable(BarterData args, Hero hero, PartyBase party)` — Returns true if the current offer is acceptable to the given party. Check this before calling accept.
- `GetOfferValueForFaction(BarterData data, IFaction faction)` — Returns the total value of offered items for a faction. Both factions need >= 0 for the deal to work.

### BarterData

Holds the participants and all available barterables for a negotiation.

**Fields:**
- `OffererHero`, `OtherHero` — the two heroes negotiating
- `OffererParty`, `OtherParty` — their parties

**Methods:**
- `GetBarterables()` — returns `List<Barterable>` of ALL available items (both sides)
- `GetOfferedBarterables()` — returns only items where `IsOffered == true`
- `GetBarterGroups()` — returns the barter group categories

### Barterable (Abstract Base)

Each tradeable item in the barter screen.

**Properties:**
- `Name` — display name (e.g. "Let Davobard Go", "Denars")
- `StringID` — identifier (e.g. `"safe_passage_barterable"`, `"gold_barterable"`)
- `IsOffered` — whether this item is currently on the table
- `CurrentAmount` — for stackable items like gold, how much is offered
- `MaxAmount` — maximum offerable (for gold: the owner's total gold)
- `OriginalOwner` — the hero who owns this barterable
- `OriginalParty` — the party that owns it
- `Side` — `Left` or `Right` (which side of the barter screen)

**Methods:**
- `SetIsOffered(bool value)` — offer or unoffer this item
- `GetUnitValueForFaction(IFaction faction)` — value per unit for a faction (positive = good for them, negative = bad)
- `GetValueForFaction(IFaction faction)` — `UnitValue * CurrentAmount`
- `Apply()` — execute the trade (called internally by `ApplyAndFinalizePlayerBarter`)

## Barterable Types

| Type | StringID | Description |
|------|----------|-------------|
| `GoldBarterable` | `gold_barterable` | Gold transfer. `MaxAmount` = owner's gold. Set `CurrentAmount` to desired payment. |
| `SafePassageBarterable` | `safe_passage_barterable` | "Let X Go" — safe passage in encounters. Used by bandits for ransom. |
| `PeaceBarterable` | — | Peace deal between factions |
| `MarriageBarterable` | — | Marriage proposal |
| `JoinKingdomAsClanBarterable` | — | Joining a kingdom |
| `LeaveKingdomAsClanBarterable` | — | Leaving a kingdom |
| `FiefBarterable` | — | Trading fiefs |
| `ItemBarterable` | — | Trading items |
| `NoAttackBarterable` | — | Non-aggression pact |
| `SetPrisonerFreeBarterable` | — | Freeing prisoners |
| `TransferPrisonerBarterable` | — | Transferring prisoners |
| `DeclareWarBarterable` | — | Declaring war |
| `MercenaryJoinKingdomBarterable` | — | Mercenary contract |

## Common Barter Scenarios

### Bandit Ransom (Safe Passage)

When bandits catch the player, a barter opens with:
- **Left side (bandit):** `SafePassageBarterable` (already offered, "Let Player Go") + `GoldBarterable` (bandit's gold)
- **Right side (player):** `GoldBarterable` (player's gold, `MaxAmount` = player's total gold)

To pay the ransom:
1. Find the player's `GoldBarterable` (where `OriginalOwner` is the player hero)
2. Call `SetIsOffered(true)` and set `CurrentAmount` to the ransom price
3. Check `IsOfferAcceptable()` to see if it's enough
4. Call `ApplyAndFinalizePlayerBarter()` to finalize

The required gold amount is calculated by `SafePassageBarterable.GetUnitValueForFaction()` based on:
- Player's total wealth (gold + item values)
- Player's military strength ratio in the encounter
- Whether the enemy is a bandit (bandits charge 1/8 of the base rate)
- Various perks (SweetTalker reduces bandit costs)

### How the GABS BarterTools Work

The tools hook into `BarterManager.BarterBegin` event to capture the active `BarterData` when a barter screen opens. The tools then expose:

1. **`bannerlord.barter/get_state`** — Lists all barterables with their type, name, offered status, amounts
2. **`bannerlord.barter/offer_item`** — Offer/unoffer items by index, set gold amounts
3. **`bannerlord.barter/accept`** — Accept the deal (checks acceptability first)
4. **`bannerlord.barter/cancel`** — Cancel and close the barter

**Important:** The `BarterBegin` hook is lazily initialized on first tool call. If the barter screen was already open before the first tool call in a session, the `_activeBarterData` may be null. The `EnsureHooked()` method handles re-subscribing.

## Architecture Notes

- `BarterManager` does NOT store the active `BarterData` as a field — it's passed via the `BarterBegin` event and then only lives in the UI ViewModel
- The barter screen UI is in `TaleWorlds.CampaignSystem.ViewModelCollection.Barter.BarterVM` (GauntletUI layer)
- After `ApplyAndFinalizePlayerBarter`, the conversation automatically continues via `ConversationManager.ContinueConversation()`
- After `CancelAndFinalizePlayerBarter`, the conversation also continues (typically returning to pre-barter dialogue options)
