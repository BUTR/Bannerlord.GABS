using Lib.GAB.Events;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Siege;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles military events: armies, sieges, and party destruction.
/// </summary>
public class MilitaryEventBehavior : BridgeEventBehaviorBase
{
    public MilitaryEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/army_created", "Army formed");
        Events.RegisterChannel("campaign/army_dispersed", "Army disbanded");
        Events.RegisterChannel("campaign/party_joined_army", "Party joined an army");
        Events.RegisterChannel("campaign/party_left_army", "Party left an army");
        Events.RegisterChannel("campaign/siege_started", "Siege began");
        Events.RegisterChannel("campaign/siege_ended", "Siege concluded");
        Events.RegisterChannel("campaign/player_siege_started", "Player initiated a siege");
        Events.RegisterChannel("campaign/party_destroyed", "A mobile party destroyed");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
        CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
        CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
#if v1313 || v1315
            CampaignEvents.OnPartyLeftArmyEvent.AddNonSerializedListener(this, OnPartyLeftArmy);
#endif
        CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeStarted);
        CampaignEvents.OnSiegeEventEndedEvent.AddNonSerializedListener(this, OnSiegeEnded);
        CampaignEvents.OnPlayerSiegeStartedEvent.AddNonSerializedListener(this, OnPlayerSiegeStarted);
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
    }

    private void OnArmyCreated(Army? army)
    {
        Emit("campaign/army_created", new
        {
            leader = army?.LeaderParty?.LeaderHero?.Name?.ToString(),
            faction = army?.Kingdom?.Name?.ToString(),
            partyCount = army?.Parties?.Count ?? 0,
        });
    }

    private void OnArmyDispersed(Army? army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
    {
        Emit("campaign/army_dispersed", new
        {
            leader = army?.LeaderParty?.LeaderHero?.Name?.ToString(),
            faction = army?.Kingdom?.Name?.ToString(),
            reason = reason.ToString(),
            isPlayersArmy,
        });
    }

    private void OnPartyJoinedArmy(MobileParty? mobileParty)
    {
        Emit("campaign/party_joined_army", new
        {
            party = mobileParty?.Name?.ToString(),
            leader = mobileParty?.LeaderHero?.Name?.ToString(),
            army = mobileParty?.Army?.LeaderParty?.LeaderHero?.Name?.ToString(),
            isPlayer = mobileParty?.IsMainParty == true,
        });
    }

#if v1313 || v1315
        private void OnPartyLeftArmy(MobileParty? party, Army? army)
        {
            Emit("campaign/party_left_army", new
            {
                party = party?.Name?.ToString(),
                leader = party?.LeaderHero?.Name?.ToString(),
                army = army?.LeaderParty?.LeaderHero?.Name?.ToString(),
                isPlayer = party?.IsMainParty == true,
            });
        }
#endif

    private void OnSiegeStarted(SiegeEvent? siegeEvent)
    {
        Emit("campaign/siege_started", new
        {
            settlement = siegeEvent?.BesiegedSettlement?.Name?.ToString(),
            attacker = siegeEvent?.BesiegerCamp?.LeaderParty?.LeaderHero?.Name?.ToString(),
            attackerFaction = siegeEvent?.BesiegerCamp?.LeaderParty?.MapFaction?.Name?.ToString(),
        });
    }

    private void OnSiegeEnded(SiegeEvent? siegeEvent)
    {
        Emit("campaign/siege_ended", new
        {
            settlement = siegeEvent?.BesiegedSettlement?.Name?.ToString(),
            attacker = siegeEvent?.BesiegerCamp?.LeaderParty?.LeaderHero?.Name?.ToString(),
        });
    }

    private void OnPlayerSiegeStarted()
    {
        Emit("campaign/player_siege_started", new
        {
            settlement = MobileParty.MainParty?.BesiegedSettlement?.Name?.ToString(),
        });
    }

    private void OnPartyDestroyed(MobileParty? party, PartyBase? destroyer)
    {
        Emit("campaign/party_destroyed", new
        {
            party = party?.Name?.ToString(),
            leader = party?.LeaderHero?.Name?.ToString(),
            faction = party?.MapFaction?.Name?.ToString(),
            destroyer = destroyer?.Name?.ToString(),
            destroyerFaction = destroyer?.MapFaction?.Name?.ToString(),
        });
    }
}