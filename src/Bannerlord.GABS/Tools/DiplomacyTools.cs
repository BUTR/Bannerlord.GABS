// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace Bannerlord.GABS.Tools;

public partial class DiplomacyTools
{
    [Tool("diplomacy/declare_war", Description = "Declare war on a faction (kingdom). The player's faction declares war.")]
    public partial Task<object> DeclareWar(
        [ToolParameter(Description = "Target kingdom name or string ID")] string targetFaction)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var playerFaction = Clan.PlayerClan?.Kingdom as IFaction ?? Clan.PlayerClan as IFaction;
            if (playerFaction == null)
                return new { error = "Player has no faction" };

            var target = Kingdom.All.FirstOrDefault(k => k.StringId == targetFaction)
                         ?? Kingdom.All.FirstOrDefault(k => k.Name?.ToString().Equals(targetFaction, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
                return new { error = $"Kingdom not found: {targetFaction}" };

            if (target == playerFaction)
                return new { error = "Cannot declare war on own faction" };

            if (FactionManager.IsAtWarAgainstFaction(playerFaction, target))
                return new { error = $"Already at war with {target.Name}" };

            DeclareWarAction.ApplyByDefault(playerFaction, target);

            return new
            {
                /// Status message
                message = $"{playerFaction.Name} declared war on {target.Name}",
            };
        });
    }

    [Tool("diplomacy/make_peace", Description = "Make peace with a faction the player is at war with.")]
    public partial Task<object> MakePeace(
        [ToolParameter(Description = "Target kingdom name or string ID")] string targetFaction)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var playerFaction = Clan.PlayerClan?.Kingdom as IFaction ?? Clan.PlayerClan as IFaction;
            if (playerFaction == null)
                return new { error = "Player has no faction" };

            var target = Kingdom.All.FirstOrDefault(k => k.StringId == targetFaction)
                         ?? Kingdom.All.FirstOrDefault(k => k.Name?.ToString().Equals(targetFaction, StringComparison.OrdinalIgnoreCase) == true);

            if (target == null)
                return new { error = $"Kingdom not found: {targetFaction}" };

            if (!FactionManager.IsAtWarAgainstFaction(playerFaction, target))
                return new { error = $"Not at war with {target.Name}" };

            MakePeaceAction.Apply(playerFaction, target);

            return new
            {
                /// Status message
                message = $"{playerFaction.Name} made peace with {target.Name}",
            };
        });
    }

    [Tool("diplomacy/change_relation", Description = "Change the player's relation with a hero.")]
    public partial Task<object> ChangeRelation(
        [ToolParameter(Description = "Hero name or string ID")] string heroNameOrId,
        [ToolParameter(Description = "Relation change amount (positive or negative)")] int amount)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            if (Hero.MainHero == null)
                return new { error = "No main hero" };

            var hero = Hero.FindFirst(h => h.StringId == heroNameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(heroNameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (hero == null)
                return new { error = $"Hero not found: {heroNameOrId}" };

            if (hero == Hero.MainHero)
                return new { error = "Cannot change relation with yourself" };

            var oldRelation = Hero.MainHero.GetRelation(hero);
            ChangeRelationAction.ApplyPlayerRelation(hero, amount);
            var newRelation = Hero.MainHero.GetRelation(hero);

            return new
            {
                /// Hero name
                hero = hero.Name?.ToString(),
                /// Relation before change
                oldRelation,
                /// Relation after change
                newRelation,
                /// Actual change amount
                change = newRelation - oldRelation,
            };
        });
    }
}