using Lib.GAB.Events;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS.Behaviors;

public abstract class BridgeEventBehaviorBase : CampaignBehaviorBase
{
    protected readonly IEventManager Events;

    protected BridgeEventBehaviorBase(IEventManager events)
    {
        Events = events;
    }

    public override void SyncData(IDataStore dataStore) { }

    protected void Emit(string channel, object payload)
    {
        Events.EmitEventAsync(channel, payload);
    }

    public abstract void RegisterChannels();
}