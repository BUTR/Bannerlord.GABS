// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.GABS.Tools;

public partial class ConversationTools
{
    [Tool("conversation/get_state", Description = "Get the current conversation state: speaker, text, and available options.")]
    public partial Task<object> GetState()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var cm = Campaign.Current.ConversationManager;
            if (cm is not { IsConversationInProgress: true })
                return new
                {
                    /// Whether a conversation is in progress
                    isActive = false,
                    /// Name of the NPC speaking
                    speaker = (string?) null,
                    /// Speaker's faction name
                    speakerFaction = (string?) null,
                    /// Current dialogue text
                    text = (string?) null,
                    /// Number of available dialogue options
                    optionCount = 0,
                    /// Array of option objects with index, id, text, isClickable, hasPersuasion
                    options = (object?) null,
                };

            var speaker = cm.OneToOneConversationHero;
            var text = cm.CurrentSentenceText;

            var persuasionActive = ConversationManager.GetPersuasionIsActive();

            var options = cm.CurOptions?
                .Select((o, i) =>
                {
                    double? successRate = null;
                    double? critSuccessRate = null;
                    double? critFailRate = null;
                    double? failRate = null;

                    if (persuasionActive && o.HasPersuasion)
                    {
                        cm.GetPersuasionChances(o, out var sc, out var csc, out var cfc, out var fc);
                        successRate = Math.Round(sc * 100, 1);
                        critSuccessRate = Math.Round(csc * 100, 1);
                        critFailRate = Math.Round(cfc * 100, 1);
                        failRate = Math.Round(fc * 100, 1);
                    }

                    return new Models.ConversationOptionInfo
                    {
                        Index = i,
                        Id = o.Id,
                        Text = o.Text?.ToString(),
                        IsClickable = o.IsClickable,
                        HasPersuasion = o.HasPersuasion,
                        SkillName = o.SkillName,
                        TraitName = o.TraitName,
                        Hint = o.HintText?.ToString(),
                        SuccessChance = successRate,
                        CritSuccessChance = critSuccessRate,
                        CritFailChance = critFailRate,
                        FailChance = failRate,
                    };
                })
                .ToList();

            return new
            {
                /// Whether a conversation is in progress
                isActive = true,
                /// Name of the NPC speaking
                speaker = speaker?.Name?.ToString(),
                /// Speaker's faction name
                speakerFaction = speaker?.MapFaction?.Name?.ToString(),
                /// Current dialogue text
                text = text,
                /// Number of available dialogue options
                optionCount = options?.Count ?? 0,
                /// Array of option objects with index, id, text, isClickable, hasPersuasion
                options = options,
            };
        });
    }

    [Tool("conversation/select_option", Description = "Select a dialogue option by index (0-based).")]
    public partial Task<object> SelectOption(
        [ToolParameter(Description = "Option index (0-based)")] int index)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var cm = Campaign.Current.ConversationManager;
            if (cm is not { IsConversationInProgress: true })
                return new { error = "No conversation in progress" };

            var options = cm.CurOptions;
            if (options == null || options.Count == 0)
                return new { error = "No options available" };

            if (index < 0 || index >= options.Count)
                return new { error = $"Invalid option index {index}. Valid: 0-{options.Count - 1}" };

            var selected = options[index];
            if (!selected.IsClickable)
                return new { error = $"Option {index} is not clickable: {selected.HintText}" };

            cm.DoOption(index);

            return new
            {
                /// Text of the selected option
                selected = selected.Text?.ToString(),
            };
        });
    }

    [Tool("conversation/continue", Description = "Continue/advance the conversation to the next line (when no options are shown).")]
    public partial Task<object> Continue()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var cm = Campaign.Current.ConversationManager;
            if (cm is not { IsConversationInProgress: true })
                return new { error = "No conversation in progress" };

            cm.ContinueConversation();

            return new
            {
                /// New dialogue text after advancing (empty string if conversation transitioned to another screen)
                text = cm.CurrentSentenceText ?? string.Empty,
                /// Whether dialogue options are now available
                hasOptions = cm.CurOptions?.Count > 0,
            };
        });
    }

    [Tool("conversation/start", Description = "Start a conversation with a hero at the current settlement. Opens a map conversation without entering a 3D scene. The hero must be present in the same settlement as the player. Returns immediately — use conversation/wait_for_state to block until dialogue appears.")]
    public partial Task<object> StartConversation(
        [ToolParameter(Description = "Hero name or string ID to talk to")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var cm = Campaign.Current.ConversationManager;
            if (cm == null)
                return new { error = "No conversation manager available" };

            if (cm.IsConversationInProgress)
                return new { error = "A conversation is already in progress" };

            var hero = Hero.FindFirst(h => h.StringId == nameOrId)
                       ?? Hero.FindFirst(h => h.Name?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (hero == null)
                return new { error = $"Hero not found: {nameOrId}" };

            // Check the hero is at the same settlement
            var playerSettlement = Settlement.CurrentSettlement;
            if (playerSettlement == null)
                return new { error = "Player is not in a settlement. Use this tool when inside a town, castle, or village." };

            if (hero.CurrentSettlement != playerSettlement)
                return new { error = $"{hero.Name} is not at {playerSettlement.Name}. They are at {hero.CurrentSettlement?.Name?.ToString() ?? "unknown location"}." };

            var playerData = new ConversationCharacterData(CharacterObject.PlayerCharacter, null, false, false, false, true);
            var npcData = new ConversationCharacterData(hero.CharacterObject, null, false, false, false, true);

            cm.OpenMapConversation(playerData, npcData);

            return new
            {
                /// Name of the hero being talked to
                hero = hero.Name?.ToString(),
                /// Status message
                message = $"Started conversation with {hero.Name}",
            };
        });
    }

    [Tool("conversation/wait_for_state", Description = "Wait until a conversation becomes active with dialogue options or text. Use after conversation/start or mission/talk_to_agent. Pass timeout via games.call_tool timeout parameter.")]
    public partial Task<object> WaitForConversationState(
        [ToolParameter(Description = "Poll interval in milliseconds (default 500).", Required = false)] int pollIntervalMs = 500)
    {
        if (pollIntervalMs < 100) pollIntervalMs = 100;
        if (pollIntervalMs > 5000) pollIntervalMs = 5000;

        return Task.Run<object>(async () =>
        {
            var startTime = DateTime.UtcNow;
            var maxWait = TimeSpan.FromSeconds(30);

            while (DateTime.UtcNow - startTime < maxWait)
            {
                var state = await MainThreadDispatcher.EnqueueAsync(() =>
                {
                    if (Campaign.Current == null)
                        return (active: false, speaker: (string?) null, text: (string?) null, optionCount: 0);

                    var cm = Campaign.Current.ConversationManager;
                    if (cm == null || !cm.IsConversationInProgress)
                        return (active: false, speaker: (string?) null, text: (string?) null, optionCount: 0);

                    var speaker = cm.OneToOneConversationHero;
                    return (
                        active: true,
                        speaker: speaker?.Name?.ToString(),
                        text: cm.CurrentSentenceText ?? string.Empty,
                        optionCount: cm.CurOptions?.Count ?? 0
                    );
                });

                if (state.active)
                {
                    return new
                    {
                        /// Whether a conversation is now active
                        isActive = true,
                        /// Name of the NPC speaking
                        speaker = state.speaker,
                        /// Current dialogue text
                        text = state.text,
                        /// Number of available dialogue options
                        optionCount = state.optionCount,
                        /// Milliseconds spent waiting
                        waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
                    };
                }

                await Task.Delay(pollIntervalMs);
            }

            return new
            {
                /// Whether a conversation is now active
                isActive = false,
                /// Name of the NPC speaking
                speaker = (string?) null,
                /// Current dialogue text
                text = (string?) null,
                /// Number of available dialogue options
                optionCount = 0,
                /// Milliseconds spent waiting
                waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
            };
        });
    }

    [Tool("conversation/get_persuasion", Description = "Get the current persuasion minigame state if active.")]
    public partial Task<object> GetPersuasion()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var cm = Campaign.Current.ConversationManager;
            if (cm is not { IsConversationInProgress: true })
                return new { error = "No conversation in progress" };

            if (!ConversationManager.GetPersuasionIsActive())
                return new
                {
                    /// Whether a persuasion minigame is active
                    isActive = false,
                    /// Current persuasion progress
                    progress = 0.0,
                    /// Persuasion goal value
                    goal = 0.0,
                    /// Array of persuasion option objects
                    options = (object?) null,
                };

            var progress = ConversationManager.GetPersuasionProgress();
            var goalValue = ConversationManager.GetPersuasionGoalValue();

            // Get persuasion options with success chances
            var persuasionOptions = cm.CurOptions?
                .Select((o, i) =>
                {
                    double? successRate = null;
                    double? critSuccessRate = null;
                    double? critFailRate = null;
                    double? failRate = null;

                    if (o.HasPersuasion)
                    {
                        cm.GetPersuasionChances(o, out var sc, out var csc, out var cfc, out var fc);
                        successRate = Math.Round(sc * 100, 1);
                        critSuccessRate = Math.Round(csc * 100, 1);
                        critFailRate = Math.Round(cfc * 100, 1);
                        failRate = Math.Round(fc * 100, 1);
                    }

                    return new
                    {
                        index = i,
                        text = o.Text?.ToString(),
                        skillName = o.SkillName,
                        traitName = o.TraitName,
                        isClickable = o.IsClickable,
                        hasPersuasion = o.HasPersuasion,
                        successChance = successRate,
                        critSuccessChance = critSuccessRate,
                        critFailChance = critFailRate,
                        failChance = failRate,
                    };
                })
                .ToList();

            return new
            {
                /// Whether a persuasion minigame is active
                isActive = true,
                /// Current persuasion progress
                progress = Math.Round(progress, 2),
                /// Persuasion goal value
                goal = Math.Round(goalValue, 2),
                /// Array of persuasion option objects
                options = persuasionOptions,
            };
        });
    }
}