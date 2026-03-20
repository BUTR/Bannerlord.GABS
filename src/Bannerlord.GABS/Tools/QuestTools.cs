// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS.Tools;

public partial class QuestTools
{
    [Tool("quest/list_quests", Description = "List active quests with title, giver, and deadline.")]
    public partial Task<object> ListQuests()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var quests = Campaign.Current.QuestManager?.Quests?
                .Where(q => q.IsOngoing)
                .Select(q => new
                {
                    id = q.StringId,
                    title = q.Title?.ToString(),
                    giver = q.QuestGiver?.Name?.ToString(),
                    isSpecialQuest = q.IsSpecialQuest,
                    dueDate = q.QuestDueTime.ToString(),
                })
                .ToList();

            return new
            {
                /// Number of active quests
                count = quests?.Count ?? 0,
                /// Array of quest objects with id, title, giver, isSpecialQuest, dueDate
                quests,
            };
        });
    }

    [Tool("quest/get_quest", Description = "Get detailed info about a specific quest by title or string ID.")]
    public partial Task<object> GetQuest(
        [ToolParameter(Description = "Quest title or string ID")] string nameOrId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var quest = Campaign.Current.QuestManager?.Quests?
                .FirstOrDefault(q => q.StringId == nameOrId
                                     || q.Title?.ToString().Equals(nameOrId, StringComparison.OrdinalIgnoreCase) == true);

            if (quest == null)
                return new { error = $"Quest not found: {nameOrId}" };

            return new
            {
                /// Quest string ID
                id = quest.StringId,
                /// Quest title
                title = quest.Title?.ToString(),
                /// Quest giver hero name
                giver = quest.QuestGiver?.Name?.ToString(),
                /// Settlement where the quest giver is located
                giverSettlement = quest.QuestGiver?.CurrentSettlement?.Name?.ToString(),
                /// Whether the quest is still active
                isOngoing = quest.IsOngoing,
                /// Whether this is a special quest
                isSpecialQuest = quest.IsSpecialQuest,
                /// Quest due date string
                dueDate = quest.QuestDueTime.ToString(),
                /// Array of journal entries with text and isCompleted
                journalEntries = quest.JournalEntries?
                    .Where(j => j != null)
                    .Select(j => new
                    {
                        text = j.LogText?.ToString(),
                        isCompleted = j.HasBeenCompleted(),
                    })
                    .ToList(),
            };
        });
    }
}