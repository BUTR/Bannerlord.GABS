// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace Bannerlord.GABS.Tools;

public partial class HeroTools
{
    [ToolResponse("id", Type = "string", Description = "Hero string ID")]
    [ToolResponse("name", Type = "string", Description = "Hero name", Nullable = true)]
    [ToolResponse("age", Type = "integer", Description = "Hero age")]
    [ToolResponse("isFemale", Type = "boolean", Description = "Whether hero is female")]
    [ToolResponse("isAlive", Type = "boolean", Description = "Whether hero is alive")]
    [ToolResponse("state", Type = "string", Description = "Hero state (Active, Fugitive, Prisoner, etc.)")]
    [ToolResponse("clan", Type = "string", Description = "Clan name", Nullable = true)]
    [ToolResponse("faction", Type = "string", Description = "Faction name", Nullable = true)]
    [ToolResponse("gold", Type = "integer", Description = "Gold amount")]
    [ToolResponse("occupation", Type = "string", Description = "Hero occupation")]
    [ToolResponse("hitPoints", Type = "integer", Description = "Current hit points (detailed)", Nullable = true)]
    [ToolResponse("maxHitPoints", Type = "integer", Description = "Max hit points (detailed)", Nullable = true)]
    [ToolResponse("isWounded", Type = "boolean", Description = "Whether wounded (detailed)", Nullable = true)]
    [ToolResponse("isPrisoner", Type = "boolean", Description = "Whether prisoner (detailed)", Nullable = true)]
    [ToolResponse("isLord", Type = "boolean", Description = "Whether hero is a lord (detailed)", Nullable = true)]
    [ToolResponse("isPlayerCompanion", Type = "boolean", Description = "Whether hero is a player companion (detailed)", Nullable = true)]
    [ToolResponse("isNotable", Type = "boolean", Description = "Whether hero is a notable (detailed)", Nullable = true)]
    [ToolResponse("isFactionLeader", Type = "boolean", Description = "Whether hero leads a faction (detailed)", Nullable = true)]
    [ToolResponse("isKingdomLeader", Type = "boolean", Description = "Whether hero leads a kingdom (detailed)", Nullable = true)]
    [ToolResponse("isClanLeader", Type = "boolean", Description = "Whether hero leads a clan (detailed)", Nullable = true)]
    [ToolResponse("currentSettlement", Type = "string", Description = "Current settlement (detailed)", Nullable = true)]
    [ToolResponse("partyName", Type = "string", Description = "Party name (detailed)", Nullable = true)]
    [ToolResponse("governorOf", Type = "string", Description = "Settlement governed by hero (detailed)", Nullable = true)]
    [ToolResponse("spouse", Type = "string", Description = "Spouse name (detailed)", Nullable = true)]
    [ToolResponse("father", Type = "string", Description = "Father name (detailed)", Nullable = true)]
    [ToolResponse("mother", Type = "string", Description = "Mother name (detailed)", Nullable = true)]
    [ToolResponse("children", Type = "array", Description = "Children names (detailed)", Nullable = true)]
    private static object SerializeHero(Hero hero, bool detailed = false)
    {
        var basic = new Dictionary<string, object?>
        {
            ["id"] = hero.StringId,
            ["name"] = hero.Name?.ToString(),
            ["age"] = (int) hero.Age,
            ["isFemale"] = hero.IsFemale,
            ["isAlive"] = hero.IsAlive,
            ["state"] = hero.HeroState.ToString(),
            ["clan"] = hero.Clan?.Name?.ToString(),
            ["faction"] = hero.MapFaction?.Name?.ToString(),
            ["gold"] = hero.Gold,
            ["occupation"] = hero.Occupation.ToString(),
        };

        if (detailed)
        {
            basic["hitPoints"] = hero.HitPoints;
            basic["maxHitPoints"] = hero.MaxHitPoints;
            basic["isWounded"] = hero.IsWounded;
            basic["isPrisoner"] = hero.IsPrisoner;
            basic["isLord"] = hero.IsLord;
            basic["isPlayerCompanion"] = hero.IsPlayerCompanion;
            basic["isNotable"] = hero.IsNotable;
            basic["isFactionLeader"] = hero.IsFactionLeader;
            basic["isKingdomLeader"] = hero.Clan?.Kingdom?.Leader == hero;
            basic["isClanLeader"] = hero.Clan?.Leader == hero;
            basic["currentSettlement"] = hero.CurrentSettlement?.Name?.ToString();
            basic["partyName"] = hero.PartyBelongedTo?.Name?.ToString();
            basic["governorOf"] = hero.GovernorOf?.Name?.ToString();
            basic["spouse"] = hero.Spouse?.Name?.ToString();
            basic["father"] = hero.Father?.Name?.ToString();
            basic["mother"] = hero.Mother?.Name?.ToString();
            basic["children"] = hero.Children?.Select(c => c.Name?.ToString()).ToList();
        }

        return basic;
    }

    [Tool("hero/get_player", Description = "Get full info about the player hero (name, stats, gold, location, clan, etc.).")]
    public partial Task<object> GetPlayer()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var hero = Hero.MainHero;
            if (hero == null)
                return new { error = "No main hero" };

            return SerializeHero(hero, detailed: true);
        });
    }

    [Tool("hero/get_hero", Description = "Get info about a specific hero by name or string ID.")]
    public partial Task<object> GetHero(
        [ToolParameter(Description = "Hero name or string ID to look up")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            return SerializeHero(hero, detailed: true);
        });
    }

    [Tool("hero/list_heroes", Description = "List heroes with optional filters. Returns name, clan, faction, state for each.")]
    public partial Task<object> ListHeroes(
        [ToolParameter(Description = "Filter: 'alive', 'dead', 'prisoner', 'wounded', 'lord', 'companion', 'notable', 'wanderer'", Required = false)] string? filter,
        [ToolParameter(Description = "Filter by faction name", Required = false)] string? faction,
        [ToolParameter(Description = "Filter by clan name", Required = false)] string? clan,
        [ToolParameter(Description = "Max results to return (default 50)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 50;
            IEnumerable<Hero> heroes = Hero.AllAliveHeroes;

            if (filter != null)
            {
                switch (filter.ToLowerInvariant())
                {
                    case "alive": heroes = Hero.AllAliveHeroes; break;
                    case "dead": heroes = Hero.DeadOrDisabledHeroes.Where(h => h.IsDead); break;
                    case "prisoner": heroes = Hero.AllAliveHeroes.Where(h => h.IsPrisoner); break;
                    case "wounded": heroes = Hero.AllAliveHeroes.Where(h => h.IsWounded); break;
                    case "lord": heroes = Hero.AllAliveHeroes.Where(h => h.IsLord); break;
                    case "companion": heroes = Hero.AllAliveHeroes.Where(h => h.IsPlayerCompanion); break;
                    case "notable": heroes = Hero.AllAliveHeroes.Where(h => h.IsNotable); break;
                    case "wanderer": heroes = Hero.AllAliveHeroes.Where(h => h.IsWanderer); break;
                }
            }

            if (!string.IsNullOrEmpty(faction))
                heroes = heroes.Where(h => h.MapFaction?.Name?.ToString().Equals(faction, StringComparison.OrdinalIgnoreCase) == true);

            if (!string.IsNullOrEmpty(clan))
                heroes = heroes.Where(h => h.Clan?.Name?.ToString().Equals(clan, StringComparison.OrdinalIgnoreCase) == true);

            var list = heroes.Take(max).Select(h => SerializeHero(h)).ToList();

            return new
            {
                /// Number of heroes returned
                count = list.Count,
                /// Array of hero summary objects
                heroes = list,
            };
        });
    }

    [Tool("hero/get_skills", Description = "Get all skill levels for a hero.")]
    public partial Task<object> GetSkills(
        [ToolParameter(Description = "Hero name or string ID (omit for player hero)", Required = false)] string? nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            Hero? hero;
            if (string.IsNullOrEmpty(nameOrId))
            {
                hero = Hero.MainHero;
            }
            else
            {
                hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            return new
            {
                /// Hero name
                hero = hero.Name?.ToString(),
                /// Array of skill objects with name and level
                skills = new[]
                {
                    new
                    {
                        /// Skill name
                        name = "OneHanded",
                        /// Skill level
                        level = hero.GetSkillValue(DefaultSkills.OneHanded)
                    },
                    new { name = "TwoHanded", level = hero.GetSkillValue(DefaultSkills.TwoHanded) },
                    new { name = "Polearm", level = hero.GetSkillValue(DefaultSkills.Polearm) },
                    new { name = "Bow", level = hero.GetSkillValue(DefaultSkills.Bow) },
                    new { name = "Crossbow", level = hero.GetSkillValue(DefaultSkills.Crossbow) },
                    new { name = "Throwing", level = hero.GetSkillValue(DefaultSkills.Throwing) },
                    new { name = "Riding", level = hero.GetSkillValue(DefaultSkills.Riding) },
                    new { name = "Athletics", level = hero.GetSkillValue(DefaultSkills.Athletics) },
                    new { name = "Crafting", level = hero.GetSkillValue(DefaultSkills.Crafting) },
                    new { name = "Tactics", level = hero.GetSkillValue(DefaultSkills.Tactics) },
                    new { name = "Scouting", level = hero.GetSkillValue(DefaultSkills.Scouting) },
                    new { name = "Roguery", level = hero.GetSkillValue(DefaultSkills.Roguery) },
                    new { name = "Charm", level = hero.GetSkillValue(DefaultSkills.Charm) },
                    new { name = "Leadership", level = hero.GetSkillValue(DefaultSkills.Leadership) },
                    new { name = "Trade", level = hero.GetSkillValue(DefaultSkills.Trade) },
                    new { name = "Steward", level = hero.GetSkillValue(DefaultSkills.Steward) },
                    new { name = "Medicine", level = hero.GetSkillValue(DefaultSkills.Medicine) },
                    new { name = "Engineering", level = hero.GetSkillValue(DefaultSkills.Engineering) },
                },
            };
        });
    }

    [Tool("hero/get_traits", Description = "Get personality traits for a hero (mercy, valor, honor, generosity, calculating).")]
    public partial Task<object> GetTraits(
        [ToolParameter(Description = "Hero name or string ID (omit for player hero)", Required = false)] string? nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            Hero? hero;
            if (string.IsNullOrEmpty(nameOrId))
            {
                hero = Hero.MainHero;
            }
            else
            {
                hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            return new
            {
                /// Hero name
                hero = hero.Name?.ToString(),
                /// Array of trait objects with name and level (-2 to 2)
                traits = new[]
                {
                    new
                    {
                        /// Trait name
                        name = "Mercy",
                        /// Trait level (-2 to 2)
                        level = hero.GetTraitLevel(DefaultTraits.Mercy)
                    },
                    new { name = "Valor", level = hero.GetTraitLevel(DefaultTraits.Valor) },
                    new { name = "Honor", level = hero.GetTraitLevel(DefaultTraits.Honor) },
                    new { name = "Generosity", level = hero.GetTraitLevel(DefaultTraits.Generosity) },
                    new { name = "Calculating", level = hero.GetTraitLevel(DefaultTraits.Calculating) },
                },
            };
        });
    }

    [Tool("hero/get_relationships", Description = "Get a hero's relations with other heroes they've interacted with.")]
    public partial Task<object> GetRelationships(
        [ToolParameter(Description = "Hero name or string ID (omit for player hero)", Required = false)] string? nameOrId,
        [ToolParameter(Description = "Max results (default 20)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 20;

            Hero? hero;
            if (string.IsNullOrEmpty(nameOrId))
            {
                hero = Hero.MainHero;
            }
            else
            {
                hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);
            }

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            var relations = Hero.AllAliveHeroes
                .Where(h => h != hero)
                .Select(h => new { name = h.Name?.ToString(), relation = hero.GetRelation(h) })
                .Where(r => r.relation != 0)
                .OrderByDescending(r => Math.Abs(r.relation))
                .Take(max)
                .Select(r => new { r.name, r.relation })
                .ToList();

            return new
            {
                /// Hero name
                hero = hero.Name?.ToString(),
                /// Number of relations returned
                count = relations.Count,
                /// Array of {name, relation} objects sorted by absolute relation value
                relations,
            };
        });
    }

    [Tool("hero/kill_hero", Description = "Kill a hero (cheat). Requires cheat mode. Use to remove NPCs for testing.")]
    public partial Task<object> KillHero(
        [ToolParameter(Description = "Hero name or string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            if (!Game.Current.CheatMode)
                return new { error = "Cheat mode must be enabled first (use core/set_cheat_mode)" };

            var hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            if (!hero.IsAlive)
                return new { error = $"{hero.Name} is already dead" };

            if (hero == Hero.MainHero)
                return new { error = "Cannot kill the player hero" };

            KillCharacterAction.ApplyByRemove(hero, true, true);

            return new
            {
                /// Name of the killed hero
                hero = hero.Name?.ToString(),
                /// Status message
                message = $"{hero.Name} has been killed",
            };
        });
    }
}