// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS.Tools;

public partial class KingdomTools
{
    [Tool("kingdom/get_player_kingdom", Description = "Get the player's kingdom info (or clan if independent).")]
    public partial Task<object> GetPlayerKingdom()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var clan = Clan.PlayerClan;
            if (clan == null)
                return new { error = "No player clan" };

            if (clan.Kingdom == null)
            {
                return new
                {
                    /// Whether the player clan has no kingdom
                    isIndependent = true,
                    /// Player clan name
                    clan = clan.Name?.ToString(),
                    /// Clan tier level
                    clanTier = clan.Tier,
                    /// Clan renown
                    renown = Math.Round(clan.Renown, 1),
                    /// Clan influence points
                    influence = Math.Round(clan.Influence, 1),
                    /// Clan treasury gold
                    gold = clan.Gold,
                    /// Array of fief name strings
                    fiefs = clan.Fiefs?.Select(f => f.Name?.ToString()).ToList(),
                    /// Kingdom name if in one
                    kingdom = (string?) null,
                };
            }

            var kingdom = clan.Kingdom;
            return new
            {
                isIndependent = false,
                clan = clan.Name?.ToString(),
                clanTier = clan.Tier,
                renown = Math.Round(clan.Renown, 1),
                influence = Math.Round(clan.Influence, 1),
                gold = clan.Gold,
                fiefs = clan.Fiefs?.Select(f => f.Name?.ToString()).ToList(),
                kingdom = kingdom.Name?.ToString(),
            };
        });
    }

    [Tool("kingdom/list_kingdoms", Description = "List all kingdoms with basic info.")]
    public partial Task<object> ListKingdoms()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var kingdoms = Kingdom.All
                .Where(k => !k.IsEliminated)
                .Select(k => new
                {
                    /// Kingdom string ID
                    id = k.StringId,
                    /// Kingdom name
                    name = k.Name?.ToString(),
                    /// Kingdom leader name
                    leader = k.Leader?.Name?.ToString(),
                    /// Kingdom culture name
                    culture = k.Culture?.Name?.ToString(),
                    /// Number of clans
                    clanCount = k.Clans?.Count ?? 0,
                    /// Number of fiefs
                    fiefCount = k.Fiefs?.Count ?? 0,
                    /// Total military strength
#if v1313 || v1315
                        strength = Math.Round(k.CurrentTotalStrength, 0),
#else
                    strength = Math.Round(k.TotalStrength, 0),
#endif
                    /// Array of enemy kingdom names
                    atWarWith = GetWarsForFaction(k),
                })
                .ToList();

            return new
            {
                /// Number of kingdoms
                count = kingdoms.Count,
                /// Array of kingdom summary objects
                kingdoms,
            };
        });
    }

    [Tool("kingdom/get_kingdom", Description = "Get detailed info about a kingdom by name or string ID.")]
    public partial Task<object> GetKingdom(
        [ToolParameter(Description = "Kingdom name or string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var kingdom = Kingdom.All.FirstOrDefault(k => k.StringId == nameOrId)
                          ?? Kingdom.All.FirstOrDefault(k => k.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (kingdom == null)
                return new { error = $"Kingdom not found: {nameOrId}" };

            return new
            {
                /// Kingdom string ID
                id = kingdom.StringId,
                /// Kingdom name
                name = kingdom.Name?.ToString(),
                /// Kingdom leader name
                leader = kingdom.Leader?.Name?.ToString(),
                /// Ruling clan name
                rulingClan = kingdom.RulingClan?.Name?.ToString(),
                /// Kingdom culture name
                culture = kingdom.Culture?.Name?.ToString(),
                /// Total military strength
#if v1313 || v1315
                    strength = Math.Round(kingdom.CurrentTotalStrength, 0),
#else
                strength = Math.Round(kingdom.TotalStrength, 0),
#endif
                /// Whether the kingdom is eliminated
                isEliminated = kingdom.IsEliminated,
                /// Array of clan objects with name, leader, tier, fiefCount
                clans = kingdom.Clans?.Select(c => new
                {
                    name = c.Name?.ToString(),
                    leader = c.Leader?.Name?.ToString(),
                    tier = c.Tier,
                    fiefCount = c.Fiefs?.Count ?? 0,
                }).ToList(),
                /// Array of fief objects with name, type, owner
                fiefs = kingdom.Fiefs?.Select(f => new
                {
                    name = f.Name?.ToString(),
                    type = f.IsTown ? "town" : "castle",
                    owner = f.OwnerClan?.Name?.ToString(),
                }).ToList(),
                /// Array of army objects with name, leader, partyCount
                armies = kingdom.Armies?.Select(a => new
                {
                    name = a.Name?.ToString(),
                    leader = a.LeaderParty?.LeaderHero?.Name?.ToString(),
                    partyCount = a.Parties?.Count ?? 0,
                }).ToList(),
                /// Array of active policy names
                policies = kingdom.ActivePolicies?.Select(p => p.Name?.ToString()).ToList(),
                /// Array of enemy kingdom names
                atWarWith = GetWarsForFaction(kingdom),
            };
        });
    }

    [Tool("kingdom/list_wars", Description = "List all active wars between factions.")]
    public partial Task<object> ListWars()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var seen = new HashSet<string>();
            var activeKingdoms = Kingdom.All.Where(k => !k.IsEliminated).ToList();

            var wars = activeKingdoms
                .SelectMany(k1 => activeKingdoms
                    .Where(k2 => k1 != k2 && FactionManager.IsAtWarAgainstFaction(k1, k2))
                    .Select(k2 => new { k1, k2 }))
                .Where(pair =>
                {
                    var key = string.Compare(pair.k1.StringId, pair.k2.StringId, StringComparison.Ordinal) < 0
                        ? $"{pair.k1.StringId}|{pair.k2.StringId}"
                        : $"{pair.k2.StringId}|{pair.k1.StringId}";
                    return seen.Add(key);
                })
                .Select(pair => new
                {
                    /// First faction name in the war
                    faction1 = pair.k1.Name?.ToString(),
                    /// Second faction name in the war
                    faction2 = pair.k2.Name?.ToString(),
                })
                .ToList();

            return new
            {
                /// Number of active wars
                count = wars.Count,
                /// Array of war pairs with faction1 and faction2
                wars,
            };
        });
    }

    [Tool("kingdom/get_clan", Description = "Get detailed info about a clan by name or string ID.")]
    public partial Task<object> GetClan(
        [ToolParameter(Description = "Clan name or string ID (omit for player clan)", Required = false)] string? nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            Clan? clan;
            if (string.IsNullOrEmpty(nameOrId))
            {
                clan = Clan.PlayerClan;
            }
            else
            {
                clan = Clan.All.FirstOrDefault(c => c.StringId == nameOrId)
                       ?? Clan.All.FirstOrDefault(c => c.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (clan == null)
                return new { error = $"Clan not found: {nameOrId}" };

            return new
            {
                /// Clan string ID
                id = clan.StringId,
                /// Clan name
                name = clan.Name?.ToString(),
                /// Clan leader name
                leader = clan.Leader?.Name?.ToString(),
                /// Kingdom name if in one
                kingdom = clan.Kingdom?.Name?.ToString(),
                /// Clan culture name
                culture = clan.Culture?.Name?.ToString(),
                /// Clan tier
                tier = clan.Tier,
                /// Clan renown
                renown = Math.Round(clan.Renown, 1),
                /// Renown required for next tier
                renownForNextTier = clan.RenownRequirementForNextTier,
                /// Clan influence
                influence = Math.Round(clan.Influence, 1),
                /// Clan gold
                gold = clan.Gold,
                /// Whether the clan is noble
                isNoble = clan.IsNoble,
                /// Whether the clan is a minor faction
                isMinorFaction = clan.IsMinorFaction,
                /// Whether the clan is under mercenary service
                isUnderMercenaryService = clan.IsUnderMercenaryService,
                /// Whether the clan is eliminated
                isEliminated = clan.IsEliminated,
                /// Array of companion name strings
                companions = clan.Companions?.Where(c => c != null).Select(c => c.Name?.ToString()).ToList(),
                /// Array of lord objects with name and age
#if v1313 || v1315
                    lords = clan.AliveLords?.Where(l => l != null).Select(l => new
#else
                lords = clan.Lords?.Where(l => l != null && l.IsAlive).Select(l => new
#endif
                {
                    name = l.Name?.ToString(),
                    age = (int) l.Age,
                }).ToList(),
                /// Array of fief objects with name and type
                fiefs = clan.Fiefs?.Where(f => f != null).Select(f => new
                {
                    name = f.Name?.ToString(),
                    type = f.IsTown ? "town" : "castle",
                }).ToList(),
                /// Array of settlement name strings
                settlements = clan.Settlements?.Where(s => s != null).Select(s => s.Name?.ToString()).ToList(),
                /// Maximum companion count
                companionLimit = clan.CompanionLimit,
                /// Maximum commander count
                commanderLimit = clan.CommanderLimit,
            };
        });
    }

    private static List<string?> GetWarsForFaction(Kingdom kingdom)
    {
        return Kingdom.All
            .Where(k => !k.IsEliminated && k != kingdom && FactionManager.IsAtWarAgainstFaction(kingdom, k))
            .Select(k => k.Name?.ToString())
            .ToList();
    }
}