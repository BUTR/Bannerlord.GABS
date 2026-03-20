using HarmonyLib;

using SandBox.View.Map;

namespace Bannerlord.GABS.Patches;

/// <summary>
/// Prevents the game from opening the escape menu when the window loses focus
/// while GABS is connected. This allows the AI agent to control the game
/// without the pause menu interrupting.
/// </summary>
[HarmonyPatch(typeof(MapScreen), nameof(MapScreen.OnFocusChangeOnGameWindow))]
public static class MapScreenFocusPatch
{
    public static bool IsEnabled { get; set; }

    static bool Prefix(bool focusGained)
    {
        if (!IsEnabled)
            return true;

        // When GABS is active, skip the escape menu on focus loss.
        // The base method still sets _focusLost, but we prevent the
        // escape menu from opening. We only suppress when focus is lost.
        if (!focusGained)
            return false;

        return true;
    }
}