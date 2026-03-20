// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem.BarterSystem;

namespace Bannerlord.GABS.Tools;

public partial class BarterTools
{
    private static BarterData? _activeBarterData;

    /// <summary>
    /// Called from SubModule.OnGameStart when BarterManager is available.
    /// </summary>
    public static void OnBarterBegin(BarterData args)
    {
        _activeBarterData = args;
    }

    /// <summary>
    /// Called from SubModule.OnGameStart when barter screen closes.
    /// </summary>
    public static void OnBarterClosed()
    {
        _activeBarterData = null;
    }

    [Tool("barter/get_state", Description = "Get the current barter screen state: participants, available items to offer, and what's currently offered.")]
    public partial Task<object> GetBarterState()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var data = _activeBarterData;

            var items = data?.GetBarterables().Select((b, i) => new
            {
                index = i,
                type = b.GetType().Name,
                name = b.Name?.ToString(),
                stringId = b.StringID,
                isOffered = b.IsOffered,
                currentAmount = b.CurrentAmount,
                maxAmount = b.MaxAmount,
                owner = b.OriginalOwner?.Name?.ToString(),
                ownerParty = b.OriginalParty?.Name?.ToString(),
                side = b.Side.ToString(),
            }).ToList();

            return new
            {
                /// Whether a barter is in progress
                active = data != null,
                /// Offerer hero name
                offerer = data?.OffererHero?.Name?.ToString(),
                /// Other party hero name
                other = data?.OtherHero?.Name?.ToString(),
                /// Offerer party name
                offererParty = data?.OffererParty?.Name?.ToString(),
                /// Other party name
                otherParty = data?.OtherParty?.Name?.ToString(),
                /// Barterable items with index, type, name, isOffered, currentAmount, maxAmount, side
                barterables = items,
            };
        });
    }

    [Tool("barter/offer_item", Description = "Offer or unoffer a barterable item by index. For gold, also set the amount.")]
    public partial Task<object> OfferItem(
        [ToolParameter(Description = "Index of the barterable item (from get_state)")] int index,
        [ToolParameter(Description = "true to offer, false to unoffer")] bool offer,
        [ToolParameter(Description = "Amount to offer (for gold). Ignored for non-gold items.", Required = false)] int? amount)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (_activeBarterData == null)
                return new { error = "No active barter" };

            var barterables = _activeBarterData.GetBarterables();
            if (index < 0 || index >= barterables.Count)
                return new { error = $"Invalid index. Must be 0-{barterables.Count - 1}" };

            var item = barterables[index];
            item.SetIsOffered(offer);

            if (amount.HasValue && offer)
            {
                item.CurrentAmount = amount.Value;
            }

            return new
            {
                /// Item name
                name = item.Name?.ToString(),
                /// Current offer state
                isOffered = item.IsOffered,
                /// Current offered amount
                currentAmount = item.CurrentAmount,
                /// Maximum amount available
                maxAmount = item.MaxAmount,
            };
        });
    }

    [Tool("barter/accept", Description = "Accept the current barter offer (finalize the deal).")]
    public partial Task<object> AcceptBarter()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (_activeBarterData == null)
                return new { error = "No active barter" };

            var data = _activeBarterData;
            var manager = BarterManager.Instance;
            if (manager == null)
                return new { error = "BarterManager not available" };

            // Check if the offer is acceptable
            var isAcceptable = manager.IsOfferAcceptable(data, data.OtherHero, data.OtherParty);

            if (!isAcceptable)
                return new { error = "Offer is not acceptable to the other party. You need to offer more." };

            manager.ApplyAndFinalizePlayerBarter(data.OffererHero, data.OtherHero, data);
            _activeBarterData = null;

            return new
            {
                /// Status message
                message = "Barter accepted and applied",
            };
        });
    }

    [Tool("barter/cancel", Description = "Cancel and close the current barter screen.")]
    public partial Task<object> CancelBarter()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (_activeBarterData == null)
                return new { error = "No active barter" };

            var data = _activeBarterData;
            var manager = BarterManager.Instance;
            if (manager == null)
                return new { error = "BarterManager not available" };

            manager.CancelAndFinalizePlayerBarter(data.OffererHero, data.OtherHero, data);
            _activeBarterData = null;

            return new
            {
                /// Status message
                message = "Barter cancelled",
            };
        });
    }
}