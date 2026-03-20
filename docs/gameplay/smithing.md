# Smithing Guide

Smithing lets you refine raw materials, smelt weapons into crafting materials, and forge new weapons. It's a major income source — crafted high-tier weapons sell for far more than their material cost.

## Accessing the Smithy

Smithing is only available in **towns** (not castles or villages).

1. Enter a town: `party/enter_settlement`
2. Select "Enter smithy" from the town menu: `menu/select_option`
3. The `GauntletCraftingScreen` opens with three tabs: **Smelt**, **Forge**, **Refine**

## Reading Materials

Check your current crafting materials at any time while in the smithy:

```
get_viewmodel_property {
  layerName: "CraftingScreen",
  propertyName: "PlayerCurrentMaterials",
  subProperties: "ResourceName,ResourceAmount"
}
```

Returns 9 material types:
| Material | Source |
|----------|--------|
| Iron Ore | Buy from markets, smelt weapons |
| Crude Iron | Smelt low-tier weapons |
| Wrought Iron | Smelt weapons, refine crude iron |
| Iron | Refine wrought iron |
| Steel | Refine iron |
| Fine Steel | Refine steel |
| Thamaskene Steel | Refine fine steel |
| Hardwood | Buy from markets, smelt weapons with wooden handles |
| Charcoal | Refine hardwood (required fuel for smelting) |

## Step 1: Refine (Hardwood → Charcoal)

Charcoal is required to smelt weapons. Refine hardwood first.

1. Switch tab: `click_widget { widgetId: "RefinementCategoryButton" }`
2. Read available recipes:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "Refinement.AvailableRefinementActions",
     subProperties: "IsEnabled,IsSelected"
   }
   ```
3. Check what a recipe produces:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "Refinement.CurrentSelectedAction.InputMaterials",
     subProperties: "ResourceName"
   }
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "Refinement.CurrentSelectedAction.OutputMaterials",
     subProperties: "ResourceName"
   }
   ```
4. Select a recipe (if not already selected):
   ```
   call_viewmodel_method_at_index {
     layerName: "CraftingScreen",
     listPropertyName: "Refinement.AvailableRefinementActions",
     index: 0,
     methodName: "ExecuteSelectAction"
   }
   ```
5. Click Refine: `click_widget { widgetId: "MainActionButtonWidget" }`

**Common refinement recipes:**
- Hardwood → Charcoal
- Crude Iron + Charcoal → Wrought Iron
- Wrought Iron + Charcoal → Iron
- Iron + Charcoal → Steel

> **Note:** Only recipes where you have sufficient input materials will be enabled. Disabled recipes cannot be selected.

## Step 2: Smelt (Weapon → Materials)

Smelting breaks weapons from your inventory into crafting materials. Requires 1 charcoal per smelt.

1. Switch tab: `click_widget { widgetId: "SmeltingCategoryButton" }`
2. Read smeltable items:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "Smelting.SmeltableItemList",
     subProperties: "Name,NumOfItems,IsSelected,IsLocked"
   }
   ```
3. Select an item:
   ```
   call_viewmodel_method_at_index {
     layerName: "CraftingScreen",
     listPropertyName: "Smelting.SmeltableItemList",
     index: 0,
     methodName: "ExecuteSelection"
   }
   ```
4. Click Smelt: `click_widget { widgetId: "MainActionButtonWidget" }`
   - If you lack charcoal, this button will be **disabled** and return an error

**Best items to smelt:**
- Cheap weapons from markets: Hatchets, Sickles, Pugio daggers, Iron Pitchforks
- Low cost, yields iron/wrought iron materials plus hardwood from handles
- Battle loot weapons you don't need

## Step 3: Forge (Materials → Weapon)

Forging creates new weapons from your accumulated materials. Crafted weapons can be extremely valuable.

### Select weapon type

1. Switch tab: `click_widget { widgetId: "CraftingCategoryButton" }`
2. Open weapon type selector: `click_widget { widgetId: "FreeModeClassSelectionButton" }`
3. Pick a type: `click_widget { widgetId: "One Handed Sword" }` (or Mace, Two Handed Sword, etc.)
   - Some types may be disabled if you haven't unlocked any parts for them

### Select weapon parts

Each weapon has 3-4 component slots (blade, guard, handle, pommel). The game auto-selects default parts from your unlocked pieces. To customize:

1. Read available parts per slot:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "WeaponDesign.PieceLists[0].Pieces",
     subProperties: "TierText,IsSelected,PlayerHasPiece,IsEmpty"
   }
   ```
   - Slot 0 = blade, 1 = guard, 2 = handle, 3 = pommel (varies by weapon type)
   - Only pieces with `PlayerHasPiece: True` can be used
   - `IsEmpty: True` means "no piece" (valid for optional slots like pommel)

2. Select a piece:
   ```
   call_viewmodel_method_at_index {
     layerName: "CraftingScreen",
     listPropertyName: "WeaponDesign.PieceLists[0].Pieces",
     index: 5,
     methodName: "ExecuteSelect"
   }
   ```

3. Check which piece is selected:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "WeaponDesign.PieceLists[0].SelectedPiece.TierText"
   }
   ```

### Check material cost

Before forging, verify you can afford it:
```
get_viewmodel_property {
  layerName: "CraftingScreen",
  propertyName: "PlayerCurrentMaterials",
  subProperties: "ResourceName,ResourceAmount,ResourceChangeAmount"
}
```
- `ResourceChangeAmount` shows the cost (negative = consumed)
- If any material's `ResourceAmount + ResourceChangeAmount < 0`, the Forge button will be disabled

### Forge the weapon

1. Click Forge: `click_widget { widgetId: "MainActionButtonWidget" }`
   - Button is disabled if: insufficient materials, not enough stamina, or unowned pieces selected

2. The crafting result popup appears. Read the result:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "WeaponDesign.CraftingResultPopup.ItemName"
   }
   ```

3. Finalize the weapon (adds it to inventory):
   ```
   call_viewmodel_method { layerName: "CraftingScreen", methodName: "ExecuteConfirm" }
   ```

4. Verify completion:
   ```
   get_viewmodel_property {
     layerName: "CraftingScreen",
     propertyName: "WeaponDesign.IsInFinalCraftingStage"
   }
   ```
   Should return `False` after finalization.

### Crafting orders

Orders provide specific weapon requests from NPCs with gold rewards. Access via `click_widget { widgetId: "CraftingOrdersButton" }`. Orders are more profitable than free-build because they pay a bounty on top of the weapon's value.

## Stamina

Each character has limited smithing stamina per day. Check stamina via the character selector at the top of the smithy screen. When a character's stamina is depleted:
- Wait in the settlement for time to pass (stamina regenerates daily)
- Switch to another companion with remaining stamina via `click_widget { widgetId: "CurrentCraftingHeroToggleWidget" }`

## Resource Planning

Charcoal is the bottleneck — it's consumed by **both** smelting and forging. Plan ahead:

1. **Calculate total charcoal needed**: (number of weapons to smelt) + (1 for forging) = total charcoal
2. **Calculate hardwood needed**: total charcoal × 2 = hardwood for refining, plus any hardwood the weapon itself costs
3. **Buy enough hardwood upfront** from town markets or forester villages
4. **Refine ALL charcoal first**, then smelt, then forge with the last charcoal

**Example for forging a One Handed Sword (costs 1 crude iron, 4 wrought iron, 1 hardwood, 1 charcoal):**
- Need ~4 smelts to get 4 wrought iron → 4 charcoal for smelting + 1 for forging = 5 charcoal
- 5 charcoal needs 10 hardwood for refining + 1 hardwood for the sword = 11 hardwood total
- Buy 11+ hardwood and 4+ cheap weapons before starting

**Tip:** Smelting yields hardwood from wooden handles, creating a partial loop. But you always lose net hardwood per cycle, so buy excess upfront.

## Profit Strategy

1. Buy cheap daggers and hardwood from town markets
2. Refine hardwood into charcoal
3. Smelt daggers for crafting materials (levels smithing)
4. As smithing skill increases, unlock better weapon parts
5. Forge two-handed swords or polearms — these sell for the highest prices
6. Sell crafted weapons at town markets for massive profit

High-level crafted weapons (Tier V) can sell for 50,000-100,000+ denars while costing only a few thousand in raw materials.

## Exiting the Smithy

Call `ExecuteCancel` on the CraftingVM. You may need to call it **twice** — the first call closes any open sub-popup (weapon class selection, crafting result, etc.), the second actually exits the smithy.

```
call_viewmodel_method { layerName: "CraftingScreen", methodName: "ExecuteCancel" }
```

Verify you've exited by checking `get_screen` — you should see `MapScreen` instead of `GauntletCraftingScreen`. If still in the smithy, call `ExecuteCancel` again.

## Verified Status

| Action | Status | Notes |
|--------|--------|-------|
| Enter smithy | Working | `menu/select_option` for "Enter smithy" |
| Switch tabs | Working | Click `SmeltingCategoryButton`, `CraftingCategoryButton`, `RefinementCategoryButton` |
| Read materials | Working | `get_viewmodel_property` with `PlayerCurrentMaterials` |
| Read smeltable items | Working | Dot-notation: `Smelting.SmeltableItemList` with `Name,NumOfItems` |
| Select item for smelting | Working | `call_viewmodel_method_at_index` with `ExecuteSelection` |
| Smelt weapon | Working | Click `MainActionButtonWidget` (respects disabled state) |
| Read refinement recipes | Working | `Refinement.AvailableRefinementActions` with input/output materials |
| Select refinement recipe | Working | `call_viewmodel_method_at_index` with `ExecuteSelectAction` |
| Refine materials | Working | Click `MainActionButtonWidget` |
| Read forge material costs | Working | `PlayerCurrentMaterials` with `ResourceChangeAmount` |
| Select weapon type | Working | Click weapon type text (e.g. "One Handed Sword") |
| Forge weapon | Working | Click `MainActionButtonWidget`, then `ExecuteConfirm` to finalize |
| Check stamina | Working | `CurrentCraftingHero.CurrentStamina` |
| Exit smithy | Working | `call_viewmodel_method { methodName: "ExecuteCancel" }` — call twice if sub-popups are open |
