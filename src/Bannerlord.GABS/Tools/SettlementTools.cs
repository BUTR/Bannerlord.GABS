// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.GABS.Tools;

public partial class SettlementTools
{
    private static string GetSettlementType(Settlement s)
    {
        if (s.IsTown) return "town";
        if (s.IsCastle) return "castle";
        if (s.IsVillage) return "village";
        if (s.IsHideout) return "hideout";
        return "other";
    }

    [ToolResponse("id", Type = "string", Description = "Settlement string ID")]
    [ToolResponse("name", Type = "string", Description = "Settlement name", Nullable = true)]
    [ToolResponse("type", Type = "string", Description = "Settlement type: town, castle, village, hideout, or other")]
    [ToolResponse("owner", Type = "string", Description = "Owner hero name", Nullable = true)]
    [ToolResponse("ownerClan", Type = "string", Description = "Owner clan name", Nullable = true)]
    [ToolResponse("faction", Type = "string", Description = "Map faction name", Nullable = true)]
    [ToolResponse("positionX", Type = "number", Description = "X map coordinate")]
    [ToolResponse("positionY", Type = "number", Description = "Y map coordinate")]
    [ToolResponse("prosperity", Type = "number", Description = "Town prosperity", Nullable = true)]
    [ToolResponse("loyalty", Type = "number", Description = "Town loyalty", Nullable = true)]
    [ToolResponse("security", Type = "number", Description = "Town security", Nullable = true)]
    [ToolResponse("foodChange", Type = "number", Description = "Food change rate", Nullable = true)]
    [ToolResponse("garrisonCount", Type = "integer", Description = "Garrison troop count", Nullable = true)]
    [ToolResponse("isUnderSiege", Type = "boolean", Description = "Whether under siege", Nullable = true)]
    [ToolResponse("villageType", Type = "string", Description = "Village production type", Nullable = true)]
    [ToolResponse("hearth", Type = "number", Description = "Village hearth", Nullable = true)]
    [ToolResponse("boundTown", Type = "string", Description = "Bound town name (village)", Nullable = true)]
    [ToolResponse("isRaided", Type = "boolean", Description = "Whether village is raided", Nullable = true)]
    [ToolResponse("militia", Type = "number", Description = "Militia strength (detailed)", Nullable = true)]
    [ToolResponse("notables", Type = "array", Description = "Notable NPCs (detailed)", Nullable = true)]
    [ToolResponse("heroesPresent", Type = "array", Description = "Heroes in settlement (detailed)", Nullable = true)]
    [ToolResponse("partiesPresent", Type = "array", Description = "Parties in settlement (detailed)", Nullable = true)]
    [ToolResponse("governor", Type = "string", Description = "Governor name (detailed, town/castle)", Nullable = true)]
    [ToolResponse("construction", Type = "number", Description = "Construction progress (detailed, town/castle)", Nullable = true)]
    [ToolResponse("hasTournament", Type = "boolean", Description = "Whether town has active tournament (detailed, town)", Nullable = true)]
    [ToolResponse("workshopCount", Type = "integer", Description = "Number of workshops (detailed, town)", Nullable = true)]
    [ToolResponse("boundVillages", Type = "array", Description = "Bound village names (detailed, town/castle)", Nullable = true)]
    private static object SerializeSettlement(Settlement s, bool detailed = false)
    {
        var basic = new Dictionary<string, object?>
        {
            ["id"] = s.StringId,
            ["name"] = s.Name?.ToString(),
            ["type"] = GetSettlementType(s),
            ["owner"] = s.Owner?.Name?.ToString(),
            ["ownerClan"] = s.OwnerClan?.Name?.ToString(),
            ["faction"] = s.MapFaction?.Name?.ToString(),
            ["positionX"] = Math.Round(s.GetPosition2D.X, 2),
            ["positionY"] = Math.Round(s.GetPosition2D.Y, 2),
        };

        if (s.IsFortification && s.Town != null)
        {
            basic["prosperity"] = Math.Round(s.Town.Prosperity, 1);
            basic["loyalty"] = Math.Round(s.Town.Loyalty, 1);
            basic["security"] = Math.Round(s.Town.Security, 1);
            basic["foodChange"] = Math.Round(s.Town.FoodChange, 2);
            basic["garrisonCount"] = s.Town.GarrisonParty?.MemberRoster?.TotalManCount ?? 0;
            basic["isUnderSiege"] = s.IsUnderSiege;
        }

        if (s.IsVillage && s.Village != null)
        {
            basic["villageType"] = s.Village.VillageType?.ToString();
            basic["hearth"] = Math.Round(s.Village.Hearth, 1);
            basic["boundTown"] = s.Village.Bound?.Name?.ToString();
            basic["isRaided"] = s.IsRaided;
        }

        if (detailed)
        {
            basic["militia"] = Math.Round(s.Militia, 1);
            basic["notables"] = s.Notables?.Select(n => new { name = n.Name?.ToString(), occupation = n.Occupation.ToString() }).ToList();
            basic["heroesPresent"] = s.HeroesWithoutParty?.Select(h => h.Name?.ToString()).ToList();
            basic["partiesPresent"] = s.Parties?.Select(p => p.Name?.ToString()).ToList();

            if (s.IsTown && s.Town != null)
            {
                basic["governor"] = s.Town.Governor?.Name?.ToString();
                basic["construction"] = Math.Round(s.Town.Construction, 1);
                basic["hasTournament"] = s.Town.HasTournament;
                basic["workshopCount"] = s.Town.Workshops?.Length ?? 0;
                basic["boundVillages"] = s.BoundVillages?.Select(v => v.Name?.ToString()).ToList();
            }

            if (s.IsCastle && s.Town != null)
            {
                basic["governor"] = s.Town.Governor?.Name?.ToString();
                basic["construction"] = Math.Round(s.Town.Construction, 1);
                basic["boundVillages"] = s.BoundVillages?.Select(v => v.Name?.ToString()).ToList();
            }
        }

        return basic;
    }

    [Tool("settlement/get_settlement", Description = "Get detailed info about a settlement by name or string ID.")]
    public partial Task<object> GetSettlement(
        [ToolParameter(Description = "Settlement name or string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == nameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {nameOrId}" };

            return SerializeSettlement(settlement, detailed: true);
        });
    }

    [Tool("settlement/list_settlements", Description = "List settlements with optional filters (type, faction, nearby player).")]
    public partial Task<object> ListSettlements(
        [ToolParameter(Description = "Filter by type: 'town', 'castle', 'village', 'hideout'", Required = false)] string? type,
        [ToolParameter(Description = "Filter by faction name", Required = false)] string? faction,
        [ToolParameter(Description = "If true, sort by distance from player party", Required = false)] bool? nearPlayer,
        [ToolParameter(Description = "Max results (default 50)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 50;
            IEnumerable<Settlement> settlements = Settlement.All;

            if (type != null)
            {
                switch (type.ToLowerInvariant())
                {
                    case "town": settlements = settlements.Where(s => s.IsTown); break;
                    case "castle": settlements = settlements.Where(s => s.IsCastle); break;
                    case "village": settlements = settlements.Where(s => s.IsVillage); break;
                    case "hideout": settlements = settlements.Where(s => s.IsHideout); break;
                }
            }

            if (!string.IsNullOrEmpty(faction))
                settlements = settlements.Where(s => s.MapFaction?.Name?.ToString().Equals(faction, StringComparison.OrdinalIgnoreCase) == true);

            if (nearPlayer == true && MobileParty.MainParty != null)
            {
                var playerPos = MobileParty.MainParty.GetPosition2D;
                settlements = settlements.OrderBy(s =>
                {
                    var dx = s.GetPosition2D.X - playerPos.X;
                    var dy = s.GetPosition2D.Y - playerPos.Y;
                    return dx * dx + dy * dy;
                });
            }

            var list = settlements.Take(max).Select(s => SerializeSettlement(s)).ToList();

            return new
            {
                /// Number of settlements returned
                count = list.Count,
                /// Array of settlement summary objects
                settlements = list,
            };
        });
    }

    [Tool("settlement/get_workshops", Description = "Get workshops in a town settlement.")]
    public partial Task<object> GetWorkshops(
        [ToolParameter(Description = "Town name or string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == nameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {nameOrId}" };

            if (!settlement.IsTown || settlement.Town == null)
                return new { error = $"{settlement.Name} is not a town" };

            var workshops = settlement.Town.Workshops?
                .Where(w => w != null)
                .Select(w => new
                {
                    type = w.WorkshopType?.Name?.ToString(),
                    owner = w.Owner?.Name?.ToString(),
                    capital = w.Capital,
                })
                .ToList();

            return new
            {
                /// Settlement name
                settlement = settlement.Name?.ToString(),
                /// Number of workshops
                count = workshops?.Count ?? 0,
                /// Array of workshop objects with type, owner, capital
                workshops,
            };
        });
    }

    [Tool("settlement/get_market_prices", Description = "Get trade goods and their prices at a settlement.")]
    public partial Task<object> GetMarketPrices(
        [ToolParameter(Description = "Town/village name or string ID")] string nameOrId,
        [ToolParameter(Description = "Max items to return (default 30)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 30;

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == nameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {nameOrId}" };

            if (settlement.Town == null)
                return new { error = $"{settlement.Name} has no market" };

            var items = settlement.ItemRoster?
                .Where(e => e is { IsEmpty: false, EquipmentElement.Item: not null })
                .Take(max)
                .Select(e => new
                {
                    name = e.EquipmentElement.Item?.Name?.ToString(),
                    quantity = e.Amount,
                    price = settlement.Town.MarketData?.GetPrice(e.EquipmentElement) ?? 0,
                })
                .ToList();

            return new
            {
                /// Settlement name
                settlement = settlement.Name?.ToString(),
                /// Number of items returned
                count = items?.Count ?? 0,
                /// Array of items with name, quantity, price
                items,
            };
        });
    }
}