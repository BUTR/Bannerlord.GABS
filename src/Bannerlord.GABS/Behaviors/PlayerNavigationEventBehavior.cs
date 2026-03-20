using Lib.GAB.Events;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles player movement, arrival detection, pursuit/threat detection, settlement enter/leave, and mission transitions.
/// </summary>
public class PlayerNavigationEventBehavior : BridgeEventBehaviorBase
{
    private bool _wasMoving;
    private readonly HashSet<string> _activePursuers = new HashSet<string>();

    public PlayerNavigationEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/settlement_entered", "Party entered a settlement");
        Events.RegisterChannel("campaign/settlement_left", "Party left a settlement");
        Events.RegisterChannel("campaign/party_arrived", "Player party arrived at destination (stopped moving)");
        Events.RegisterChannel("campaign/party_pursuing_player", "A hostile party started chasing the player (game auto-paused)");
        Events.RegisterChannel("campaign/party_stopped_pursuing", "A hostile party stopped chasing the player");
        Events.RegisterChannel("campaign/mission_started", "Entered a 3D scene (town, battle, etc.)");
        Events.RegisterChannel("campaign/mission_ended", "Left a 3D scene");
        Events.RegisterChannel("campaign/hideout_spotted", "Bandit hideout discovered");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
        CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
        CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, OnHideoutSpotted);
    }

    private void OnSettlementEntered(MobileParty? party, Settlement? settlement, Hero? hero)
    {
        Emit("campaign/settlement_entered", new
        {
            party = party?.Name?.ToString(),
            hero = hero?.Name?.ToString(),
            settlement = settlement?.Name?.ToString(),
            settlementType = settlement?.IsTown == true ? "town" : settlement?.IsCastle == true ? "castle" : "village",
            isPlayer = party?.IsMainParty == true || hero == Hero.MainHero,
        });
    }

    private void OnSettlementLeft(MobileParty? party, Settlement? settlement)
    {
        Emit("campaign/settlement_left", new
        {
            party = party?.Name?.ToString(),
            leader = party?.LeaderHero?.Name?.ToString(),
            settlement = settlement?.Name?.ToString(),
            isPlayer = party?.IsMainParty == true,
        });
    }

    private void OnMissionStarted(IMission? mission)
    {
        Emit("campaign/mission_started", new
        {
            scene = TaleWorlds.MountAndBlade.Mission.Current?.SceneName,
        });
    }

    private void OnMissionEnded(IMission? mission)
    {
        Emit("campaign/mission_ended", new
        {
            scene = TaleWorlds.MountAndBlade.Mission.Current?.SceneName,
        });
    }

    private void OnHideoutSpotted(PartyBase? party, PartyBase? hideoutParty)
    {
        Emit("campaign/hideout_spotted", new
        {
            discoveredBy = party?.Name?.ToString(),
            hideout = hideoutParty?.Name?.ToString(),
            isPlayer = party?.MobileParty?.IsMainParty == true,
        });
    }

    private void OnTick(float dt)
    {
        var party = MobileParty.MainParty;
        if (party == null) return;

        var isMoving = party.IsMoving;
        if (_wasMoving && !isMoving)
        {
            Emit("campaign/party_arrived", new
            {
                settlement = party.CurrentSettlement?.Name?.ToString(),
                positionX = Math.Round(party.GetPosition2D.X, 2),
                positionY = Math.Round(party.GetPosition2D.Y, 2),
            });
        }
        _wasMoving = isMoving;

        // Pursuit detection: check if any hostile party is engaging the player
        CheckForPursuers(party);
    }

    private void CheckForPursuers(MobileParty playerParty)
    {
        var currentPursuers = new HashSet<string>();

        foreach (var party in MobileParty.All)
        {
            if (party is not { IsActive: true } || party == playerParty) continue;
            if (party.CurrentSettlement != null) continue;
            var isEngaging = party.ShortTermBehavior == AiBehavior.EngageParty ||
                             party.DefaultBehavior == AiBehavior.EngageParty;
            if (!isEngaging) continue;
            var isTargetingPlayer = party.ShortTermTargetParty == playerParty;
            if (!isTargetingPlayer) continue;

            var id = party.StringId;
            currentPursuers.Add(id);

            if (!_activePursuers.Contains(id))
            {
                var dx = party.GetPosition2D.X - playerParty.GetPosition2D.X;
                var dy = party.GetPosition2D.Y - playerParty.GetPosition2D.Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var payload = new
                {
                    partyId = id,
                    partyName = party.Name?.ToString(),
                    leader = party.LeaderHero?.Name?.ToString(),
                    faction = party.MapFaction?.Name?.ToString(),
                    troopCount = party.MemberRoster?.TotalManCount ?? 0,
                    speed = Math.Round(party.Speed, 2),
                    distance = Math.Round(distance, 2),
                    positionX = Math.Round(party.GetPosition2D.X, 2),
                    positionY = Math.Round(party.GetPosition2D.Y, 2),
                    playerSpeed = Math.Round(playerParty.Speed, 2),
                    canOutrun = playerParty.Speed > party.Speed,
                };

                Emit("campaign/party_pursuing_player", payload);
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }

        foreach (var oldId in _activePursuers)
        {
            if (!currentPursuers.Contains(oldId))
            {
                var party = MobileParty.All.FirstOrDefault(p => p.StringId == oldId);
                Emit("campaign/party_stopped_pursuing", new
                {
                    partyId = oldId,
                    partyName = party?.Name?.ToString(),
                    reason = party == null ? "destroyed" :
                        !party.IsActive ? "inactive" :
                        party.DefaultBehavior.ToString(),
                });
            }
        }

        _activePursuers.Clear();
        foreach (var id in currentPursuers)
            _activePursuers.Add(id);
    }
}