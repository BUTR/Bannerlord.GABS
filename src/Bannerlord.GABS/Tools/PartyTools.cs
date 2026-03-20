// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Bannerlord.GABS.Patches;

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace Bannerlord.GABS.Tools;

public partial class PartyTools
{
    [ToolResponse("id", Type = "string", Description = "Party string ID")]
    [ToolResponse("name", Type = "string", Description = "Party name", Nullable = true)]
    [ToolResponse("leader", Type = "string", Description = "Leader hero name", Nullable = true)]
    [ToolResponse("faction", Type = "string", Description = "Faction name", Nullable = true)]
    [ToolResponse("clan", Type = "string", Description = "Clan name", Nullable = true)]
    [ToolResponse("troopCount", Type = "integer", Description = "Total troop count")]
    [ToolResponse("woundedCount", Type = "integer", Description = "Wounded troop count")]
    [ToolResponse("prisonerCount", Type = "integer", Description = "Prisoner count")]
    [ToolResponse("morale", Type = "number", Description = "Party morale")]
    [ToolResponse("food", Type = "number", Description = "Food amount")]
    [ToolResponse("foodChange", Type = "number", Description = "Food change rate")]
    [ToolResponse("positionX", Type = "number", Description = "X map coordinate")]
    [ToolResponse("positionY", Type = "number", Description = "Y map coordinate")]
    [ToolResponse("isMoving", Type = "boolean", Description = "Whether party is moving")]
    [ToolResponse("currentSettlement", Type = "string", Description = "Current settlement name", Nullable = true)]
    [ToolResponse("isMainParty", Type = "boolean", Description = "Whether this is the player's party (detailed)", Nullable = true)]
    [ToolResponse("isLordParty", Type = "boolean", Description = "Whether this is a lord's party (detailed)", Nullable = true)]
    [ToolResponse("isCaravan", Type = "boolean", Description = "Whether this is a caravan (detailed)", Nullable = true)]
    [ToolResponse("isBandit", Type = "boolean", Description = "Whether this is a bandit party (detailed)", Nullable = true)]
    [ToolResponse("isGarrison", Type = "boolean", Description = "Whether this is a garrison (detailed)", Nullable = true)]
    [ToolResponse("isMilitia", Type = "boolean", Description = "Whether this is a militia (detailed)", Nullable = true)]
    [ToolResponse("isVillager", Type = "boolean", Description = "Whether this is a villager party (detailed)", Nullable = true)]
    [ToolResponse("speed", Type = "number", Description = "Party speed (detailed)", Nullable = true)]
    [ToolResponse("seeingRange", Type = "number", Description = "Party seeing range (detailed)", Nullable = true)]
    [ToolResponse("inventoryCapacity", Type = "integer", Description = "Inventory capacity (detailed)", Nullable = true)]
    [ToolResponse("army", Type = "string", Description = "Army name (detailed)", Nullable = true)]
    [ToolResponse("defaultBehavior", Type = "string", Description = "Default AI behavior (detailed)", Nullable = true)]
    [ToolResponse("shortTermBehavior", Type = "string", Description = "Short-term AI behavior (detailed)", Nullable = true)]
    [ToolResponse("targetSettlement", Type = "string", Description = "Target settlement (detailed)", Nullable = true)]
    [ToolResponse("targetParty", Type = "string", Description = "Target party (detailed)", Nullable = true)]
    private static object SerializeParty(MobileParty party, bool detailed = false)
    {
        var basic = new Dictionary<string, object?>
        {
            ["id"] = party.StringId,
            ["name"] = party.Name?.ToString(),
            ["leader"] = party.LeaderHero?.Name?.ToString(),
            ["faction"] = party.MapFaction?.Name?.ToString(),
            ["clan"] = party.ActualClan?.Name?.ToString(),
            ["troopCount"] = party.MemberRoster?.TotalManCount ?? 0,
            ["woundedCount"] = party.MemberRoster?.TotalWounded ?? 0,
            ["prisonerCount"] = party.PrisonRoster?.TotalManCount ?? 0,
            ["morale"] = Math.Round(party.Morale, 1),
            ["food"] = Math.Round(party.Food, 1),
            ["foodChange"] = Math.Round(party.FoodChange, 2),
            ["positionX"] = Math.Round(party.GetPosition2D.X, 2),
            ["positionY"] = Math.Round(party.GetPosition2D.Y, 2),
            ["isMoving"] = party.IsMoving,
            ["currentSettlement"] = party.CurrentSettlement?.Name?.ToString(),
        };

        if (detailed)
        {
            basic["isMainParty"] = party.IsMainParty;
            basic["isLordParty"] = party.IsLordParty;
            basic["isCaravan"] = party.IsCaravan;
            basic["isBandit"] = party.IsBandit;
            basic["isGarrison"] = party.IsGarrison;
            basic["isMilitia"] = party.IsMilitia;
            basic["isVillager"] = party.IsVillager;
            basic["speed"] = Math.Round(party.Speed, 2);
            basic["seeingRange"] = Math.Round(party.SeeingRange, 1);
            basic["inventoryCapacity"] = party.InventoryCapacity;
            basic["army"] = party.Army?.Name?.ToString();
            basic["defaultBehavior"] = party.DefaultBehavior.ToString();
            basic["shortTermBehavior"] = party.ShortTermBehavior.ToString();
            basic["targetSettlement"] = party.ShortTermTargetSettlement?.Name?.ToString();
            basic["targetParty"] = party.ShortTermTargetParty?.Name?.ToString();
        }

        return basic;
    }

    [Tool("party/get_player_party", Description = "Get detailed info about the player's party (troops, morale, food, speed, position).")]
    public partial Task<object> GetPlayerParty()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            return SerializeParty(party, detailed: true);
        });
    }

    [Tool("party/get_party", Description = "Get info about a specific party by leader name or party string ID.")]
    public partial Task<object> GetParty(
        [ToolParameter(Description = "Party leader name or party string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.All.FirstOrDefault(p => p.StringId == nameOrId)
                        ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true)
                        ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (party == null)
                return new { error = $"Party not found: {nameOrId}" };

            return SerializeParty(party, detailed: true);
        });
    }

    [Tool("party/list_parties", Description = "List mobile parties with optional filters. Use filter='visible' to show only parties the player can see and interact with on the campaign map.")]
    public partial Task<object> ListParties(
        [ToolParameter(Description = "Filter: 'lord', 'caravan', 'bandit', 'villager', 'garrison', 'visible'. Use 'visible' to see only parties you can interact with.", Required = false)] string? filter,
        [ToolParameter(Description = "Filter by faction name", Required = false)] string? faction,
        [ToolParameter(Description = "Max results (default 50)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 50;
            IEnumerable<MobileParty> parties = MobileParty.All.Where(p => p.IsActive);

            if (filter != null)
            {
                parties = filter.ToLowerInvariant() switch
                {
                    "lord" => parties.Where(p => p.IsLordParty),
                    "caravan" => parties.Where(p => p.IsCaravan),
                    "bandit" => parties.Where(p => p.IsBandit),
                    "villager" => parties.Where(p => p.IsVillager),
                    "garrison" => parties.Where(p => p.IsGarrison),
                    "visible" => parties.Where(p => p.IsVisible),
                    _ => parties
                };
            }

            if (!string.IsNullOrEmpty(faction))
                parties = parties.Where(p => p.MapFaction?.Name?.ToString().Equals(faction, StringComparison.OrdinalIgnoreCase) == true);

            var list = parties.Take(max).Select(p => SerializeParty(p)).ToList();

            return new
            {
                /// Number of parties returned
                count = list.Count,
                /// Array of party summary objects
                parties = list,
            };
        });
    }

    [Tool("party/get_troop_roster", Description = "Get the detailed troop roster for a party.")]
    public partial Task<object> GetTroopRoster(
        [ToolParameter(Description = "Party leader name or string ID (omit for player party)", Required = false)] string? nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            MobileParty? party;
            if (string.IsNullOrEmpty(nameOrId))
            {
                party = MobileParty.MainParty;
            }
            else
            {
                party = MobileParty.All.FirstOrDefault(p => p.StringId == nameOrId)
                        ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (party == null)
                return new { error = $"Party not found: {nameOrId}" };

            var troops = party.MemberRoster.GetTroopRoster().Select(element => new
            {
                /// Troop character name
                name = element.Character?.Name?.ToString(),
                /// Troop tier level
                tier = element.Character?.Tier ?? 0,
                /// Number of this troop type
                count = element.Number,
                /// Number of wounded
                wounded = element.WoundedNumber,
                /// Whether this entry is a hero/companion
                isHero = element.Character?.IsHero ?? false,
            }).ToList();

            var prisoners = party.PrisonRoster.GetTroopRoster().Select(element => new
            {
                name = element.Character?.Name?.ToString(),
                tier = element.Character?.Tier ?? 0,
                count = element.Number,
                wounded = element.WoundedNumber,
                isHero = element.Character?.IsHero ?? false,
            }).ToList();

            return new
            {
                /// Party display name
                party = party.Name?.ToString(),
                /// Total troop count
                totalTroops = party.MemberRoster.TotalManCount,
                /// Total wounded count
                totalWounded = party.MemberRoster.TotalWounded,
                /// Total prisoner count
                totalPrisoners = party.PrisonRoster.TotalManCount,
                /// Array of troop entries with name, tier, count, wounded, isHero
                troops,
                /// Array of prisoner entries with name, tier, count, wounded, isHero
                prisoners,
            };
        });
    }

    [Tool("party/move_to_settlement", Description = "Order the player's party to move toward a settlement. Returns immediately — use party/wait_for_arrival to block until the party arrives or is interrupted.")]
    public partial Task<object> MoveToSettlement(
        [ToolParameter(Description = "Settlement name or string ID to move to")] string settlementNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementNameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(settlementNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {settlementNameOrId}" };

#if v1313 || v1315
                SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, false, false);
#else
            SetPartyAiAction.GetActionForVisitingSettlement(party, settlement);
#endif

            return new
            {
                /// Status message
                message = $"Moving to {settlement.Name}",
                /// Target settlement name
                target = settlement.Name?.ToString(),
            };
        });
    }

    [Tool("party/move_to_point", Description = "Order the player's party to move toward a specific map coordinate (x, y). Returns immediately — use party/wait_for_arrival to block until the party arrives or is interrupted.")]
    public partial Task<object> MoveToPoint(
        [ToolParameter(Description = "X coordinate on the campaign map")] float x,
        [ToolParameter(Description = "Y coordinate on the campaign map")] float y)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

#if v1313 || v1315
                var target = new CampaignVec2(new Vec2(x, y), true);
                party.SetMoveGoToPoint(target, MobileParty.NavigationType.Default);
#else
            party.Ai.SetMoveGoToPoint(new Vec2(x, y));
#endif

            return new
            {
                /// Status message
                message = $"Moving to ({x:F1}, {y:F1})",
                /// Target X coordinate
                targetX = Math.Round(x, 1),
                /// Target Y coordinate
                targetY = Math.Round(y, 1),
            };
        });
    }

    [Tool("party/follow_party", Description = "Order the player's party to escort/follow another party. Returns immediately — the party will follow continuously. Use party/get_player_party to check movement status.")]
    public partial Task<object> FollowParty(
        [ToolParameter(Description = "Target party leader name or string ID")] string targetNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var target = MobileParty.All.FirstOrDefault(p => p.StringId == targetNameOrId)
                         ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase) == true)
                         ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
                return new { error = $"Party not found: {targetNameOrId}" };

#if v1313 || v1315
                SetPartyAiAction.GetActionForEscortingParty(party, target, MobileParty.NavigationType.Default, false, false);
#else
            SetPartyAiAction.GetActionForEscortingParty(party, target);
#endif

            // Unpause the game so the party actually starts moving
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;

            return new
            {
                /// Status message
                message = $"Following {target.Name}",
                /// Target party name
                target = target.Name?.ToString(),
            };
        });
    }

    // Stores the string ID of the party being tracked by engage/follow
    private static volatile string? _trackingTargetId;

    [Tool("party/engage_party", Description = "Order the player's party to pursue and engage another party. The party will continuously track the target until it catches up and triggers an encounter. Use wait_for_arrival after this to wait for the encounter.")]
    public partial Task<object> EngageParty(
        [ToolParameter(Description = "Target party leader name or string ID")] string targetNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var target = MobileParty.All.FirstOrDefault(p => p.StringId == targetNameOrId)
                         ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase) == true)
                         ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(targetNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
                return new { error = $"Party not found: {targetNameOrId}" };

            // Check distance to target
            var dist = party.GetPosition2D.Distance(target.GetPosition2D);

            if (dist < 5f)
            {
                // Close enough — trigger the encounter directly
                _trackingTargetId = null;
                EncounterManager.StartPartyEncounter(party.Party, target.Party);
                return new
                {
                    /// Status message
                    message = $"Encountered {target.Name}",
                    /// Target party name
                    target = target.Name?.ToString(),
                };
            }

            // Far away — move toward the target and start tracking
#if v1313 || v1315
                var targetPos = new CampaignVec2(target.GetPosition2D, true);
                party.SetMoveGoToPoint(targetPos, MobileParty.NavigationType.Default);
#else
            party.Ai.SetMoveGoToPoint(target.GetPosition2D);
#endif

            // Store target ID for continuous tracking in wait_for_arrival
            _trackingTargetId = target.StringId;

            // Unpause the game so the party actually starts moving
            if (Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop)
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;

            return new
            {
                message = $"Pursuing {target.Name} (distance: {dist:F0})",
                target = target.Name?.ToString(),
            };
        });
    }

    [Tool("party/detect_threats", Description = "Detect nearby hostile parties that could attack the player. Returns threats sorted by distance with speed comparison to determine if fleeing is possible.")]
    public partial Task<object> DetectThreats(
        [ToolParameter(Description = "Max detection range in map units (default 20)", Required = false)] float range = 20f)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var player = MobileParty.MainParty;
            if (player == null)
                return new { error = "No main party" };

            var playerPos = player.GetPosition2D;
            var playerSpeed = player.Speed;
            var playerFaction = player.MapFaction;

            var threats = MobileParty.All
                .Where(p => p.IsActive && p != player && !p.IsGarrison && !p.IsMilitia
                            && p.MapFaction != null && playerFaction != null
                            && p.MapFaction.IsAtWarWith(playerFaction)
                            && p.GetPosition2D.Distance(playerPos) < range)
                .OrderBy(p => p.GetPosition2D.Distance(playerPos))
                .Take(10)
                .Select(p => new
                {
                    name = p.Name?.ToString(),
                    id = p.StringId,
                    troopCount = p.MemberRoster.TotalManCount,
                    distance = Math.Round(p.GetPosition2D.Distance(playerPos), 1),
                    speed = Math.Round(p.Speed, 1),
                    canOutrun = playerSpeed > p.Speed,
                    positionX = Math.Round(p.GetPosition2D.X, 1),
                    positionY = Math.Round(p.GetPosition2D.Y, 1),
                })
                .ToList();

            return new
            {
                /// Number of hostile parties detected in range
                threatCount = threats.Count,
                /// Player party's current speed
                playerSpeed = Math.Round(playerSpeed, 1),
                /// Array of nearby hostile party objects
                threats,
            };
        });
    }

    [Tool("party/flee_to_safety", Description = "Move the player's party to the nearest friendly settlement AWAY from ALL nearby threats. Combines direction vectors from all hostile parties in range to find the safest escape route.")]
    public partial Task<object> FleeToSafety(
        [ToolParameter(Description = "Detection range for threats (default 20)", Required = false)] float range = 20f)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var player = MobileParty.MainParty;
            if (player == null)
                return new { error = "No main party" };

            var playerPos = player.GetPosition2D;
            var playerFaction = player.MapFaction;

            // Find ALL nearby hostile parties
            var threats = MobileParty.All
                .Where(p => p.IsActive && p != player && !p.IsGarrison && !p.IsMilitia
                            && p.MapFaction != null && playerFaction != null
                            && p.MapFaction.IsAtWarWith(playerFaction)
                            && p.GetPosition2D.Distance(playerPos) < range)
                .ToList();

            if (threats.Count == 0)
                return new { error = "No nearby threats detected" };

            // Combined flee direction: sum of (player - threat) vectors, weighted by inverse distance
            // Closer threats have more influence on the flee direction
            var fleeX = 0f;
            var fleeY = 0f;
            foreach (var t in threats)
            {
                var diff = playerPos - t.GetPosition2D;
                var dist = t.GetPosition2D.Distance(playerPos);
                var weight = 1f / (dist + 0.1f); // Closer threats weigh more
                fleeX += diff.X * weight;
                fleeY += diff.Y * weight;
            }
            var fleeDirection = new Vec2(fleeX, fleeY);

            var threatNames = string.Join(", ", threats.Take(3).Select(t => t.Name?.ToString()));
            if (threats.Count > 3) threatNames += $" (+{threats.Count - 3} more)";

            // Score settlements: prefer ones in the combined flee direction
            var safeSettlement = Settlement.All
                .Where(s => (s.IsTown || s.IsCastle)
                            && !s.IsUnderSiege
                            && s.MapFaction != null
                            && playerFaction != null
                            && !s.MapFaction.IsAtWarWith(playerFaction))
                .Select(s =>
                {
                    var settlementPos = s.GetPosition2D;
                    var toSettlement = settlementPos - playerPos;
                    var dist = settlementPos.Distance(playerPos);
                    // Dot product: positive = settlement is in flee direction
                    var fleeScore = fleeDirection.X * toSettlement.X + fleeDirection.Y * toSettlement.Y;
                    // Normalize — closer safe settlements are better, but only in the right direction
                    var score = fleeScore / (dist + 1f) - dist * 0.1f;
                    return new { settlement = s, dist, fleeScore, score };
                })
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (safeSettlement == null)
                return new { error = "No friendly settlement found to flee to" };

            var target = safeSettlement.settlement;
#if v1313 || v1315
                SetPartyAiAction.GetActionForVisitingSettlement(player, target, MobileParty.NavigationType.Default, false, false);
#else
            SetPartyAiAction.GetActionForVisitingSettlement(player, target);
#endif

            return new
            {
                /// Status message
                message = $"Fleeing from {threats.Count} threat(s) to {target.Name}",
                /// Settlement name
                settlement = target.Name?.ToString(),
                /// Settlement type (town/castle)
                type = target.IsTown ? "town" : "castle",
                /// Distance to the settlement
                distance = Math.Round(safeSettlement.dist, 1),
                /// Whether the settlement is in the safe direction (away from combined threat vector)
                isSafeDirection = safeSettlement.fleeScore > 0,
                /// Number of threats considered
                threatCount = threats.Count,
                /// Names of threats being fled from
                fleeingFrom = threatNames,
            };
        });
    }

    [Tool("party/enter_settlement", Description = "Enter a settlement. Returns immediately — use core/wait_for_state (expectedState='game_menu') to block until the settlement menu appears.")]
    public partial Task<object> EnterSettlement(
        [ToolParameter(Description = "Settlement name or string ID (omit if currently targeting one)", Required = false)] string? settlementNameOrId,
        [ToolParameter(Description = "Party leader name or string ID (omit for player party)", Required = false)] string? partyNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            MobileParty? party;
            bool isPlayerParty;
            if (string.IsNullOrEmpty(partyNameOrId))
            {
                party = MobileParty.MainParty;
                isPlayerParty = true;
            }
            else
            {
                party = MobileParty.All.FirstOrDefault(p => p.StringId == partyNameOrId)
                        ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(partyNameOrId, StringComparison.OrdinalIgnoreCase) == true)
                        ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(partyNameOrId, StringComparison.OrdinalIgnoreCase) == true);
                isPlayerParty = party?.IsMainParty == true;
            }

            if (party == null)
                return new { error = $"Party not found: {partyNameOrId}" };

            if (party.CurrentSettlement != null)
                return new { error = $"Already in settlement: {party.CurrentSettlement.Name}" };

            Settlement? settlement;
            if (string.IsNullOrEmpty(settlementNameOrId))
            {
                settlement = party.TargetSettlement ?? party.ShortTermTargetSettlement;
                if (settlement == null)
                    return new { error = "No target settlement. Specify a settlement name." };
            }
            else
            {
                settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementNameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(settlementNameOrId, StringComparison.OrdinalIgnoreCase) == true);

                if (settlement == null)
                    return new { error = $"Settlement not found: {settlementNameOrId}" };
            }

            if (isPlayerParty)
            {
                // Player party must use EncounterManager to set up PlayerEncounter properly
                EncounterManager.StartSettlementEncounter(party, settlement);
            }
            else
            {
                // NPC parties can use the action directly
                EnterSettlementAction.ApplyForParty(party, settlement);
            }

            return new
            {
                /// Status message
                message = $"{party.Name} entering {settlement.Name}",
                /// Party name
                party = party.Name?.ToString(),
                /// Settlement name
                settlement = settlement.Name?.ToString(),
            };
        });
    }

    [Tool("party/get_available_recruits", Description = "List troops available for recruitment at a settlement from its notables.")]
    public partial Task<object> GetAvailableRecruits(
        [ToolParameter(Description = "Settlement name or string ID")] string settlementNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementNameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(settlementNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {settlementNameOrId}" };

            var buyer = Hero.MainHero;
            var volunteerModel = Campaign.Current.Models.VolunteerModel;
            var wageModel = Campaign.Current.Models.PartyWageModel;

            var notableRecruits = settlement.Notables
                .Where(notable => notable.CanHaveRecruits && notable.IsAlive)
                .Select(notable =>
                {
                    int maxIndex = volunteerModel.MaximumIndexHeroCanRecruitFromHero(buyer, notable);
                    var recruits = Enumerable.Range(0, maxIndex)
                        .Where(i => notable.VolunteerTypes[i] != null)
                        .Select(i => new
                        {
                            /// Volunteer slot index
                            slotIndex = i,
                            /// Troop name
                            name = notable.VolunteerTypes[i]!.Name?.ToString(),
                            /// Troop tier level
                            tier = notable.VolunteerTypes[i]!.Tier,
                            /// Recruitment cost in gold
#if v1313 || v1315
                                cost = wageModel.GetTroopRecruitmentCost(notable.VolunteerTypes[i]!, buyer).ResultNumber,
#else
                            cost = wageModel.GetTroopRecruitmentCost(notable.VolunteerTypes[i]!, buyer, false),
#endif
                        })
                        .ToList();

                    return new
                    {
                        /// Notable hero name
                        notable = notable.Name?.ToString(),
                        /// Player relation with this notable
                        relation = notable.GetRelationWithPlayer(),
                        /// Available recruits with slotIndex, name, tier, cost
                        recruits,
                    };
                })
                .Where(x => x.recruits.Count > 0)
                .ToList();

            return new
            {
                /// Settlement name
                settlement = settlement.Name?.ToString(),
                /// Player's current gold
                playerGold = Hero.MainHero?.Gold ?? 0,
                /// Number of notables with recruits
                notableCount = notableRecruits.Count,
                /// Array of notable objects with name, relation, and recruits array
                notables = notableRecruits,
            };
        });
    }

    [Tool("party/recruit_troop", Description = "Recruit a troop from a settlement notable. Must be at or near the settlement.")]
    public partial Task<object> RecruitTroop(
        [ToolParameter(Description = "Settlement name or string ID")] string settlementNameOrId,
        [ToolParameter(Description = "Notable hero name providing the recruit")] string notableName,
        [ToolParameter(Description = "Slot index of the troop to recruit (from get_available_recruits)")] int slotIndex)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementNameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(settlementNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {settlementNameOrId}" };

            var notable = settlement.Notables.FirstOrDefault(n =>
                n.Name?.ToString().Equals(notableName, StringComparison.OrdinalIgnoreCase) == true);

            if (notable == null)
                return new { error = $"Notable not found: {notableName}" };

            if (!notable.CanHaveRecruits)
                return new { error = $"{notableName} cannot provide recruits" };

            if (slotIndex < 0 || slotIndex >= notable.VolunteerTypes.Length)
                return new { error = $"Invalid slot index {slotIndex}. Valid: 0-{notable.VolunteerTypes.Length - 1}" };

            var troop = notable.VolunteerTypes[slotIndex];
            if (troop == null)
                return new { error = $"No troop available at slot {slotIndex}" };

            var buyer = Hero.MainHero;
            var volunteerModel = Campaign.Current.Models.VolunteerModel;
            int maxIndex = volunteerModel.MaximumIndexHeroCanRecruitFromHero(buyer, notable);

            if (slotIndex >= maxIndex)
                return new { error = $"Insufficient relation to recruit from slot {slotIndex} (need higher relation with {notableName})" };

#if v1313 || v1315
                var cost = (int) Campaign.Current.Models.PartyWageModel
                    .GetTroopRecruitmentCost(troop, buyer).ResultNumber;
#else
            var cost = Campaign.Current.Models.PartyWageModel
                .GetTroopRecruitmentCost(troop, buyer, false);
#endif

            if (buyer.Gold < cost)
                return new { error = $"Not enough gold. Need {cost}, have {buyer.Gold}" };

            // Deduct gold
            GiveGoldAction.ApplyBetweenCharacters(buyer, null, cost, true);

            // Clear the volunteer slot
            notable.VolunteerTypes[slotIndex] = null;

            // Add troop to party
            party.MemberRoster.AddToCounts(troop, 1);

            // Fire the recruitment event
            CampaignEventDispatcher.Instance.OnTroopRecruited(buyer, settlement, notable, troop, 1);

            return new
            {
                /// Recruited troop name
                recruited = troop.Name?.ToString(),
                /// Troop tier
                tier = troop.Tier,
                /// Gold spent
                cost,
                /// Player gold after recruitment
                remainingGold = buyer.Gold,
                /// Party size after recruitment
                partySize = party.MemberRoster.TotalManCount,
            };
        });
    }

    [Tool("party/recruit_all", Description = "Recruit all available troops from all notables at a settlement.")]
    public partial Task<object> RecruitAll(
        [ToolParameter(Description = "Settlement name or string ID")] string settlementNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = MobileParty.MainParty;
            if (party == null)
                return new { error = "No main party" };

            var settlement = Settlement.All.FirstOrDefault(s => s.StringId == settlementNameOrId)
                             ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(settlementNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (settlement == null)
                return new { error = $"Settlement not found: {settlementNameOrId}" };

            var buyer = Hero.MainHero;
            var volunteerModel = Campaign.Current.Models.VolunteerModel;
            var wageModel = Campaign.Current.Models.PartyWageModel;

            int totalRecruited = 0;
            int totalCost = 0;
            var recruited = new List<string>();

            foreach (var notable in settlement.Notables)
            {
                if (!notable.CanHaveRecruits || !notable.IsAlive) continue;

                int maxIndex = volunteerModel.MaximumIndexHeroCanRecruitFromHero(buyer, notable);

                for (int i = 0; i < maxIndex; i++)
                {
                    var troop = notable.VolunteerTypes[i];
                    if (troop == null) continue;

#if v1313 || v1315
                        var cost = (int) wageModel.GetTroopRecruitmentCost(troop, buyer).ResultNumber;
#else
                    var cost = wageModel.GetTroopRecruitmentCost(troop, buyer, false);
#endif
                    if (buyer.Gold < cost) continue;

                    GiveGoldAction.ApplyBetweenCharacters(buyer, null, cost, true);
                    notable.VolunteerTypes[i] = null;
                    party.MemberRoster.AddToCounts(troop, 1);
                    CampaignEventDispatcher.Instance.OnTroopRecruited(buyer, settlement, notable, troop, 1);

                    totalRecruited++;
                    totalCost += cost;
                    recruited.Add(troop.Name?.ToString() ?? "unknown");
                }
            }

            return new
            {
                /// Whether any troops were recruited
                success = totalRecruited > 0,
                /// Settlement name
                settlement = settlement.Name?.ToString(),
                /// Total troops recruited
                totalRecruited,
                /// Total gold spent
                totalCost,
                /// Player gold after recruitment
                remainingGold = buyer.Gold,
                /// Party size after recruitment
                partySize = party.MemberRoster.TotalManCount,
                /// Array of recruited troop name strings
                recruited,
            };
        });
    }

    [Tool("party/wait_for_arrival", Description = "Wait until the player's party stops moving (arrives at destination or is interrupted). Use after move_to_settlement or move_to_point. Automatically unpauses the game if paused. Returns early if interrupted by an inquiry, encounter, menu, or screen change. Pass timeout via games.call_tool timeout parameter.")]
    public partial Task<object> WaitForArrival(
        [ToolParameter(Description = "Game speed to set while waiting (1-4, default 4). Set to 0 to not change speed.", Required = false)] int speed = 4,
        [ToolParameter(Description = "Poll interval in milliseconds (default 1000).", Required = false)] int pollIntervalMs = 1000)
    {
        if (pollIntervalMs < 200) pollIntervalMs = 200;
        if (pollIntervalMs > 5000) pollIntervalMs = 5000;
        if (speed < 0) speed = 0;
        if (speed > 4) speed = 4;

        return Task.Run<object>(async () =>
        {
            var startTime = DateTime.UtcNow;
            var maxWait = TimeSpan.FromSeconds(120);
            var hasBeenMoving = false;

            while (DateTime.UtcNow - startTime < maxWait)
            {
                var check = await MainThreadDispatcher.EnqueueAsync(() =>
                {
                    if (Campaign.Current == null)
                        return new MovementCheck { Done = true, Reason = "arrived", Error = "No active campaign" };

                    var party = MobileParty.MainParty;
                    if (party == null)
                        return new MovementCheck { Done = true, Reason = "arrived", Error = "No main party" };

                    var result = new MovementCheck
                    {
                        IsMoving = party.IsMoving,
                        Settlement = party.CurrentSettlement?.Name?.ToString(),
                        PosX = Math.Round(party.GetPosition2D.X, 2),
                        PosY = Math.Round(party.GetPosition2D.Y, 2),
                        ScreenType = ScreenManager.TopScreen?.GetType().Name,
                    };

                    // Check for interruptions

                    // 1. Inquiry popup (random event, decision, etc.)
                    if (InquiryState.CurrentInquiry != null)
                    {
                        result.Done = true;
                        result.Reason = "inquiry";
                        result.InterruptDetail = $"Inquiry: {InquiryState.CurrentInquiry.TitleText}";
                        return result;
                    }
                    if (InquiryState.CurrentMultiSelection != null)
                    {
                        result.Done = true;
                        result.Reason = "inquiry";
                        result.InterruptDetail = $"Multi-selection: {InquiryState.CurrentMultiSelection.TitleText}";
                        return result;
                    }
                    if (InquiryState.CurrentIncident != null)
                    {
                        result.Done = true;
                        result.Reason = "incident";
#if v1313 || v1315
                            result.InterruptDetail = $"Random event: {InquiryState.CurrentIncident.Title?.ToString()}";
#else
                        result.InterruptDetail = "Random event";
#endif
                        return result;
                    }
                    if (InquiryState.CurrentSceneNotification != null)
                    {
                        result.Done = true;
                        result.Reason = "scene_notification";
                        result.InterruptDetail = $"Scene notification: {InquiryState.CurrentSceneNotification.TitleText?.ToString()}";
                        return result;
                    }

                    // 2. Active conversation (hostile party initiated dialogue)
                    var cm = Campaign.Current.ConversationManager;
                    if (cm is { IsConversationInProgress: true })
                    {
                        result.Done = true;
                        result.Reason = "conversation";
                        result.InterruptDetail = $"Conversation with {cm.OneToOneConversationHero?.Name?.ToString() ?? "unknown"}";
                        return result;
                    }

                    // 3. Screen changed away from MapScreen (battle, conversation, etc.)
                    if (result.ScreenType != null && result.ScreenType != "MapScreen")
                    {
                        result.Done = true;
                        result.Reason = "screen_change";
                        result.InterruptDetail = $"Screen changed to {result.ScreenType}";
                        return result;
                    }

                    // 4. Game menu appeared (encounter, settlement entry, etc.)
                    if (Campaign.Current.CurrentMenuContext?.GameMenu != null)
                    {
                        var menuId = Campaign.Current.CurrentMenuContext.GameMenu.StringId;
                        result.Done = true;
                        result.Reason = "menu";
                        result.InterruptDetail = $"Game menu: {menuId}";
                        return result;
                    }

                    // 5. Party stopped moving (arrived)
                    // Only return "arrived" once the party has been seen moving at least once,
                    // so that engage/follow commands have time to be processed by the game loop.
                    if (party.IsMoving)
                        hasBeenMoving = true;

                    if (!party.IsMoving && hasBeenMoving)
                    {
                        result.Done = true;
                        result.Reason = "arrived";
                        return result;
                    }

                    // 6. If tracking a party (engage/follow), update target position or trigger encounter
                    if (_trackingTargetId != null)
                    {
                        var trackTarget = MobileParty.All.FirstOrDefault(p => p.StringId == _trackingTargetId);
                        if (trackTarget is { IsActive: true })
                        {
                            var dist = party.GetPosition2D.Distance(trackTarget.GetPosition2D);
                            if (dist < 5f)
                            {
                                // Close enough — trigger encounter
                                _trackingTargetId = null;
                                EncounterManager.StartPartyEncounter(party.Party, trackTarget.Party);
                                // The encounter will show a menu — let the next poll catch it
                            }
                            else
                            {
#if v1313 || v1315
                                    var targetPos = new CampaignVec2(trackTarget.GetPosition2D, true);
                                    party.SetMoveGoToPoint(targetPos, MobileParty.NavigationType.Default);
#else
                                party.Ai.SetMoveGoToPoint(trackTarget.GetPosition2D);
#endif
                            }
                        }
                        else
                        {
                            // Target lost — stop tracking
                            _trackingTargetId = null;
                        }
                    }

                    // Still moving — unpause if needed
                    if (speed > 0)
                    {
                        var mode = Campaign.Current.TimeControlMode;
                        if (mode is CampaignTimeControlMode.Stop or CampaignTimeControlMode.StoppablePlay)
                        {
                            Campaign.Current.TimeControlMode = speed switch
                            {
                                1 => CampaignTimeControlMode.StoppablePlay,
                                2 => CampaignTimeControlMode.StoppableFastForward,
                                3 => CampaignTimeControlMode.UnstoppableFastForward,
                                4 => CampaignTimeControlMode.UnstoppableFastForwardForPartyWaitTime,
                                _ => CampaignTimeControlMode.StoppableFastForward,
                            };
                        }
                    }

                    return result;
                });

                if (check.Error != null)
                    return new { error = check.Error };

                if (check.Done)
                {
                    _trackingTargetId = null;
                    return new
                    {
                        /// Why the wait ended: 'arrived', 'inquiry', 'incident', 'screen_change', 'menu', 'timeout'
                        reason = check.Reason,
                        /// Whether movement was interrupted (true if reason is not 'arrived')
                        interrupted = check.Reason != "arrived",
                        /// Details about the interruption
                        interruptDetail = check.InterruptDetail,
                        /// Current settlement name if in one
                        settlement = check.Settlement,
                        /// Final map X coordinate
                        positionX = check.PosX,
                        /// Final map Y coordinate
                        positionY = check.PosY,
                        /// Current screen type name
                        screenType = check.ScreenType,
                        /// Milliseconds spent waiting
                        waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
                    };
                }

                await Task.Delay(pollIntervalMs);
            }

            // Timeout
            var final = await MainThreadDispatcher.EnqueueAsync(() =>
            {
                var party = MobileParty.MainParty;
                return new MovementCheck
                {
                    PosX = Math.Round(party?.GetPosition2D.X ?? 0, 2),
                    PosY = Math.Round(party?.GetPosition2D.Y ?? 0, 2),
                    Settlement = party?.CurrentSettlement?.Name?.ToString(),
                    ScreenType = ScreenManager.TopScreen?.GetType().Name,
                };
            });

            return new
            {
                /// Why the wait ended: 'arrived', 'inquiry', 'incident', 'screen_change', 'menu', 'timeout'
                reason = "timeout",
                /// Whether movement was interrupted (true if reason is not 'arrived')
                interrupted = false,
                /// Details about the interruption
                interruptDetail = $"Timed out waiting for arrival. Current position: ({final.PosX}, {final.PosY})",
                /// Current settlement name if in one
                settlement = final.Settlement,
                /// Final map X coordinate
                positionX = final.PosX,
                /// Final map Y coordinate
                positionY = final.PosY,
                /// Current screen type name
                screenType = final.ScreenType,
                /// Milliseconds spent waiting
                waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
            };
        });
    }

    private class MovementCheck
    {
        public bool Done;
        public string? Reason;
        public string? Error;
        public string? InterruptDetail;
        public bool IsMoving;
        public string? Settlement;
        public double PosX;
        public double PosY;
        public string? ScreenType;
    }

    [Tool("party/leave_settlement", Description = "Leave the current settlement. For the player party, uses the game menu Leave option. For NPC parties, uses LeaveSettlementAction directly.")]
    public partial Task<object> LeaveSettlement(
        [ToolParameter(Description = "Party leader name or string ID (omit for player party)", Required = false)] string? partyNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            MobileParty? party;
            bool isPlayerParty;
            if (string.IsNullOrEmpty(partyNameOrId))
            {
                party = MobileParty.MainParty;
                isPlayerParty = true;
            }
            else
            {
                party = MobileParty.All.FirstOrDefault(p => p.StringId == partyNameOrId)
                        ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(partyNameOrId, StringComparison.OrdinalIgnoreCase) == true)
                        ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(partyNameOrId, StringComparison.OrdinalIgnoreCase) == true);
                isPlayerParty = party?.IsMainParty == true;
            }

            if (party == null)
                return new { error = $"Party not found: {partyNameOrId}" };

            if (party.CurrentSettlement == null)
                return new { error = "Not in a settlement" };

            var settlementName = party.CurrentSettlement.Name?.ToString();

            if (isPlayerParty)
            {
                // Player party must leave through the game menu
                var menuContext = Campaign.Current.CurrentMenuContext;
                if (menuContext?.GameMenu == null)
                    return new { error = "No active game menu to leave through" };

                var menuManager = Campaign.Current.GameMenuManager;
                var optionCount = menuManager.GetVirtualMenuOptionAmount(menuContext);

                for (int i = 0; i < optionCount; i++)
                {
                    if (menuManager.GetVirtualMenuOptionIsLeave(menuContext, i))
                    {
                        menuManager.RunConsequencesOfMenuOption(menuContext, i);
                        return new
                        {
                            /// Status message
                            message = $"{party.Name} left {settlementName}",
                        };
                    }
                }

                return new { error = "No Leave option found in current menu" };
            }
            else
            {
                // NPC parties can use the action directly
                LeaveSettlementAction.ApplyForParty(party);
                return new
                {
                    message = $"{party.Name} left {settlementName}",
                };
            }
        });
    }
}