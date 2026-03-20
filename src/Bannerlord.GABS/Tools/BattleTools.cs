// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.GABS.Tools;

public partial class BattleTools
{
    [Tool("battle/get_state", Description = "Get the current battle/mission state including sides, troop counts, and battle phase.")]
    public partial Task<object> GetState()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            try
            {
                var mission = Mission.Current;
                if (mission == null)
                    return new { error = "No active mission" };

                var playerTeam = mission.PlayerTeam;
                var enemyTeam = mission.PlayerEnemyTeam;
                var playerQS = playerTeam?.QuerySystem;
                var enemyQS = enemyTeam?.QuerySystem;

                object? playerAgent = mission.MainAgent != null
                    ? new
                    {
                        /// Agent display name
                        name = mission.MainAgent.Name,
                        /// Current health points
                        health = Math.Round(mission.MainAgent.Health, 1),
                        /// Maximum health points
                        healthLimit = Math.Round(mission.MainAgent.HealthLimit, 1),
                        /// Whether the agent is alive and active
                        isActive = mission.MainAgent.IsActive(),
                        /// Agent world position (x, y, z)
                        position = new
                        {
                            /// X coordinate
                            x = Math.Round(mission.MainAgent.Position.x, 1),
                            /// Y coordinate
                            y = Math.Round(mission.MainAgent.Position.y, 1),
                            /// Z coordinate
                            z = Math.Round(mission.MainAgent.Position.z, 1),
                        },
                    }
                    : null;

                object? battleResult = mission.MissionResult != null
                    ? new
                    {
                        /// Whether the player won
                        playerVictory = mission.MissionResult.PlayerVictory,
                        /// Whether the player was defeated
                        playerDefeated = mission.MissionResult.PlayerDefeated,
                        /// Battle state enum value
                        battleState = mission.MissionResult.BattleState.ToString(),
                    }
                    : null;

                return new
                {
                    /// Mission current state
                    state = mission.CurrentState.ToString(),
                    /// Mission mode
                    mode = mission.Mode.ToString(),
                    /// Mission combat type
                    combatType = mission.CombatType.ToString(),
                    /// Whether it's a field battle
                    isFieldBattle = mission.IsFieldBattle,
                    /// Whether it's a siege battle
                    isSiegeBattle = mission.IsSiegeBattle,
                    /// Whether the mission has ended
                    missionEnded = mission.MissionEnded,
                    /// Elapsed mission time in seconds
                    elapsedTime = Math.Round(mission.CurrentTime, 1),
                    /// Scene name of the mission
                    sceneName = mission.SceneName,
                    /// Player side name
                    playerSide = playerTeam?.Side.ToString(),
                    /// Player side active troop count
                    playerTroopCount = playerQS?.MemberCount,
                    /// Player side death count
                    playerDeaths = playerQS?.DeathCount,
                    /// Player side team power
                    playerPower = playerQS != null ? Math.Round(playerQS.TeamPower, 1) : (double?) null,
                    /// Player side remaining power ratio
                    playerRemainingPowerRatio = playerQS != null ? Math.Round(playerQS.RemainingPowerRatio, 3) : (double?) null,
                    /// Enemy side name
                    enemySide = enemyTeam?.Side.ToString(),
                    /// Enemy side active troop count
                    enemyTroopCount = enemyQS?.MemberCount,
                    /// Enemy side death count
                    enemyDeaths = enemyQS?.DeathCount,
                    /// Enemy side team power
                    enemyPower = enemyQS != null ? Math.Round(enemyQS.TeamPower, 1) : (double?) null,
                    /// Enemy side remaining power ratio
                    enemyRemainingPowerRatio = enemyQS != null ? Math.Round(enemyQS.RemainingPowerRatio, 3) : (double?) null,
                    /// Player agent info (name, health, position) or null if not active
                    playerAgent,
                    /// Battle result if mission ended, or null
                    battleResult,
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to read battle state: {ex.Message}" };
            }
        });
    }

    [Tool("battle/get_formations", Description = "List the player's formations with troop counts, types, and current orders.")]
    public partial Task<object> GetFormations()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var playerTeam = mission.PlayerTeam;
            if (playerTeam == null)
                return new { error = "No player team" };

            var formations = new List<object>();
            foreach (var formation in playerTeam.FormationsIncludingEmpty)
            {
                if (formation.CountOfUnits == 0)
                    continue;

                formations.Add(new
                {
                    /// Formation class (e.g. 'Infantry', 'Ranged')
                    formationClass = formation.RepresentativeClass.ToString(),
                    /// Formation index number
                    index = (int) formation.FormationIndex,
                    /// Number of units in the formation
                    unitCount = formation.CountOfUnits,
                    /// Whether the formation is AI-controlled
                    isAIControlled = formation.IsAIControlled,
                    /// Captain agent name
                    captain = formation.Captain?.Name,
                    /// Current movement order
                    currentOrder = OrderController.GetActiveMovementOrderOf(formation).ToString(),
                    /// Current arrangement order
                    arrangement = OrderController.GetActiveArrangementOrderOf(formation).ToString(),
                    /// Current firing order
                    firingOrder = OrderController.GetActiveFiringOrderOf(formation).ToString(),
                    /// Whether the player troop is in this formation
                    hasPlayerTroop = formation.IsPlayerTroopInFormation,
                });
            }

            return new
            {
                /// Number of active formations
                count = formations.Count,
                /// Whether the player is the army general
                isPlayerGeneral = playerTeam.IsPlayerGeneral,
                /// Whether the player is a sergeant
                isPlayerSergeant = playerTeam.IsPlayerSergeant,
                /// Array of formation objects with formationClass, unitCount, currentOrder, etc.
                formations,
            };
        });
    }

    [Tool("battle/order_charge", Description = "Order one or all player formations to charge the enemy.")]
    public partial Task<object> OrderCharge(
        [ToolParameter(Description = "Formation class to order (Infantry, Ranged, Cavalry, HorseArcher, Skirmisher, HeavyInfantry, LightCavalry, HeavyCavalry). Omit to order all formations.", Required = false)] string? formationClass)
    {
        return IssueOrder(formationClass, OrderType.Charge, "Charge");
    }

    [Tool("battle/order_hold", Description = "Order one or all player formations to hold position (stand your ground).")]
    public partial Task<object> OrderHold(
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        return IssueOrder(formationClass, OrderType.StandYourGround, "Hold");
    }

    [Tool("battle/order_retreat", Description = "Order one or all player formations to retreat from battle.")]
    public partial Task<object> OrderRetreat(
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        return IssueOrder(formationClass, OrderType.Retreat, "Retreat");
    }

    [Tool("battle/order_advance", Description = "Order one or all player formations to advance toward the enemy.")]
    public partial Task<object> OrderAdvance(
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        return IssueOrder(formationClass, OrderType.Advance, "Advance");
    }

    [Tool("battle/order_fallback", Description = "Order one or all player formations to fall back away from the enemy.")]
    public partial Task<object> OrderFallBack(
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        return IssueOrder(formationClass, OrderType.FallBack, "FallBack");
    }

    [Tool("battle/order_follow_me", Description = "Order one or all player formations to follow the player character.")]
    public partial Task<object> OrderFollowMe(
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var playerTeam = mission.PlayerTeam;
            if (playerTeam == null)
                return new { error = "No player team" };

            if (mission.MainAgent == null)
                return new { error = "Player agent not active" };

            var oc = playerTeam.PlayerOrderController;
            SelectFormations(oc, playerTeam, formationClass);

            if (oc.SelectedFormations.Count == 0)
                return new { error = $"No formation found for: {formationClass}" };

            oc.SetOrderWithAgent(OrderType.FollowMe, mission.MainAgent);

            return new
            {
                /// Order type that was issued
                order = "FollowMe",
                /// Array of formation class names that received the order
                formationsOrdered = oc.SelectedFormations.Select(f => f.RepresentativeClass.ToString()).ToList(),
            };
        });
    }

    [Tool("battle/set_fire_order", Description = "Set firing order for one or all player formations.")]
    public partial Task<object> SetFireOrder(
        [ToolParameter(Description = "true for FireAtWill, false for HoldFire")] bool fireAtWill,
        [ToolParameter(Description = "Formation class to order. Omit to order all formations.", Required = false)] string? formationClass)
    {
        var orderType = fireAtWill ? OrderType.FireAtWill : OrderType.HoldFire;
        return IssueOrder(formationClass, orderType, fireAtWill ? "FireAtWill" : "HoldFire");
    }

    [Tool("battle/order_delegate_to_ai", Description = "Delegate command of all formations to the AI.")]
    public partial Task<object> OrderDelegateToAI()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var playerTeam = mission.PlayerTeam;
            if (playerTeam == null)
                return new { error = "No player team" };

            playerTeam.DelegateCommandToAI();

            return new
            {
                /// Status message
                message = "Command delegated to AI",
            };
        });
    }

    private Task<object> IssueOrder(string? formationClass, OrderType orderType, string orderName)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var mission = Mission.Current;
            if (mission == null)
                return new { error = "No active mission" };

            var playerTeam = mission.PlayerTeam;
            if (playerTeam == null)
                return new { error = "No player team" };

            var oc = playerTeam.PlayerOrderController;
            SelectFormations(oc, playerTeam, formationClass);

            if (oc.SelectedFormations.Count == 0)
                return new { error = $"No formation found for: {formationClass}" };

            oc.SetOrder(orderType);

            return new
            {
                /// Order type that was issued
                order = orderName,
                /// Array of formation class names that received the order
                formationsOrdered = oc.SelectedFormations.Select(f => f.RepresentativeClass.ToString()).ToList(),
            };
        });
    }

    private static void SelectFormations(OrderController oc, Team team, string? formationClass)
    {
        if (string.IsNullOrWhiteSpace(formationClass))
        {
            oc.SelectAllFormations(uiFeedback: false);
        }
        else if (Enum.TryParse<FormationClass>(formationClass, ignoreCase: true, out var fc))
        {
            oc.ClearSelectedFormations();
            var formation = team.GetFormation(fc);
            if (formation is { CountOfUnits: > 0 })
                oc.SelectFormation(formation);
        }
    }
}