// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.LogEntries;

namespace Bannerlord.GABS.Tools;

public partial class HistoryTools
{
    [Tool("history/get_recent_events", Description = "Get recent game events from the log (wars, deaths, battles, marriages, etc.).")]
    public partial Task<object> GetRecentEvents(
        [ToolParameter(Description = "Max events to return (default 30)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var max = limit ?? 30;
            var history = Campaign.Current.LogEntryHistory;
            if (history == null)
                return new { error = "No log entry history available" };

            var events = history.GetGameActionLogs<LogEntry>(e => true)
                .Take(max)
                .Select(SerializeLogEntry)
                .ToList();

            return new
            {
                /// Number of events returned
                count = events.Count,
                /// Array of event objects with type and text
                events,
            };
        });
    }

    [Tool("history/get_events_by_type", Description = "Get events filtered by type (e.g. 'war', 'death', 'battle', 'marriage', 'birth', 'siege').")]
    public partial Task<object> GetEventsByType(
        [ToolParameter(Description = "Event type filter: 'war', 'peace', 'death', 'battle', 'marriage', 'birth', 'siege', 'prisoner', 'kingdom'")] string type,
        [ToolParameter(Description = "Max events to return (default 30)", Required = false)] int? limit)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            if (string.IsNullOrWhiteSpace(type))
                return new { error = "Event type is required. Valid types: war, peace, death, battle, marriage, birth, siege, prisoner, kingdom" };

            var max = limit ?? 30;
            var history = Campaign.Current.LogEntryHistory;
            if (history == null)
                return new { error = "No log entry history available" };

            Func<LogEntry, bool> predicate = type.ToLowerInvariant() switch
            {
                "war" => e => e is DeclareWarLogEntry,
                "peace" => e => e is MakePeaceLogEntry,
                "death" => e => e is CharacterKilledLogEntry,
                "battle" => e => e is BattleStartedLogEntry or PlayerBattleEndedLogEntry,
                "marriage" => e => e is CharacterMarriedLogEntry,
                "birth" => e => e is ChildbirthLogEntry or CharacterBornLogEntry,
                "siege" => e => e is BesiegeSettlementLogEntry,
                "prisoner" => e => e is TakePrisonerLogEntry or EndCaptivityLogEntry,
                "kingdom" => e => e is ClanChangeKingdomLogEntry or KingdomDecisionConcludedLogEntry,
                _ => e => e.GetType().Name.IndexOf(type, StringComparison.OrdinalIgnoreCase) >= 0,
            };

            var events = history.GetGameActionLogs(predicate)
                .Take(max)
                .Select(SerializeLogEntry)
                .ToList();

            return new
            {
                /// Number of events returned
                count = events.Count,
                /// Array of event objects with type and text
                events,
            };
        });
    }

    private static object SerializeLogEntry(LogEntry entry)
    {
        return new
        {
            type = entry.GetType().Name,
            text = entry.ToString(),
        };
    }
}