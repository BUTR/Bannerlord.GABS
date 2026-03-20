using HarmonyLib;

using System;
using System.IO;
using System.Linq;
using System.Reflection;

#if v1313 || v1315
using TaleWorlds.CampaignSystem.Incidents;
#endif
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.GABS.Patches;

[HarmonyPatch(typeof(InformationManager), nameof(InformationManager.ShowInquiry))]
internal static class ShowInquiryPatch
{
    static void Postfix(InquiryData data)
    {
        InquiryState.Log($"ShowInquiry: title={data?.TitleText}");
        InquiryState.CurrentInquiry = data;
        InquiryState.CurrentMultiSelection = null;
        InquiryState.CurrentTextInquiry = null;
    }
}

[HarmonyPatch(typeof(InformationManager), nameof(InformationManager.HideInquiry))]
internal static class HideInquiryPatch
{
    static void Postfix()
    {
        InquiryState.Log("HideInquiry");
        InquiryState.CurrentInquiry = null;
        InquiryState.CurrentMultiSelection = null;
        InquiryState.CurrentTextInquiry = null;
    }
}

[HarmonyPatch(typeof(InformationManager), nameof(InformationManager.ShowTextInquiry))]
internal static class ShowTextInquiryPatch
{
    static void Postfix(TextInquiryData textData)
    {
        InquiryState.Log($"ShowTextInquiry: title={textData?.TitleText}");
        InquiryState.CurrentTextInquiry = textData;
        InquiryState.CurrentInquiry = null;
        InquiryState.CurrentMultiSelection = null;
    }
}

[HarmonyPatch(typeof(MBInformationManager), nameof(MBInformationManager.ShowMultiSelectionInquiry))]
internal static class ShowMultiSelectionInquiryPatch
{
    static void Postfix(MultiSelectionInquiryData data)
    {
        InquiryState.Log($"ShowMultiSelectionInquiry: title={data?.TitleText}");
        InquiryState.CurrentMultiSelection = data;
        InquiryState.CurrentInquiry = null;
        InquiryState.CurrentTextInquiry = null;
    }
}

[HarmonyPatch(typeof(MBInformationManager), nameof(MBInformationManager.ShowSceneNotification))]
internal static class ShowSceneNotificationPatch
{
    static void Postfix(SceneNotificationData data)
    {
        InquiryState.Log($"ShowSceneNotification: type={data?.GetType().Name}, title={data?.TitleText}");
        InquiryState.CurrentSceneNotification = data;
    }
}

[HarmonyPatch(typeof(MBInformationManager), nameof(MBInformationManager.HideSceneNotification))]
internal static class HideSceneNotificationPatch
{
    static void Postfix()
    {
        InquiryState.Log("HideSceneNotification");
        InquiryState.CurrentSceneNotification = null;
    }
}

#if v1313 || v1315
    /// <summary>
    /// Manually patches GauntletMapIncidentView.CreateLayout and OnFinalize
    /// to track the Incident system (random events like "Feeling the Bite").
    /// These use a completely separate UI from InformationManager inquiries.
    /// Only available in v1.3.x+ where the Incident system exists.
    /// </summary>
    internal static class IncidentPatches
    {
        public static void Apply(Harmony harmony)
        {
            try
            {
                // Find GauntletMapIncidentView type across all loaded assemblies
                var viewType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.Name == "GauntletMapIncidentView");

                if (viewType == null)
                {
                    InquiryState.Log("IncidentPatches: GauntletMapIncidentView not found");
                    return;
                }

                // Patch CreateLayout to capture the incident
                var createLayout = viewType.GetMethod("CreateLayout",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (createLayout != null)
                {
                    harmony.Patch(createLayout,
                        postfix: new HarmonyMethod(typeof(IncidentPatches), nameof(CreateLayoutPostfix)));
                    InquiryState.Log("IncidentPatches: Patched CreateLayout");
                }

                // Patch OnFinalize to clear the incident
                var onFinalize = viewType.GetMethod("OnFinalize",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (onFinalize != null)
                {
                    harmony.Patch(onFinalize,
                        postfix: new HarmonyMethod(typeof(IncidentPatches), nameof(OnFinalizePostfix)));
                    InquiryState.Log("IncidentPatches: Patched OnFinalize");
                }
            }
            catch (Exception ex)
            {
                InquiryState.Log($"IncidentPatches: FAILED: {ex}");
            }
        }

        static void CreateLayoutPostfix(object __instance)
        {
            try
            {
                // MapIncidentView has a public Incident field
                var incidentField = __instance.GetType().GetField("Incident", BindingFlags.Instance | BindingFlags.Public);
                if (incidentField != null)
                {
                    if (incidentField.GetValue(__instance) is Incident incident)
                    {
                        InquiryState.CurrentIncident = incident;
                        InquiryState.CurrentIncidentView = __instance;
                        InquiryState.Log($"Incident started: {incident.Title}");
                    }
                }
            }
            catch (Exception ex)
            {
                InquiryState.Log($"CreateLayoutPostfix error: {ex.Message}");
            }
        }

        static void OnFinalizePostfix(object __instance)
        {
            try
            {
                if (InquiryState.CurrentIncidentView == __instance)
                {
                    InquiryState.Log($"Incident finalized: {InquiryState.CurrentIncident?.Title}");
                    InquiryState.CurrentIncident = null;
                    InquiryState.CurrentIncidentView = null;
                }
            }
            catch (Exception ex)
            {
                InquiryState.Log($"OnFinalizePostfix error: {ex.Message}");
            }
        }
    }
#endif

internal static class InquiryState
{
    public static volatile InquiryData? CurrentInquiry;
    public static volatile MultiSelectionInquiryData? CurrentMultiSelection;
    public static volatile TextInquiryData? CurrentTextInquiry;
#if v1313 || v1315
        public static volatile Incident? CurrentIncident;
#else
    public static volatile object? CurrentIncident;
#endif
    public static volatile object? CurrentIncidentView;
    public static volatile SceneNotificationData? CurrentSceneNotification;

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Mount and Blade II Bannerlord", "gabs_inquiry.log");

    public static void Log(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch { }
    }
}