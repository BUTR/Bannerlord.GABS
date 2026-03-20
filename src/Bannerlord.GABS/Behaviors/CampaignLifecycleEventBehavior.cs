using Lib.GAB.Events;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles campaign lifecycle events: daily tick, save load, game over, and party starvation.
/// </summary>
public class CampaignLifecycleEventBehavior : BridgeEventBehaviorBase
{
    public CampaignLifecycleEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/day_passed", "New day notification");
        Events.RegisterChannel("campaign/loaded", "Campaign save loaded and ready");
        Events.RegisterChannel("campaign/game_over", "Game ended (player died with no heir, etc.)");
        Events.RegisterChannel("campaign/party_starving", "Player party has no food");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
        CampaignEvents.OnGameOverEvent.AddNonSerializedListener(this, OnGameOver);
        CampaignEvents.OnMainPartyStarvingEvent.AddNonSerializedListener(this, OnMainPartyStarving);
    }

    private void OnDailyTick()
    {
        var now = CampaignTime.Now;
        Emit("campaign/day_passed", new
        {
            year = (int) now.GetYear,
            season = now.GetSeasonOfYear.ToString(),
            dayOfSeason = now.GetDayOfSeason,
            hourOfDay = now.GetHourOfDay,
        });
    }

    private void OnGameLoadFinished()
    {
        Emit("campaign/loaded", new
        {
            campaignTime = CampaignTime.Now.ToString(),
            playerName = Hero.MainHero?.Name?.ToString(),
            playerSettlement = MobileParty.MainParty?.CurrentSettlement?.Name?.ToString(),
        });
    }

    private void OnGameOver()
    {
        Emit("campaign/game_over", new
        {
            campaignTime = CampaignTime.Now.ToString(),
            playerName = Hero.MainHero?.Name?.ToString(),
        });
    }

    private void OnMainPartyStarving()
    {
        Emit("campaign/party_starving", new
        {
            partySize = MobileParty.MainParty?.MemberRoster?.TotalManCount ?? 0,
            morale = Math.Round(MobileParty.MainParty?.Morale ?? 0, 1),
        });
    }
}