// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using Bannerlord.GABS.Patches;

using Lib.GAB.Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.GABS.Tools;

public partial class InquiryTools
{
    [Tool("ui/get_inquiry", Description = "Get the currently displayed popup inquiry/dialog (title, text, options). Also detects random event popups (Incidents) and cinematic scene notifications (kingdom created, marriage, death, etc.).")]
    public partial Task<object> GetInquiry()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
#if v1313 || v1315
                // Check for Incident (random event popup) first — most common blocker (v1.3.x+)
                if (InquiryState.CurrentIncident != null)
                {
                    var incident = InquiryState.CurrentIncident;
                    var options = new List<object>();
                    for (int i = 0; i < incident.NumOfOptions; i++)
                    {
                        var hints = incident.GetOptionHint(i);
                        options.Add(new
                        {
                            /// Option index (0-based)
                            index = i,
                            /// Option title text
                            title = incident.GetOptionText(i)?.ToString(),
                            /// Hint text for the option
                            hint = hints is { Count: > 0 }
                                ? string.Join("; ", hints.Select(h => h?.ToString()))
                                : null,
                        });
                    }

                    return new
                    {
                        /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                        type = "incident",
                        /// Inquiry title text
                        title = incident.Title?.ToString(),
                        /// Inquiry description/body text
                        description = incident.Description?.ToString(),
                        /// Array of option objects (for multi_selection/incident)
                        options = options,
                        /// Text of the affirmative/confirm button
                        affirmativeText = "Done",
                        /// Text of the negative/cancel button
                        negativeText = (string?) null,
                        /// Whether an exit button is shown
                        isExitShown = false,
                    };
                }
#endif

            if (InquiryState.CurrentMultiSelection != null)
            {
                var elements = InquiryState.CurrentMultiSelection.InquiryElements?
                    .Select((e, i) => new
                    {
                        /// Option index (0-based)
                        index = i,
                        /// Option title text
                        title = e.Title,
                        /// Whether the option is enabled
                        isEnabled = e.IsEnabled,
                        /// Hint text for the option
                        hint = e.Hint,
                    }).ToList();

                return new
                {
                    /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                    type = "multi_selection",
                    /// Inquiry title text
                    title = InquiryState.CurrentMultiSelection.TitleText,
                    /// Inquiry description/body text
                    description = InquiryState.CurrentMultiSelection.DescriptionText,
                    /// Array of option objects (for multi_selection/incident)
                    options = elements,
                    /// Text of the affirmative/confirm button
                    affirmativeText = InquiryState.CurrentMultiSelection.AffirmativeText,
                    /// Text of the negative/cancel button
                    negativeText = InquiryState.CurrentMultiSelection.NegativeText,
                    /// Whether an exit button is shown
                    isExitShown = InquiryState.CurrentMultiSelection.IsExitShown,
                };
            }

            if (InquiryState.CurrentInquiry != null)
            {
                return new
                {
                    /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                    type = "yes_no",
                    /// Inquiry title text
                    title = InquiryState.CurrentInquiry.TitleText,
                    /// Inquiry description/body text
                    description = InquiryState.CurrentInquiry.Text,
                    /// Array of option objects (for multi_selection/incident)
                    options = new List<object>(),
                    /// Text of the affirmative/confirm button
                    affirmativeText = InquiryState.CurrentInquiry.AffirmativeText,
                    /// Text of the negative/cancel button
                    negativeText = InquiryState.CurrentInquiry.NegativeText,
                    /// Whether an exit button is shown
                    isExitShown = false,
                };
            }

            if (InquiryState.CurrentTextInquiry != null)
            {
                return new
                {
                    /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                    type = "text_input",
                    /// Inquiry title text
                    title = InquiryState.CurrentTextInquiry.TitleText,
                    /// Inquiry description/body text
                    description = InquiryState.CurrentTextInquiry.Text,
                    /// Array of option objects (for multi_selection/incident)
                    options = new List<object>(),
                    /// Text of the affirmative/confirm button
                    affirmativeText = InquiryState.CurrentTextInquiry.AffirmativeText,
                    /// Text of the negative/cancel button
                    negativeText = InquiryState.CurrentTextInquiry.NegativeText,
                    /// Whether an exit button is shown
                    isExitShown = false,
                };
            }

            if (InquiryState.CurrentSceneNotification != null)
            {
                var notification = InquiryState.CurrentSceneNotification;
                return new
                {
                    /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                    type = "scene_notification",
                    /// Inquiry title text
                    title = notification.TitleText?.ToString(),
                    /// Inquiry description/body text
                    description = notification.GetType().Name,
                    /// Array of option objects (for multi_selection/incident)
                    options = new List<object>(),
                    /// Text of the affirmative/confirm button
                    affirmativeText = notification.AffirmativeText?.ToString(),
                    /// Text of the negative/cancel button
                    negativeText = (string?) null,
                    /// Whether an exit button is shown
                    isExitShown = false,
                };
            }

            return new
            {
                /// Inquiry type: 'none', 'yes_no', 'multi_selection', 'text_input', 'incident', 'scene_notification'
                type = "none",
                /// Inquiry title text
                title = (string?) null,
                /// Inquiry description/body text
                description = (string?) null,
                /// Array of option objects (for multi_selection/incident)
                options = (object?) null,
                /// Text of the affirmative/confirm button
                affirmativeText = (string?) null,
                /// Text of the negative/cancel button
                negativeText = (string?) null,
                /// Whether an exit button is shown
                isExitShown = false,
            };
        });
    }

    [Tool("ui/answer_inquiry", Description = "Answer the current popup inquiry or incident. For yes/no: use affirmative=true/false. For multi-selection: provide selectedIndices and affirmative=true. For incidents: provide selectedIndices with the option index (e.g. '1') and affirmative=true.")]
    public partial Task<object> AnswerInquiry(
        [ToolParameter(Description = "true for affirmative/confirm, false for negative/cancel")] bool affirmative,
        [ToolParameter(Description = "For multi-selection/incidents: comma-separated indices of selected options (e.g. '0' or '0,2'). For text input: the text to submit.", Required = false)] string? selectedIndices)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
#if v1313 || v1315
                // Handle Incident (random event popup, v1.3.x+)
                if (InquiryState.CurrentIncident != null && InquiryState.CurrentIncidentView != null)
                {
                    var incident = InquiryState.CurrentIncident;
                    var view = InquiryState.CurrentIncidentView;

                    if (!affirmative)
                    {
                        return new { error = "Incidents must be answered with affirmative=true and a selectedIndices option" };
                    }

                    if (string.IsNullOrWhiteSpace(selectedIndices) ||
                        !int.TryParse(selectedIndices?.Trim(), out var optionIdx) ||
                        optionIdx < 0 || optionIdx >= incident.NumOfOptions)
                    {
                        return new { error = $"Invalid option index. Must be 0-{incident.NumOfOptions - 1}" };
                    }

                    // Invoke the option (executes the consequence)
                    var results = incident.InvokeOption(optionIdx);
                    foreach (var text in results)
                    {
                        MBInformationManager.AddQuickInformation(text);
                    }

                    // Close the view by calling the private OnCloseView method
                    try
                    {
                        var closeMethod = view.GetType().GetMethod("OnCloseView",
                            BindingFlags.Instance | BindingFlags.NonPublic);
                        closeMethod?.Invoke(view, null);
                    }
                    catch (Exception ex)
                    {
                        InquiryState.Log($"Failed to close incident view: {ex.Message}");
                    }

                    return new
                    {
                        /// Action taken: 'affirmative', 'negative', or 'incident_option_N'
                        action = "incident_option_" + optionIdx,
                    };
                }
#endif

            if (InquiryState.CurrentMultiSelection != null)
            {
                var data = InquiryState.CurrentMultiSelection;
                InquiryState.CurrentMultiSelection = null;

                if (affirmative)
                {
                    var selected = new List<InquiryElement>();
                    if (!string.IsNullOrWhiteSpace(selectedIndices))
                    {
                        foreach (var part in selectedIndices?.Split(',') ?? [])
                        {
                            if (int.TryParse(part.Trim(), out var idx) &&
                                idx >= 0 && idx < data.InquiryElements.Count)
                            {
                                selected.Add(data.InquiryElements[idx]);
                            }
                        }
                    }

                    data.AffirmativeAction?.Invoke(selected);
                }
                else
                {
                    data.NegativeAction?.Invoke(new List<InquiryElement>());
                }

                InformationManager.HideInquiry();
                return new
                {
                    /// Action taken: 'affirmative', 'negative', or 'incident_option_N'
                    action = affirmative ? "affirmative" : "negative",
                };
            }

            if (InquiryState.CurrentInquiry != null)
            {
                var data = InquiryState.CurrentInquiry;
                InquiryState.CurrentInquiry = null;

                if (affirmative)
                    data.AffirmativeAction?.Invoke();
                else
                    data.NegativeAction?.Invoke();

                InformationManager.HideInquiry();
                return new
                {
                    /// Action taken: 'affirmative', 'negative', or 'incident_option_N'
                    action = affirmative ? "affirmative" : "negative",
                };
            }

            if (InquiryState.CurrentTextInquiry != null)
            {
                var data = InquiryState.CurrentTextInquiry;
                InquiryState.CurrentTextInquiry = null;

                if (affirmative)
                    data.AffirmativeAction?.Invoke(selectedIndices ?? "");
                else
                    data.NegativeAction?.Invoke();

                InformationManager.HideInquiry();
                return new
                {
                    /// Action taken: 'affirmative', 'negative', or 'incident_option_N'
                    action = affirmative ? "affirmative" : "negative",
                };
            }

            if (InquiryState.CurrentSceneNotification != null)
            {
                var notificationType = InquiryState.CurrentSceneNotification.GetType().Name;
                MBInformationManager.HideSceneNotification();
                return new
                {
                    /// Action taken: 'affirmative', 'negative', or 'incident_option_N'
                    action = $"dismissed_{notificationType}",
                };
            }

            return new { error = "No active inquiry to answer" };
        });
    }
}