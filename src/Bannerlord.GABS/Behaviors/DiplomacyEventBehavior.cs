using Lib.GAB.Events;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles diplomacy and politics events: war, peace, alliances, kingdoms, clan changes.
/// </summary>
public class DiplomacyEventBehavior : BridgeEventBehaviorBase
{
    public DiplomacyEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        // Diplomacy
        Events.RegisterChannel("campaign/war_declared", "War declared between factions");
        Events.RegisterChannel("campaign/peace_made", "Peace made between factions");
        Events.RegisterChannel("campaign/alliance_started", "Two kingdoms formed alliance");
        Events.RegisterChannel("campaign/alliance_ended", "Alliance between kingdoms broken");

        // Kingdoms & politics
        Events.RegisterChannel("campaign/kingdom_created", "New kingdom formed");
        Events.RegisterChannel("campaign/kingdom_destroyed", "Kingdom eliminated");
        Events.RegisterChannel("campaign/kingdom_decision_added", "New vote/proposal in kingdom");
        Events.RegisterChannel("campaign/kingdom_decision_concluded", "Vote finished, outcome decided");
        Events.RegisterChannel("campaign/clan_changed_kingdom", "Clan joined/left a kingdom");
        Events.RegisterChannel("campaign/clan_leader_changed", "Clan leadership transferred");
        Events.RegisterChannel("campaign/clan_defected", "Clan defected to another kingdom");
        Events.RegisterChannel("campaign/ruling_clan_changed", "Kingdom ruler changed");

        // Mercenary & vassal
        Events.RegisterChannel("campaign/mercenary_service_started", "Clan entered mercenary service");
        Events.RegisterChannel("campaign/mercenary_service_ended", "Clan left mercenary service");

        // Offers & proposals
        Events.RegisterChannel("campaign/peace_offered", "Enemy faction offers peace terms");
        Events.RegisterChannel("campaign/marriage_offered", "Marriage proposal received");
        Events.RegisterChannel("campaign/ransom_offered", "Ransom offer for captured hero");
        Events.RegisterChannel("campaign/service_offered", "Kingdom offers mercenary/vassal contract");
    }

    public override void RegisterEvents()
    {
        // Diplomacy
        CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
        CampaignEvents.MakePeace.AddNonSerializedListener(this, OnPeaceMade);
#if v1313 || v1315
            CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnAllianceStarted);
            CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnAllianceEnded);
#endif

        // Kingdoms & politics
        CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, OnKingdomCreated);
        CampaignEvents.KingdomDestroyedEvent.AddNonSerializedListener(this, OnKingdomDestroyed);
        CampaignEvents.KingdomDecisionAdded.AddNonSerializedListener(this, OnKingdomDecisionAdded);
        CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
        CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
        CampaignEvents.OnClanLeaderChangedEvent.AddNonSerializedListener(this, OnClanLeaderChanged);
#if v1313 || v1315
            CampaignEvents.OnClanDefectedEvent.AddNonSerializedListener(this, OnClanDefected);
#endif
        CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);

#if v1313 || v1315
            // Mercenary & vassal (v1.3.x+)
            CampaignEvents.OnMercenaryServiceStartedEvent.AddNonSerializedListener(this, OnMercenaryServiceStarted);
            CampaignEvents.OnMercenaryServiceEndedEvent.AddNonSerializedListener(this, OnMercenaryServiceEnded);
#endif

        // Offers & proposals
#if v1313 || v1315
            CampaignEvents.OnPeaceOfferedToPlayerEvent.AddNonSerializedListener(this, OnPeaceOffered);
#endif
        CampaignEvents.OnMarriageOfferedToPlayerEvent.AddNonSerializedListener(this, OnMarriageOffered);
        CampaignEvents.OnRansomOfferedToPlayerEvent.AddNonSerializedListener(this, OnRansomOffered);
        CampaignEvents.OnVassalOrMercenaryServiceOfferedToPlayerEvent.AddNonSerializedListener(this, OnServiceOffered);
    }

    private void OnWarDeclared(IFaction? faction1, IFaction? faction2, DeclareWarAction.DeclareWarDetail detail)
    {
        Emit("campaign/war_declared", new
        {
            faction1 = faction1?.Name?.ToString(),
            faction2 = faction2?.Name?.ToString(),
            reason = detail.ToString(),
            involvesPlayer = faction1 == Clan.PlayerClan?.MapFaction || faction2 == Clan.PlayerClan?.MapFaction,
        });
    }

    private void OnPeaceMade(IFaction? faction1, IFaction? faction2, MakePeaceAction.MakePeaceDetail detail)
    {
        Emit("campaign/peace_made", new
        {
            faction1 = faction1?.Name?.ToString(),
            faction2 = faction2?.Name?.ToString(),
            reason = detail.ToString(),
            involvesPlayer = faction1 == Clan.PlayerClan?.MapFaction || faction2 == Clan.PlayerClan?.MapFaction,
        });
    }

#if v1313 || v1315
        private void OnAllianceStarted(Kingdom? kingdom1, Kingdom? kingdom2)
        {
            Emit("campaign/alliance_started", new
            {
                kingdom1 = kingdom1?.Name?.ToString(),
                kingdom2 = kingdom2?.Name?.ToString(),
                involvesPlayer = kingdom1 == Clan.PlayerClan?.Kingdom || kingdom2 == Clan.PlayerClan?.Kingdom,
            });
        }

        private void OnAllianceEnded(Kingdom? kingdom1, Kingdom? kingdom2)
        {
            Emit("campaign/alliance_ended", new
            {
                kingdom1 = kingdom1?.Name?.ToString(),
                kingdom2 = kingdom2?.Name?.ToString(),
                involvesPlayer = kingdom1 == Clan.PlayerClan?.Kingdom || kingdom2 == Clan.PlayerClan?.Kingdom,
            });
        }
#endif

    private void OnKingdomCreated(Kingdom? kingdom)
    {
        Emit("campaign/kingdom_created", new
        {
            kingdom = kingdom?.Name?.ToString(),
            leader = kingdom?.Leader?.Name?.ToString(),
            culture = kingdom?.Culture?.Name?.ToString(),
        });
    }

    private void OnKingdomDestroyed(Kingdom? kingdom)
    {
        Emit("campaign/kingdom_destroyed", new
        {
            kingdom = kingdom?.Name?.ToString(),
        });
    }

    private void OnKingdomDecisionAdded(KingdomDecision? decision, bool isPlayerInvolved)
    {
        Emit("campaign/kingdom_decision_added", new
        {
            kingdom = decision?.Kingdom?.Name?.ToString(),
            type = decision?.GetType().Name,
            description = decision?.GetGeneralTitle()?.ToString(),
            proposer = decision?.ProposerClan?.Name?.ToString(),
            isPlayerInvolved,
        });
    }

    private void OnKingdomDecisionConcluded(KingdomDecision? decision, DecisionOutcome? chosenOutcome, bool isPlayerInvolved)
    {
        Emit("campaign/kingdom_decision_concluded", new
        {
            kingdom = decision?.Kingdom?.Name?.ToString(),
            type = decision?.GetType().Name,
            description = decision?.GetGeneralTitle()?.ToString(),
            outcome = chosenOutcome?.ToString(),
            isPlayerInvolved,
        });
    }

    private void OnClanChangedKingdom(Clan? clan, Kingdom? oldKingdom, Kingdom? newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
    {
        Emit("campaign/clan_changed_kingdom", new
        {
            clan = clan?.Name?.ToString(),
            oldKingdom = oldKingdom?.Name?.ToString(),
            newKingdom = newKingdom?.Name?.ToString(),
            reason = detail.ToString(),
            isPlayer = clan == Clan.PlayerClan,
        });
    }

    private void OnClanLeaderChanged(Hero? oldLeader, Hero? newLeader)
    {
        Emit("campaign/clan_leader_changed", new
        {
            clan = newLeader?.Clan?.Name?.ToString(),
            oldLeader = oldLeader?.Name?.ToString(),
            newLeader = newLeader?.Name?.ToString(),
            isPlayerClan = newLeader?.Clan == Clan.PlayerClan,
        });
    }

#if v1313 || v1315
        private void OnClanDefected(Clan? clan, Kingdom? oldKingdom, Kingdom? newKingdom)
        {
            Emit("campaign/clan_defected", new
            {
                clan = clan?.Name?.ToString(),
                oldKingdom = oldKingdom?.Name?.ToString(),
                newKingdom = newKingdom?.Name?.ToString(),
            });
        }
#endif

    private void OnRulingClanChanged(Kingdom? kingdom, Clan? newRulingClan)
    {
        Emit("campaign/ruling_clan_changed", new
        {
            kingdom = kingdom?.Name?.ToString(),
            newRuler = newRulingClan?.Leader?.Name?.ToString(),
            newRulingClan = newRulingClan?.Name?.ToString(),
        });
    }

#if v1313 || v1315
        private void OnMercenaryServiceStarted(Clan? mercenaryClan, StartMercenaryServiceAction.StartMercenaryServiceActionDetails details)
        {
            Emit("campaign/mercenary_service_started", new
            {
                clan = mercenaryClan?.Name?.ToString(),
                kingdom = mercenaryClan?.Kingdom?.Name?.ToString(),
                isPlayer = mercenaryClan == Clan.PlayerClan,
            });
        }

        private void OnMercenaryServiceEnded(Clan? mercenaryClan, EndMercenaryServiceAction.EndMercenaryServiceActionDetails details)
        {
            Emit("campaign/mercenary_service_ended", new
            {
                clan = mercenaryClan?.Name?.ToString(),
                reason = details.ToString(),
                isPlayer = mercenaryClan == Clan.PlayerClan,
            });
        }
#endif

    // --- Offers & Proposals ---

#if v1313 || v1315
        private void OnPeaceOffered(IFaction? opponentFaction, int tributeAmount, int tributeDurationInDays)
        {
            Emit("campaign/peace_offered", new
            {
                faction = opponentFaction?.Name?.ToString(),
                tributeAmount,
                tributeDurationInDays,
            });
        }
#endif

    private void OnMarriageOffered(Hero? suitor, Hero? maiden)
    {
        Emit("campaign/marriage_offered", new
        {
            suitor = suitor?.Name?.ToString(),
            suitorClan = suitor?.Clan?.Name?.ToString(),
            maiden = maiden?.Name?.ToString(),
            maidenClan = maiden?.Clan?.Name?.ToString(),
        });
    }

    private void OnRansomOffered(Hero? captiveHero)
    {
        Emit("campaign/ransom_offered", new
        {
            captive = captiveHero?.Name?.ToString(),
            captiveClan = captiveHero?.Clan?.Name?.ToString(),
        });
    }

    private void OnServiceOffered(Kingdom? offeredKingdom)
    {
        Emit("campaign/service_offered", new
        {
            kingdom = offeredKingdom?.Name?.ToString(),
            leader = offeredKingdom?.Leader?.Name?.ToString(),
        });
    }
}