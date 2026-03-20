// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.GABS.Tools;

public partial class MissionTools
{
    [Tool("mission/leave", Description = "Leave the current mission/scene (lord's hall, tavern, village, etc.). Equivalent to pressing Tab. Returns immediately — use core/wait_for_state (expectedState='campaign_map' or 'game_menu') to block until the scene exits.")]
    public partial Task<object> LeaveMission()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            if (mission.MissionEnded)
                return new { error = "Mission already ended" };

            mission.EndMission();
            return new
            {
                /// Status message
                message = "Leaving mission",
            };
        });
    }

    [Tool("mission/list_agents", Description = "List all NPC agents in the current mission scene. Shows heroes and named characters you can interact with.")]
    public partial Task<object> ListAgents(
        [ToolParameter(Description = "Filter: 'heroes' for hero agents only, 'all' for everyone (default: 'heroes')", Required = false)] string? filter)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var mainAgent = mission.MainAgent;
            if (mainAgent == null)
                return new { error = "No player agent in mission" };

            var heroesOnly = filter != "all";
            var agents = mission.Agents
                .Where(agent => agent != null && agent.IsActive() && agent != mainAgent && !agent.IsMount)
                .Select(agent => (character: agent.Character as CharacterObject, agent))
                .Where(x => x.character != null && (!heroesOnly || x.character.IsHero))
                .Select(x =>
                {
                    var dx = x.agent.Position.x - mainAgent.Position.x;
                    var dy = x.agent.Position.y - mainAgent.Position.y;
                    var distance = Math.Round(Math.Sqrt(dx * dx + dy * dy), 1);
                    var hero = x.character!.IsHero ? x.character.HeroObject : null;

                    return new
                    {
                        /// Agent display name
                        name = x.agent.Name,
                        /// Whether this agent is a hero character
                        isHero = x.character.IsHero,
                        /// Distance from the player agent
                        distance,
                        /// Hero string ID (null for non-heroes)
                        heroId = hero?.StringId,
                        /// Hero clan name (null for non-heroes)
                        clan = hero?.Clan?.Name?.ToString(),
                        /// Hero faction name (null for non-heroes)
                        faction = hero?.MapFaction?.Name?.ToString(),
                    };
                })
                .OrderBy(a => a.distance)
                .ToList();

            return new
            {
                /// Number of agents found
                count = agents.Count,
                /// Array of agent info objects with name, heroName, isHero, distance
                agents,
            };
        });
    }

    [Tool("mission/talk_to_agent", Description = "Start a conversation with an NPC agent in the current mission scene. Use list_agents first to see who is available. Returns immediately — use conversation/wait_for_state to block until dialogue appears.")]
    public partial Task<object> TalkToAgent(
        [ToolParameter(Description = "Hero name or hero string ID to talk to")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var mainAgent = mission.MainAgent;
            if (mainAgent == null)
                return new { error = "No player agent in mission" };

            if (Campaign.Current?.ConversationManager == null)
                return new { error = "No conversation manager available" };

            if (Campaign.Current.ConversationManager.IsConversationInProgress)
                return new { error = "A conversation is already in progress" };

            // Find the target agent by hero name or ID
            Agent? targetAgent = null;
            var nameOrIdLower = nameOrId.ToLowerInvariant();

            foreach (var agent in mission.Agents)
            {
                if (agent == null || !agent.IsActive() || agent == mainAgent || agent.IsMount)
                    continue;

                if (agent.Character is not CharacterObject { IsHero: true } character || character.HeroObject == null)
                    continue;

                var hero = character.HeroObject;
                if (hero.StringId.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) ||
                    hero.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true ||
                    hero.Name?.ToString().ToLowerInvariant().Contains(nameOrIdLower) == true)
                {
                    targetAgent = agent;
                    break;
                }
            }

            if (targetAgent == null)
                return new { error = $"Agent not found in current mission: {nameOrId}. Use list_agents to see available NPCs." };

            var targetCharacter = targetAgent.Character as CharacterObject;
            var heroName = targetCharacter?.HeroObject?.Name?.ToString() ?? targetAgent.Name;

            Campaign.Current.ConversationManager.SetupAndStartMissionConversation(targetAgent, mainAgent, false);

            return new
            {
                /// Name of the hero being talked to
                hero = heroName,
                /// Status message
                message = $"Started conversation with {heroName}",
            };
        });
    }
}