// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.GABS.Tools;

public partial class InventoryTools
{
    [Tool("inventory/get_inventory", Description = "List the player party's inventory items.")]
    public partial Task<object> GetInventory(
        [ToolParameter(Description = "Max items to return (default 50)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var max = limit ?? 50;
            var items = new List<object>();

            foreach (var element in party.ItemRoster)
            {
                if (element.IsEmpty || element.EquipmentElement.Item == null)
                    continue;

                var item = element.EquipmentElement.Item;
                items.Add(new
                {
                    /// Item display name
                    name = item.Name?.ToString(),
                    /// Item string ID
                    id = item.StringId,
                    /// Quantity in inventory
                    quantity = element.Amount,
                    /// Item type (e.g. 'OneHandedWeapon', 'BodyArmor')
                    type = item.ItemType.ToString(),
                    /// Item base value in gold
                    value = item.Value,
                    /// Item weight
                    weight = Math.Round(item.Weight, 2),
                    /// Item tier
                    tier = item.Tier.ToString(),
                });

                if (items.Count >= max)
                    break;
            }

            return new
            {
                /// Player hero's current gold
                gold = Hero.MainHero?.Gold ?? 0,
                /// Number of items returned
                itemCount = items.Count,
                /// Array of item objects with name, id, quantity, type, value, weight, tier
                items,
            };
        });
    }

    [Tool("inventory/give_gold", Description = "Give gold from the player hero to another hero.")]
    public partial Task<object> GiveGold(
        [ToolParameter(Description = "Recipient hero name or string ID")] string recipientNameOrId,
        [ToolParameter(Description = "Amount of gold to give")] int amount)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var player = Hero.MainHero;
            if (player == null)
                return new { error = "No main hero" };

            if (amount <= 0)
                return new { error = "Amount must be positive" };

            if (player.Gold < amount)
                return new { error = $"Not enough gold. Have {player.Gold}, need {amount}" };

            var recipient = Hero.FindFirst(h => h.StringId == recipientNameOrId)
                            ?? Hero.FindFirst(h => h.Name?.ToString().Equals(recipientNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (recipient == null)
                return new { error = $"Hero not found: {recipientNameOrId}" };

            GiveGoldAction.ApplyBetweenCharacters(player, recipient, amount);

            return new
            {
                /// Status message
                message = $"Gave {amount} gold to {recipient.Name}",
                /// Player gold after transfer
                playerGoldRemaining = player.Gold,
                /// Recipient gold after transfer
                recipientGold = recipient.Gold,
            };
        });
    }

    [Tool("inventory/add_gold", Description = "Add gold to the player hero (debug/cheat).")]
    public partial Task<object> AddGold(
        [ToolParameter(Description = "Amount of gold to add")] int amount)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var hero = Hero.MainHero;
            if (hero == null)
                return new { error = "No main hero" };

            GiveGoldAction.ApplyBetweenCharacters(null, hero, amount, true);

            return new
            {
                /// Player gold after addition
                gold = hero.Gold,
            };
        });
    }

    [Tool("inventory/buy_item", Description = "Buy an item from the current settlement's market.")]
    public partial Task<object> BuyItem(
        [ToolParameter(Description = "Item name to buy")] string itemName,
        [ToolParameter(Description = "Quantity to buy (default 1)", Required = false)] int? quantity)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var settlement = party.CurrentSettlement;
            if (settlement == null)
                return new { error = "Not in a settlement" };

            if (settlement.Town == null)
                return new { error = $"{settlement.Name} has no market" };

            var qty = quantity ?? 1;
            if (qty <= 0)
                return new { error = "Quantity must be positive" };

            // Find the item in the settlement's roster
            ItemRosterElement? found = null;
            foreach (var element in settlement.ItemRoster)
            {
                if (element.IsEmpty || element.EquipmentElement.Item == null)
                    continue;

                if (element.EquipmentElement.Item.Name?.ToString().Equals(itemName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    found = element;
                    break;
                }
            }

            if (found == null)
                return new { error = $"Item not found in market: {itemName}" };

            var itemElement = found.Value;
            if (itemElement.Amount < qty)
                return new { error = $"Only {itemElement.Amount} available" };

            var price = settlement.Town.MarketData?.GetPrice(itemElement.EquipmentElement) ?? 0;
            var totalCost = price * qty;

            if (Hero.MainHero.Gold < totalCost)
                return new { error = $"Not enough gold. Need {totalCost}, have {Hero.MainHero.Gold}" };

            // Transfer gold and items
            settlement.ItemRoster.AddToCounts(itemElement.EquipmentElement, -qty);
            party.ItemRoster.AddToCounts(itemElement.EquipmentElement, qty);
            GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, settlement, totalCost);

            return new
            {
                /// Status message with item name and cost
                message = $"Bought {qty}x {itemElement.EquipmentElement.Item?.Name} for {totalCost} gold",
                /// Player gold after purchase
                goldRemaining = Hero.MainHero.Gold,
            };
        });
    }

    [Tool("inventory/sell_item", Description = "Sell an item from inventory at the current settlement's market.")]
    public partial Task<object> SellItem(
        [ToolParameter(Description = "Item name to sell")] string itemName,
        [ToolParameter(Description = "Quantity to sell (default 1)", Required = false)] int? quantity)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var settlement = party.CurrentSettlement;
            if (settlement == null)
                return new { error = "Not in a settlement" };

            if (settlement.Town == null)
                return new { error = $"{settlement.Name} has no market" };

            var qty = quantity ?? 1;
            if (qty <= 0)
                return new { error = "Quantity must be positive" };

            // Find the item in party inventory
            ItemRosterElement? found = null;
            foreach (var element in party.ItemRoster)
            {
                if (element.IsEmpty || element.EquipmentElement.Item == null)
                    continue;

                if (element.EquipmentElement.Item.Name?.ToString().Equals(itemName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    found = element;
                    break;
                }
            }

            if (found == null)
                return new { error = $"Item not in inventory: {itemName}" };

            var itemElement = found.Value;
            if (itemElement.Amount < qty)
                return new { error = $"Only have {itemElement.Amount}" };

            var price = settlement.Town.MarketData?.GetPrice(itemElement.EquipmentElement) ?? 0;
            var totalValue = price * qty;

            // Transfer items and gold
            party.ItemRoster.AddToCounts(itemElement.EquipmentElement, -qty);
            settlement.ItemRoster.AddToCounts(itemElement.EquipmentElement, qty);
            GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, totalValue);

            return new
            {
                /// Status message with item name and value
                message = $"Sold {qty}x {itemElement.EquipmentElement.Item?.Name} for {totalValue} gold",
                /// Player gold after sale
                goldRemaining = Hero.MainHero.Gold,
            };
        });
    }
}