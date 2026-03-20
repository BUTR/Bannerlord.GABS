// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Lib.GAB.Tools;

using System.Collections.Generic;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;

namespace Bannerlord.GABS.Tools;

public partial class MenuTools
{
    [Tool("menu/get_current", Description = "Get the current game menu: title, text, and available options.")]
    public partial Task<object> GetCurrentMenu()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var menuContext = Campaign.Current.CurrentMenuContext;
            var isActive = menuContext is { GameMenu: not null };

            string? menuId = null;
            string? menuText = null;
            var options = new List<object>();

            if (isActive)
            {
                var menuManager = Campaign.Current.GameMenuManager;
                menuText = menuManager.GetMenuText(menuContext!)?.ToString();
                menuId = menuContext?.GameMenu?.StringId;

                var optionCount = menuManager.GetVirtualMenuOptionAmount(menuContext);

                for (int i = 0; i < optionCount; i++)
                {
                    var optionText = menuManager.GetVirtualMenuOptionText(menuContext, i)?.ToString();
                    var isEnabled = menuManager.GetVirtualMenuOptionIsEnabled(menuContext, i);
                    var isLeave = menuManager.GetVirtualMenuOptionIsLeave(menuContext, i);
                    var tooltip = menuManager.GetVirtualMenuOptionTooltip(menuContext, i)?.ToString();
                    // GetMenuOptionIdString uses physical index; use GetVirtualGameMenuOption for the correct virtual id
                    var idString = menuManager.GetVirtualGameMenuOption(menuContext, i)?.IdString;

                    options.Add(new
                    {
                        /// Option index (0-based)
                        index = i,
                        /// Menu option string ID
                        id = idString,
                        /// Option display text
                        text = optionText,
                        /// Whether the option is enabled
                        isEnabled,
                        /// Whether the option is a leave/exit option
                        isLeave,
                        /// Tooltip text (may explain why disabled)
                        tooltip,
                    });
                }
            }

            return new
            {
                /// Whether a game menu is active
                isActive,
                /// Game menu string ID
                menuId,
                /// Menu description text
                text = menuText,
                /// Number of menu options
                optionCount = options.Count,
                /// Array of option objects with index, id, text, isEnabled, isLeave, tooltip
                options,
            };
        });
    }

    [Tool("menu/select_option", Description = "Select a game menu option by index (0-based) or by option string ID (e.g. 'attack', 'leave'). Prefer optionId when the menu has repeatable items, as virtual indices can shift between get_current and select_option.")]
    public partial Task<object> SelectOption(
        [ToolParameter(Description = "Menu option index (0-based). Ignored if optionId is provided.")] int index,
        [ToolParameter(Description = "Option string ID (e.g. 'attack', 'leave'). Takes precedence over index.", Required = false)] string? optionId)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var menuContext = Campaign.Current.CurrentMenuContext;
            if (menuContext?.GameMenu == null)
                return new { error = "No active game menu" };

            var menuManager = Campaign.Current.GameMenuManager;
            var optionCount = menuManager.GetVirtualMenuOptionAmount(menuContext);

            // If an ID was provided, find the matching virtual index at execution time
            if (!string.IsNullOrEmpty(optionId))
            {
                index = -1;
                for (int i = 0; i < optionCount; i++)
                {
                    if (menuManager.GetVirtualGameMenuOption(menuContext, i)?.IdString == optionId)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                    return new { error = $"No option found with id '{optionId}'" };
            }

            if (index < 0 || index >= optionCount)
                return new { error = $"Invalid option index {index}. Valid: 0-{optionCount - 1}" };

            if (!menuManager.GetVirtualMenuOptionIsEnabled(menuContext, index))
            {
                var tooltip = menuManager.GetVirtualMenuOptionTooltip(menuContext, index)?.ToString();
                return new { error = $"Option {index} is disabled: {tooltip}" };
            }

            var optionText = menuManager.GetVirtualMenuOptionText(menuContext, index)?.ToString();
            // RunConsequencesOfMenuOption takes a physical index, but 'index' here is virtual.
            // Use the internal RunConsequenceOfVirtualMenuOption which handles the virtual-to-physical mapping.
            var runVirtualMethod = menuManager.GetType()
                .GetMethod("RunConsequenceOfVirtualMenuOption",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (runVirtualMethod != null)
                runVirtualMethod.Invoke(menuManager, [menuContext, index]);
            else
                menuManager.RunConsequencesOfMenuOption(menuContext, index);

            return new
            {
                /// Text of the selected option
                selected = optionText,
            };
        });
    }
}