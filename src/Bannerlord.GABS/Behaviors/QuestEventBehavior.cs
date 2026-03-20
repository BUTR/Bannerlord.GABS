using Lib.GAB.Events;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles quest events: quest start and completion.
/// </summary>
public class QuestEventBehavior : BridgeEventBehaviorBase
{
    public QuestEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/quest_started", "New quest available");
        Events.RegisterChannel("campaign/quest_completed", "Quest completed");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
        CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
    }

    private void OnQuestStarted(QuestBase? quest)
    {
        Emit("campaign/quest_started", new
        {
            title = quest?.Title?.ToString(),
            id = quest?.StringId,
            giver = quest?.QuestGiver?.Name?.ToString(),
        });
    }

    private void OnQuestCompleted(QuestBase? quest, QuestBase.QuestCompleteDetails detail)
    {
        Emit("campaign/quest_completed", new
        {
            title = quest?.Title?.ToString(),
            id = quest?.StringId,
            result = detail.ToString(),
        });
    }
}