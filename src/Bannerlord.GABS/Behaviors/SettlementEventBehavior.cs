using Lib.GAB.Events;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles settlement events: ownership, governors, raids, rebellions, crime.
/// </summary>
public class SettlementEventBehavior : BridgeEventBehaviorBase
{
    public SettlementEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/settlement_owner_changed", "Settlement captured/granted");
        Events.RegisterChannel("campaign/governor_changed", "Settlement governor changed");
        Events.RegisterChannel("campaign/village_being_raided", "Village raid in progress");
        Events.RegisterChannel("campaign/village_looted", "Village fully looted");
        Events.RegisterChannel("campaign/town_rebellion_state", "Town entering/leaving rebellious state");
        Events.RegisterChannel("campaign/rebellion_finished", "Rebellion concluded");
        Events.RegisterChannel("campaign/crime_rating_changed", "Player's crime rating changed");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.OnGovernorChangedEvent.AddNonSerializedListener(this, OnGovernorChanged);
        CampaignEvents.VillageBeingRaided.AddNonSerializedListener(this, OnVillageBeingRaided);
        CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
        CampaignEvents.TownRebelliosStateChanged.AddNonSerializedListener(this, OnTownRebellionStateChanged);
        CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
        CampaignEvents.CrimeRatingChanged.AddNonSerializedListener(this, OnCrimeRatingChanged);
    }

    private void OnSettlementOwnerChanged(Settlement? settlement, bool openToClaim, Hero? newOwner, Hero? oldOwner, Hero? capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        Emit("campaign/settlement_owner_changed", new
        {
            settlement = settlement?.Name?.ToString(),
            newOwner = newOwner?.Name?.ToString(),
            oldOwner = oldOwner?.Name?.ToString(),
            capturer = capturerHero?.Name?.ToString(),
            reason = detail.ToString(),
        });
    }

    private void OnGovernorChanged(Town? fortification, Hero? oldGovernor, Hero? newGovernor)
    {
        Emit("campaign/governor_changed", new
        {
            settlement = fortification?.Name?.ToString(),
            oldGovernor = oldGovernor?.Name?.ToString(),
            newGovernor = newGovernor?.Name?.ToString(),
        });
    }

    private void OnVillageBeingRaided(Village? village)
    {
        Emit("campaign/village_being_raided", new
        {
            village = village?.Name?.ToString(),
            boundTown = village?.Bound?.Name?.ToString(),
            owner = village?.Owner?.Name?.ToString(),
        });
    }

    private void OnVillageLooted(Village? village)
    {
        Emit("campaign/village_looted", new
        {
            village = village?.Name?.ToString(),
            boundTown = village?.Bound?.Name?.ToString(),
            owner = village?.Owner?.Name?.ToString(),
        });
    }

    private void OnTownRebellionStateChanged(Town? town, bool rebelliousState)
    {
        Emit("campaign/town_rebellion_state", new
        {
            town = town?.Name?.ToString(),
            isRebellious = rebelliousState,
            owner = town?.OwnerClan?.Name?.ToString(),
        });
    }

    private void OnRebellionFinished(Settlement? settlement, Clan? oldOwnerClan)
    {
        Emit("campaign/rebellion_finished", new
        {
            settlement = settlement?.Name?.ToString(),
            oldOwner = oldOwnerClan?.Name?.ToString(),
            newOwner = settlement?.OwnerClan?.Name?.ToString(),
        });
    }

    private void OnCrimeRatingChanged(IFaction? kingdom, float deltaCrimeAmount)
    {
        if (Math.Abs(deltaCrimeAmount) < 1f) return;

        Emit("campaign/crime_rating_changed", new
        {
            kingdom = kingdom?.Name?.ToString(),
            change = Math.Round(deltaCrimeAmount, 1),
            totalCrime = Math.Round(kingdom?.MainHeroCrimeRating ?? 0, 1),
        });
    }
}