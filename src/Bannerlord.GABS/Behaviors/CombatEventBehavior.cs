using Lib.GAB.Events;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.GABS.Behaviors;

/// <summary>
/// Handles combat events: battle start/end, character defeats.
/// </summary>
public class CombatEventBehavior : BridgeEventBehaviorBase
{
    public CombatEventBehavior(IEventManager events) : base(events) { }

    public override void RegisterChannels()
    {
        Events.RegisterChannel("campaign/battle_started", "Battle/raid/siege assault began on campaign map");
        Events.RegisterChannel("campaign/battle_result", "Battle/skirmish outcome");
        Events.RegisterChannel("campaign/player_battle_ended", "Player's battle finished");
        Events.RegisterChannel("campaign/character_defeated", "Hero defeated another in battle");

        // Tournaments
        Events.RegisterChannel("campaign/tournament_joined", "Player entered a tournament");
        Events.RegisterChannel("campaign/tournament_finished", "Tournament concluded");
        Events.RegisterChannel("campaign/tournament_eliminated", "Player knocked out of tournament");
    }

    public override void RegisterEvents()
    {
        CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
        CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
        CampaignEvents.CharacterDefeated.AddNonSerializedListener(this, OnCharacterDefeated);

        // Tournaments
        CampaignEvents.OnPlayerJoinedTournamentEvent.AddNonSerializedListener(this, OnTournamentJoined);
        CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
        CampaignEvents.PlayerEliminatedFromTournament.AddNonSerializedListener(this, OnTournamentEliminated);
    }

    private void OnMapEventStarted(MapEvent? mapEvent, PartyBase? attackerParty, PartyBase? defenderParty)
    {
        Emit("campaign/battle_started", new
        {
            type = mapEvent?.EventType.ToString(),
            attacker = attackerParty?.Name?.ToString(),
            attackerFaction = attackerParty?.MapFaction?.Name?.ToString(),
            defender = defenderParty?.Name?.ToString(),
            defenderFaction = defenderParty?.MapFaction?.Name?.ToString(),
            settlement = mapEvent?.MapEventSettlement?.Name?.ToString(),
            involvesPlayer = attackerParty?.MobileParty?.IsMainParty == true || defenderParty?.MobileParty?.IsMainParty == true,
        });
    }

    private void OnMapEventEnded(MapEvent? mapEvent)
    {
        if (mapEvent == null) return;

        Emit("campaign/battle_result", new
        {
            type = mapEvent.EventType.ToString(),
            winner = mapEvent.WinningSide.ToString(),
            attackerLeader = mapEvent.AttackerSide?.LeaderParty?.Name?.ToString(),
            defenderLeader = mapEvent.DefenderSide?.LeaderParty?.Name?.ToString(),
#if v1313 || v1315
                attackerCasualties = mapEvent.AttackerSide?.TroopCasualties ?? 0,
                defenderCasualties = mapEvent.DefenderSide?.TroopCasualties ?? 0,
#else
            attackerCasualties = mapEvent.AttackerSide?.Casualties ?? 0,
            defenderCasualties = mapEvent.DefenderSide?.Casualties ?? 0,
#endif
            settlement = mapEvent.MapEventSettlement?.Name?.ToString(),
        });
    }

    private void OnPlayerBattleEnd(MapEvent? mapEvent)
    {
        if (mapEvent == null) return;

        Emit("campaign/player_battle_ended", new
        {
            type = mapEvent.EventType.ToString(),
            winner = mapEvent.WinningSide.ToString(),
            attackerLeader = mapEvent.AttackerSide?.LeaderParty?.Name?.ToString(),
            defenderLeader = mapEvent.DefenderSide?.LeaderParty?.Name?.ToString(),
#if v1313 || v1315
                attackerCasualties = mapEvent.AttackerSide?.TroopCasualties ?? 0,
                defenderCasualties = mapEvent.DefenderSide?.TroopCasualties ?? 0,
#else
            attackerCasualties = mapEvent.AttackerSide?.Casualties ?? 0,
            defenderCasualties = mapEvent.DefenderSide?.Casualties ?? 0,
#endif
            settlement = mapEvent.MapEventSettlement?.Name?.ToString(),
        });
    }

    private void OnCharacterDefeated(Hero? winner, Hero? loser)
    {
        Emit("campaign/character_defeated", new
        {
            winner = winner?.Name?.ToString(),
            winnerFaction = winner?.MapFaction?.Name?.ToString(),
            loser = loser?.Name?.ToString(),
            loserFaction = loser?.MapFaction?.Name?.ToString(),
            involvesPlayer = winner == Hero.MainHero || loser == Hero.MainHero,
        });
    }

    // --- Tournaments ---

    private void OnTournamentJoined(Town? town, bool isParticipant)
    {
        Emit("campaign/tournament_joined", new
        {
            town = town?.Name?.ToString(),
            isParticipant,
        });
    }

    private void OnTournamentFinished(CharacterObject? winner, MBReadOnlyList<CharacterObject?>? participants, Town? town, ItemObject? prize)
    {
        Emit("campaign/tournament_finished", new
        {
            winner = winner?.Name?.ToString(),
            town = town?.Name?.ToString(),
            prize = prize?.Name?.ToString(),
            playerWon = winner == CharacterObject.PlayerCharacter,
            participantCount = participants?.Count ?? 0,
        });
    }

    private void OnTournamentEliminated(int round, Town? town)
    {
        Emit("campaign/tournament_eliminated", new
        {
            round,
            town = town?.Name?.ToString(),
        });
    }
}