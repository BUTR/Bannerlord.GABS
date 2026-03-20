// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Helpers;

using Lib.GAB.Tools;

using System;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.GABS.Tools;

public partial class PartyTools
{
    private static MobileParty? FindParty(string nameOrId)
    {
        return MobileParty.All.FirstOrDefault(p => p.StringId == nameOrId)
               ?? MobileParty.All.FirstOrDefault(p => p.LeaderHero?.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true)
               ?? MobileParty.All.FirstOrDefault(p => p.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static Settlement? FindSettlement(string nameOrId)
    {
        return Settlement.All.FirstOrDefault(s => s.StringId == nameOrId)
               ?? Settlement.All.FirstOrDefault(s => s.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static bool ArePartiesNearby(MobileParty a, MobileParty b)
    {
        if (a.CurrentSettlement != null && a.CurrentSettlement == b.CurrentSettlement)
            return true;

        return a.GetPosition2D.Distance(b.GetPosition2D) <= 5f;
    }

    [Tool("party/create_companion_party", Description = "Split a companion out of the player's party to lead their own independent party. The companion must currently be in the player's party. Use party/transfer_troops afterwards to give them soldiers.")]
    public partial Task<object> CreateCompanionParty(
        [ToolParameter(Description = "Companion hero name or string ID")] string companionNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            if (MobileParty.MainParty == null)
                return new { error = "No main party" };

            var hero = Hero.FindFirst(h => h.StringId == companionNameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(companionNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (hero == null)
                return new { error = $"Hero not found: {companionNameOrId}" };

            if (hero == Hero.MainHero)
                return new { error = "Cannot split the player hero into a separate party" };

            if (hero.PartyBelongedTo == null || hero.PartyBelongedTo != MobileParty.MainParty)
                return new { error = $"{hero.Name} is not currently in the player's party" };

            if (hero.Clan != Clan.PlayerClan)
                return new { error = $"{hero.Name} does not belong to the player's clan" };

#if v1313 || v1315
                var newParty = MobilePartyHelper.CreateNewClanMobileParty(hero, Clan.PlayerClan);
#else
            MobilePartyHelper.CreateNewClanMobileParty(hero, Clan.PlayerClan, out bool _);
            var newParty = hero.PartyBelongedTo;
#endif
            if (newParty == null)
                return new { error = $"Party creation failed for {hero.Name}" };

            newParty.Ai.SetDoNotMakeNewDecisions(false);

            return new
            {
                /// Status message
                message = $"{hero.Name} now leads {newParty.Name}",
                /// New party string ID
                partyId = newParty.StringId,
                /// New party display name
                partyName = newParty.Name?.ToString(),
                /// Leader hero name
                leader = hero.Name?.ToString(),
                /// Leader hero string ID
                leaderId = hero.StringId,
                /// Troop count in the new party
                troopCount = newParty.MemberRoster.TotalManCount,
                /// Map X
                positionX = Math.Round(newParty.GetPosition2D.X, 2),
                /// Map Y
                positionY = Math.Round(newParty.GetPosition2D.Y, 2),
            };
        });
    }

    [Tool("party/disband_party", Description = "Disband a companion's party. The companion and any remaining troops return to the player's party.")]
    public partial Task<object> DisbandParty(
        [ToolParameter(Description = "Party leader name, party name, or party string ID")] string partyNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = FindParty(partyNameOrId);
            if (party == null)
                return new { error = $"Party not found: {partyNameOrId}" };

            if (party == MobileParty.MainParty)
                return new { error = "Cannot disband the player's main party" };

            if (party.LeaderHero?.Clan != Clan.PlayerClan)
                return new { error = $"Can only disband parties led by player clan members" };

            var leaderName = party.LeaderHero?.Name?.ToString();
            var partyName = party.Name?.ToString();
            var memberCount = party.MemberRoster.TotalManCount;

            DisbandPartyAction.StartDisband(party);

            return new
            {
                /// Status message
                message = $"Disbanded {partyName}",
                /// Leader who was commanding the party
                leader = leaderName,
                /// How many members were in the party
                memberCount,
            };
        });
    }

    [Tool("party/transfer_troops", Description = "Move troops between the player's party and another nearby party. Parties must be within 5 map units or in the same settlement.")]
    public partial Task<object> TransferTroops(
        [ToolParameter(Description = "Other party: leader name or party string ID")] string otherPartyNameOrId,
        [ToolParameter(Description = "Troop character name or string ID")] string troopNameOrId,
        [ToolParameter(Description = "How many to transfer")] int count,
        [ToolParameter(Description = "'give' to send troops from player to other party, 'take' to pull troops from other party to player. Default: 'give'", Required = false)] string direction = "give")
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var mainParty = MobileParty.MainParty;
            if (mainParty == null)
                return new { error = "No main party" };

            var otherParty = FindParty(otherPartyNameOrId);
            if (otherParty == null)
                return new { error = $"Party not found: {otherPartyNameOrId}" };

            if (otherParty == mainParty)
                return new { error = "Source and destination are the same party" };

            if (!ArePartiesNearby(mainParty, otherParty))
                return new { error = $"Too far from {otherParty.Name}. Move closer or enter the same settlement." };

            var giving = direction?.ToLowerInvariant() != "take";
            var from = giving ? mainParty : otherParty;
            var to = giving ? otherParty : mainParty;

            var troop = CharacterObject.All.FirstOrDefault(c => c.StringId == troopNameOrId)
                        ?? CharacterObject.All.FirstOrDefault(c => c.Name?.ToString().Equals(troopNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (troop == null)
                return new { error = $"Troop type not found: {troopNameOrId}" };

            if (troop.IsHero && troop.HeroObject == from.LeaderHero)
                return new { error = $"Cannot transfer a party's leader" };

            var available = from.MemberRoster.GetTroopCount(troop);
            if (available <= 0)
                return new { error = $"{from.Name} has no {troop.Name} to transfer" };

            var actual = Math.Min(count, available);
            from.MemberRoster.RemoveTroop(troop, actual);
            to.MemberRoster.AddToCounts(troop, actual);

            return new
            {
                /// Status message
                message = $"Moved {actual}x {troop.Name} from {from.Name} to {to.Name}",
                /// Troop name
                troop = troop.Name?.ToString(),
                /// Number actually moved
                transferred = actual,
                /// Remaining in source
                sourceRemaining = from.MemberRoster.TotalManCount,
                /// New total in destination
                destinationTotal = to.MemberRoster.TotalManCount,
            };
        });
    }

    [Tool("party/transfer_prisoners", Description = "Move prisoners between the player's party and another nearby party. Parties must be within 5 map units or in the same settlement.")]
    public partial Task<object> TransferPrisoners(
        [ToolParameter(Description = "Other party: leader name or party string ID")] string otherPartyNameOrId,
        [ToolParameter(Description = "Prisoner character name or string ID")] string prisonerNameOrId,
        [ToolParameter(Description = "How many to transfer")] int count,
        [ToolParameter(Description = "'give' or 'take'. Default: 'give'", Required = false)] string direction = "give")
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var mainParty = MobileParty.MainParty;
            if (mainParty == null)
                return new { error = "No main party" };

            var otherParty = FindParty(otherPartyNameOrId);
            if (otherParty == null)
                return new { error = $"Party not found: {otherPartyNameOrId}" };

            if (otherParty == mainParty)
                return new { error = "Source and destination are the same party" };

            if (!ArePartiesNearby(mainParty, otherParty))
                return new { error = $"Too far from {otherParty.Name}." };

            var giving = direction?.ToLowerInvariant() != "take";
            var from = giving ? mainParty : otherParty;
            var to = giving ? otherParty : mainParty;

            var prisoner = CharacterObject.All.FirstOrDefault(c => c.StringId == prisonerNameOrId)
                           ?? CharacterObject.All.FirstOrDefault(c => c.Name?.ToString().Equals(prisonerNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (prisoner == null)
                return new { error = $"Character not found: {prisonerNameOrId}" };

            var available = from.PrisonRoster.GetTroopCount(prisoner);
            if (available <= 0)
                return new { error = $"{from.Name} holds no prisoner of type {prisoner.Name}" };

            var actual = Math.Min(count, available);
            from.PrisonRoster.RemoveTroop(prisoner, actual);
            to.PrisonRoster.AddToCounts(prisoner, actual);

            return new
            {
                /// Status message
                message = $"Moved {actual}x {prisoner.Name} (prisoner) from {from.Name} to {to.Name}",
                /// Prisoner type name
                prisoner = prisoner.Name?.ToString(),
                /// Number actually moved
                transferred = actual,
                /// Remaining prisoners in source
                sourceRemaining = from.PrisonRoster.TotalManCount,
                /// Prisoners now in destination
                destinationTotal = to.PrisonRoster.TotalManCount,
            };
        });
    }

    [Tool("party/order_companion_party", Description = "Give a movement order to a companion-led party. The party must not be the player's main party.")]
    public partial Task<object> OrderCompanionParty(
        [ToolParameter(Description = "Companion party: leader name or party string ID")] string partyNameOrId,
        [ToolParameter(Description = "Order: 'go_to_settlement', 'escort_player', 'hold', 'engage_party', 'besiege_settlement', 'raid_village'")] string order,
        [ToolParameter(Description = "Target name or string ID (for orders that need a target)", Required = false)] string? target)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = FindParty(partyNameOrId);
            if (party == null)
                return new { error = $"Party not found: {partyNameOrId}" };

            if (party == MobileParty.MainParty)
                return new { error = "Use party/move_to_settlement or party/engage_party for the player's party" };

            party.Ai.SetDoNotMakeNewDecisions(false);

            string message;
            string? targetName = null;

            switch (order?.ToLowerInvariant())
            {
                case "go_to_settlement":
                {
                    var settlement = target != null ? FindSettlement(target) : null;
                    if (settlement == null)
                        return new { error = $"Settlement not found: {target}" };

#if v1313 || v1315
                        SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, false, false);
#else
                    SetPartyAiAction.GetActionForVisitingSettlement(party, settlement);
#endif
                    message = $"{party.Name} moving to {settlement.Name}";
                    targetName = settlement.Name?.ToString();
                    break;
                }

                case "escort_player":
                {
                    if (MobileParty.MainParty == null)
                        return new { error = "No player party to escort" };

#if v1313 || v1315
                        SetPartyAiAction.GetActionForEscortingParty(party, MobileParty.MainParty, MobileParty.NavigationType.Default, false, false);
#else
                    SetPartyAiAction.GetActionForEscortingParty(party, MobileParty.MainParty);
#endif
                    message = $"{party.Name} escorting the player";
                    break;
                }

                case "hold":
                {
#if v1313 || v1315
                        party.SetMoveModeHold();
#else
                    party.Ai.SetMoveModeHold();
#endif
                    message = $"{party.Name} holding at ({party.GetPosition2D.X:F1}, {party.GetPosition2D.Y:F1})";
                    break;
                }

                case "engage_party":
                {
                    var targetParty = target != null ? FindParty(target) : null;
                    if (targetParty == null)
                        return new { error = $"Target party not found: {target}" };

#if v1313 || v1315
                        party.SetMoveEngageParty(targetParty, MobileParty.NavigationType.Default);
#else
                    party.Ai.SetMoveEngageParty(targetParty);
#endif
                    message = $"{party.Name} engaging {targetParty.Name}";
                    targetName = targetParty.Name?.ToString();
                    break;
                }

                case "besiege_settlement":
                {
                    var settlement = target != null ? FindSettlement(target) : null;
                    if (settlement == null)
                        return new { error = $"Settlement not found: {target}" };

                    if (!settlement.IsFortification)
                        return new { error = $"{settlement.Name} is not a fortification" };

#if v1313 || v1315
                        party.SetMoveBesiegeSettlement(settlement, MobileParty.NavigationType.Default);
#else
                    party.Ai.SetMoveBesiegeSettlement(settlement);
#endif
                    message = $"{party.Name} marching to besiege {settlement.Name}";
                    targetName = settlement.Name?.ToString();
                    break;
                }

                case "raid_village":
                {
                    var settlement = target != null ? FindSettlement(target) : null;
                    if (settlement == null)
                        return new { error = $"Settlement not found: {target}" };

                    if (!settlement.IsVillage)
                        return new { error = $"{settlement.Name} is not a village" };

#if v1313 || v1315
                        party.SetMoveRaidSettlement(settlement, MobileParty.NavigationType.Default);
#else
                    party.Ai.SetMoveRaidSettlement(settlement);
#endif
                    message = $"{party.Name} moving to raid {settlement.Name}";
                    targetName = settlement.Name?.ToString();
                    break;
                }

                default:
                    return new { error = $"Unknown order: {order}. Valid: go_to_settlement, escort_player, hold, engage_party, besiege_settlement, raid_village" };
            }

            return new
            {
                /// Status message
                message,
                /// Party string ID
                partyId = party.StringId,
                /// Order issued
                orderType = order,
                /// Target name, if applicable
                target = targetName,
            };
        });
    }

    [Tool("party/start_siege", Description = "Begin a siege on a town or castle. The besieging party must be near the settlement and at war with its owner. Omit partyNameOrId to use the player's party.")]
    public partial Task<object> StartSiege(
        [ToolParameter(Description = "Target town or castle name or string ID")] string settlementNameOrId,
        [ToolParameter(Description = "Besieging party leader name or party string ID. Omit for player's party.", Required = false)] string? partyNameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var party = string.IsNullOrEmpty(partyNameOrId)
                ? MobileParty.MainParty
                : FindParty(partyNameOrId);

            if (party == null)
                return new { error = string.IsNullOrEmpty(partyNameOrId) ? "No main party" : $"Party not found: {partyNameOrId}" };

            var settlement = FindSettlement(settlementNameOrId);
            if (settlement == null)
                return new { error = $"Settlement not found: {settlementNameOrId}" };

            if (!settlement.IsFortification)
                return new { error = $"{settlement.Name} is not a town or castle" };

            if (settlement.IsUnderSiege)
                return new { error = $"{settlement.Name} is already under siege" };

            var dist = party.GetPosition2D.Distance(settlement.GetPosition2D);
            if (dist > 5f)
                return new { error = $"Too far from {settlement.Name} ({dist:F1} units away). Move closer first." };

            // Let the game engine handle war-state validation internally
            Campaign.Current.SiegeEventManager.StartSiegeEvent(settlement, party);

            return new
            {
                /// Status message
                message = $"{party.Name} is now besieging {settlement.Name}",
                /// Besieging party name
                partyName = party.Name?.ToString(),
                /// Target settlement name
                settlementName = settlement.Name?.ToString(),
                /// Target settlement string ID
                settlementId = settlement.StringId,
                /// Garrison strength
                garrisonCount = settlement.Town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0,
                /// Siege confirmation
                isUnderSiege = settlement.IsUnderSiege,
            };
        });
    }
}