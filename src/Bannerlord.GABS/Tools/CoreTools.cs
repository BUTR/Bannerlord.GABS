// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using Lib.GAB.Tools;

using SandBox;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;

namespace Bannerlord.GABS.Tools;

public partial class CoreTools
{
    private static readonly AccessTools.FieldRef<string>? ActiveSaveSlotNameField = AccessTools2.StaticFieldRefAccess<string>(typeof(MBSaveLoad), "ActiveSaveSlotName");
    private static readonly AccessTools.FieldRef<IDictionary>? AllFunctionsField = AccessTools2.StaticFieldRefAccess<IDictionary>(typeof(CommandLineFunctionality), "AllFunctions");

    [Tool("core/ping", Description = "Connectivity test. Returns pong with server timestamp.")]
    public partial object Ping()
    {
        return new
        {
            /// Always 'pong'
            message = "pong",
            /// UTC timestamp of the server
            timestamp = DateTime.UtcNow,
        };
    }


    [Tool("core/skip_video", Description = "Skip the currently playing video (e.g. the campaign intro video after new_game). Calls the video's completion callback so the next game state loads properly.")]
    public partial Task<object> SkipVideo()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var activeState = GameStateManager.Current?.ActiveState;
            if (activeState is VideoPlaybackState videoState)
            {
                videoState.OnVideoFinished();
                return new
                {
                    /// Status message
                    message = "Video skipped",
                    /// The video path that was playing
                    videoPath = videoState.VideoPath,
                };
            }

            return new
            {
                /// Status message
                message = $"No video playing. Current state: {activeState?.GetType().Name ?? "null"}",
                /// The video path that was playing
                videoPath = (string?) null,
            };
        });
    }

    [Tool("core/get_game_state", Description = "Get the current game state: main menu, campaign map, mission/battle, or loading.")]
    public partial Task<object> GetGameState()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current != null)
            {
                var state = "campaign_map";
                if (Mission.Current != null)
                    state = "mission";
                else if (Campaign.Current.GameMenuManager?.NextGameMenuId != null)
                    state = "game_menu";

                return new
                {
                    /// Current state: 'campaign_map', 'mission', 'game_menu', 'unknown', or GameState class name
                    state,
                    /// Current in-game time string
                    campaignTime = CampaignTime.Now.ToString(),
                    /// Active game menu ID if in a menu
                    currentMenu = Campaign.Current.GameMenuManager?.NextGameMenuId,
                    /// Mission scene name if in a mission
                    missionName = Mission.Current?.SceneName,
                };
            }

            if (GameStateManager.Current?.ActiveState != null)
            {
                return new
                {
                    /// Current state: 'campaign_map', 'mission', 'game_menu', 'unknown', or GameState class name
                    state = GameStateManager.Current.ActiveState.GetType().Name,
                    /// Current in-game time string
                    campaignTime = (string?) null,
                    /// Active game menu ID if in a menu
                    currentMenu = (string?) null,
                    /// Mission scene name if in a mission
                    missionName = (string?) null,
                };
            }

            return new
            {
                /// Current state: 'campaign_map', 'mission', 'game_menu', 'unknown', or GameState class name
                state = "unknown",
                /// Current in-game time string
                campaignTime = (string?) null,
                /// Active game menu ID if in a menu
                currentMenu = (string?) null,
                /// Mission scene name if in a mission
                missionName = (string?) null,
            };
        });
    }

    [Tool("core/check_blockers", Description = "Check for anything blocking gameplay: active conversations, map conversation overlays, inquiry popups, scene notifications, active missions, or pending menus. Call this before time-sensitive actions (set_time_speed, move_to_settlement) to ensure the game is in a clean state.")]
    public partial Task<object> CheckBlockers()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var blockers = new List<string>();

            // Check conversation
            if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
                blockers.Add("conversation_active");

            // Check map conversation overlay (persists after conversation ends)
            try
            {
                var screen = TaleWorlds.ScreenSystem.ScreenManager.TopScreen;
                if (screen != null)
                {
                    foreach (var layer in screen.Layers)
                    {
                        if (layer.Name == "MapConversation" && layer.IsActive)
                        {
                            blockers.Add("map_conversation_overlay");
                            break;
                        }
                    }
                }
            }
            catch { }

            // Check inquiry/popup
            if (TaleWorlds.Library.InformationManager.IsAnyInquiryActive())
                blockers.Add("inquiry_active");

            // Check mission
            if (Mission.Current != null)
                blockers.Add($"mission_active:{Mission.Current.SceneName}");

            // Check game menu
            if (Campaign.Current?.CurrentMenuContext?.GameMenu != null)
                blockers.Add($"menu_active:{Campaign.Current.CurrentMenuContext.GameMenu.StringId}");

            // Check if paused
            if (Campaign.Current?.TimeControlMode == CampaignTimeControlMode.Stop)
                blockers.Add("paused");

            return new
            {
                /// Whether the game is in a clean state with no blockers
                clear = blockers.Count == 0,
                /// Number of active blockers
                blockerCount = blockers.Count,
                /// List of active blockers
                blockers,
                /// Current game state for context
                state = Campaign.Current != null
                    ? (Mission.Current != null ? "mission" :
                        Campaign.Current.GameMenuManager?.NextGameMenuId != null ? "game_menu" : "campaign_map")
                    : GameStateManager.Current?.ActiveState?.GetType().Name ?? "unknown",
            };
        });
    }

    [Tool("core/wait_for_state", Description = "Wait until the game reaches an expected state. Use after core/load_save (state='campaign_map'), party/enter_settlement (state='game_menu'), or menu/select_option for missions (state='mission'). Pass timeout via games.call_tool timeout parameter.")]
    public partial Task<object> WaitForState(
        [ToolParameter(Description = "Expected state: 'campaign_map', 'mission', 'game_menu', or a GameState class name. Case-insensitive substring match.")] string expectedState,
        [ToolParameter(Description = "Poll interval in milliseconds (default 500).", Required = false)] int pollIntervalMs = 500)
    {
        if (string.IsNullOrWhiteSpace(expectedState))
            return Task.FromResult<object>(new { error = "expectedState is required (e.g. 'campaign_map', 'mission', 'game_menu')" });

        if (pollIntervalMs < 100) pollIntervalMs = 100;
        if (pollIntervalMs > 5000) pollIntervalMs = 5000;

        return Task.Run<object>(async () =>
        {
            var startTime = DateTime.UtcNow;
            var maxWait = TimeSpan.FromSeconds(120);

            while (DateTime.UtcNow - startTime < maxWait)
            {
                var current = await MainThreadDispatcher.EnqueueAsync(() =>
                {
                    if (Campaign.Current != null)
                    {
                        if (Mission.Current != null)
                            return "mission";
                        if (Campaign.Current.GameMenuManager?.NextGameMenuId != null)
                            return "game_menu";
                        return "campaign_map";
                    }

                    return GameStateManager.Current?.ActiveState?.GetType().Name ?? "unknown";
                });

                if (current.IndexOf(expectedState, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // State string matched, but verify the UI is not still on a loading screen.
                    // Campaign.Current becomes non-null before GameLoadingScreen finishes transitioning.
                    var isLoading = await MainThreadDispatcher.EnqueueAsync(() =>
                    {
                        var screenName = ScreenManager.TopScreen?.GetType().Name;
                        return screenName != null && screenName.Contains("Loading", StringComparison.OrdinalIgnoreCase);
                    });

                    if (!isLoading)
                    {
                        return new
                        {
                            /// Whether the expected state was reached
                            matched = true,
                            /// Current game state
                            state = current,
                            /// Milliseconds spent waiting
                            waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
                        };
                    }
                }

                await Task.Delay(pollIntervalMs);
            }

            var finalState = await MainThreadDispatcher.EnqueueAsync(() =>
            {
                if (Campaign.Current != null)
                {
                    if (Mission.Current != null) return "mission";
                    if (Campaign.Current.GameMenuManager?.NextGameMenuId != null) return "game_menu";
                    return "campaign_map";
                }
                return GameStateManager.Current?.ActiveState?.GetType().Name ?? "unknown";
            });

            return new
            {
                /// Whether the expected state was reached
                matched = false,
                /// Current game state
                state = finalState,
                /// Milliseconds spent waiting
                waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
            };
        });
    }

    [Tool("core/get_campaign_time", Description = "Get the current in-game campaign date and time.")]
    public partial Task<object> GetCampaignTime()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            var now = CampaignTime.Now;
            return new
            {
                /// Full time string
                currentTime = now.ToString(),
                /// Current year
                year = (int) now.GetYear,
                /// Current season (Spring, Summer, Autumn, Winter)
                season = CampaignTime.Now.GetSeasonOfYear.ToString(),
                /// Day within the current season
                dayOfSeason = now.GetDayOfSeason,
                /// Hour of the day (0-23)
                hourOfDay = now.GetHourOfDay,
            };
        });
    }

    [Tool("core/get_time_speed", Description = "Get the current campaign map time control mode and speed.")]
    public partial Task<object> GetTimeSpeed()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            return new
            {
                /// Time control mode enum value (Stop, StoppablePlay, StoppableFastForward, etc.)
                timeControlMode = Campaign.Current.TimeControlMode.ToString(),
                /// Whether the game is paused
                isPaused = Campaign.Current.TimeControlMode == CampaignTimeControlMode.Stop,
                /// Whether fast-forward is active
                speedUp = Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppableFastForward
                          || Campaign.Current.TimeControlMode == CampaignTimeControlMode.StoppableFastForward
                          || Campaign.Current.TimeControlMode == CampaignTimeControlMode.FastForwardStop
                          || Campaign.Current.TimeControlMode == CampaignTimeControlMode.UnstoppablePlay,
            };
        });
    }

    [Tool("core/set_time_speed", Description = "Set the campaign map time speed (0 = pause, 1-4 = speed multiplier).")]
    public partial Task<object> SetTimeSpeed(
        [ToolParameter(Description = "Speed value: 0 to pause, 1-4 for game speed")] int speed)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current == null)
                return new { error = "No active campaign" };

            if (speed is < 0 or > 4)
                return new { error = "Speed must be between 0 and 4" };

            if (speed == 0)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
            else
            {
                Campaign.Current.TimeControlMode = speed >= 3
                    ? CampaignTimeControlMode.UnstoppableFastForward
                    : CampaignTimeControlMode.StoppableFastForward;
                Campaign.Current.SetTimeSpeed(speed);
            }

            return new
            {
                /// The speed value that was set
                speed,
                /// Resulting time control mode
                timeControlMode = Campaign.Current.TimeControlMode.ToString(),
            };
        });
    }

    [Tool("core/set_cheat_mode", Description = "Enable or disable cheat mode. Many console commands require cheat mode to be enabled.")]
    public partial Task<object> SetCheatMode(
        [ToolParameter(Description = "true to enable, false to disable")] bool enabled)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Game.Current == null)
                return new { error = "No active game" };

            // CheatMode flows: Game.CheatMode -> MBGameManager.CheatMode -> NativeConfig.CheatMode
            // NativeConfig is in TaleWorlds.Engine; use reflection to set it
            var nativeConfigType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return []; } })
                .FirstOrDefault(t => t.FullName == "TaleWorlds.Engine.NativeConfig");

            if (nativeConfigType == null)
                return new { error = "Could not find NativeConfig type" };

            var cheatProp = nativeConfigType.GetProperty("CheatMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (cheatProp != null && cheatProp.CanWrite)
            {
                cheatProp.SetValue(null, enabled);
            }
            else
            {
                // Try setting backing field directly
                var field = nativeConfigType.GetField("_cheatMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            ?? nativeConfigType.GetField("<CheatMode>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (field == null)
                    return new { error = "Could not find CheatMode field on NativeConfig" };

                field.SetValue(null, enabled);
            }

            return new
            {
                /// Current cheat mode state
                cheatMode = Game.Current.CheatMode,
            };
        });
    }

    [Tool("core/run_command", Description = "Execute a Bannerlord console command (e.g. 'campaign.add_gold_to_hero 1000'). Most 'campaign.*' commands require cheat mode to be enabled first — use set_cheat_mode to enable it.")]
    public partial Task<object> RunCommand(
        [ToolParameter(Description = "Full command string, e.g. 'campaign.add_gold_to_hero 1000'")] string command)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (string.IsNullOrWhiteSpace(command))
                return new { error = "Command cannot be empty" };

            // Split command into name and args (CallFunction takes concatenated args string)
            var parts = command.Split([' '], 2);
            var commandName = parts[0];
            var args = parts.Length > 1 ? parts[1] : "";

            var result = CommandLineFunctionality.CallFunction(commandName, args, out var found);

            return new
            {
                /// Whether the command was found
                found,
                /// The command name that was executed
                command = commandName,
                /// Command output text
                result = result?.Trim(),
            };
        });
    }

    [Tool("core/list_commands", Description = "List available Bannerlord console commands, optionally filtered by prefix.")]
    public partial Task<object> ListCommands(
        [ToolParameter(Description = "Optional prefix filter, e.g. 'campaign' to list only campaign.* commands", Required = false)] string? prefix)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            // CollectCommandLineFunctions() adds to a static Dictionary without clearing it first,
            // so calling it a second time throws "An item with the same key has already been added."
            // Read AllFunctions directly if it is already populated; only collect on the first call.
            List<string>? commands;
            if (AllFunctionsField?.Invoke() is { Count: > 0 } allFunctions)
            {
                commands = allFunctions.Keys.Cast<string>().ToList();
            }
            else
            {
                commands = CommandLineFunctionality.CollectCommandLineFunctions();
                // Clear the dict - if the game didn't call yet, it will at some point.
                allFunctions = AllFunctionsField?.Invoke();
                allFunctions?.Clear();
            }

            IEnumerable<string> filtered = commands;
            if (!string.IsNullOrWhiteSpace(prefix))
                filtered = commands.Where(c => c.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            var list = filtered.OrderBy(c => c).ToList();

            return new
            {
                /// Number of commands returned
                count = list.Count,
                /// Array of command name strings
                commands = list,
            };
        });
    }

    [Tool("core/new_game", Description = "Start a new sandbox campaign from the main menu. Triggers the character creation flow. Use ui/get_screen and ui/click_widget to navigate through character creation screens. Returns immediately — use ui/wait_for_screen to track screen transitions.")]
    public partial Task<object> NewGame()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (Campaign.Current != null)
                return new { error = "A campaign is already active. Return to the main menu first." };

            var activeState = GameStateManager.Current?.ActiveState;
            if (activeState == null || activeState.GetType().Name != "InitialState")
                return new { error = $"Not at the main menu. Current state: {activeState?.GetType().Name ?? "null"}" };

            // Find the SandBox initial state option and invoke its action
            var options = Module.CurrentModule.GetInitialStateOptions();
            InitialStateOption? sandboxOption = null;
            var availableIds = new List<string>();

            foreach (var option in options)
            {
                availableIds.Add($"{option.Id} ({option.Name})");
                if (option.Id.IndexOf("Sandbox", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    option.Name?.ToString()?.IndexOf("SandBox", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    sandboxOption = option;
                }
            }

            if (sandboxOption == null)
                return new { error = $"SandBox option not found in initial menu. Available: {string.Join(", ", availableIds)}" };

            var (isDisabled, disabledReason) = sandboxOption.IsDisabledAndReason();
            if (isDisabled)
                return new { error = $"SandBox option is disabled: {disabledReason}" };

            sandboxOption.DoAction();

            return new
            {
                /// Status message
                message = "Starting new sandbox campaign — character creation will begin shortly",
                /// The option that was invoked
                optionId = sandboxOption.Id,
            };
        });
    }

    [Tool("core/list_saves", Description = "List available save game files.")]
    public partial Task<object> ListSaves()
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            var saveNames = MBSaveLoad.GetSaveFileNames();
            if (saveNames == null || saveNames.Length == 0)
                return new
                {
                    /// Number of save files
                    count = 0,
                    /// Array of save file name strings
                    saves = Array.Empty<string>(),
                    /// Currently active save slot name
                    activeSave = (string?) null,
                };

            return new
            {
                /// Number of save files
                count = saveNames.Length,
                /// Array of save file name strings
                saves = saveNames,
                /// Currently active save slot name
#if v1313 || v1315
                activeSave = MBSaveLoad.ActiveSaveSlotName,
#else
                activeSave = ActiveSaveSlotNameField?.Invoke(),
#endif
            };
        });
    }

    [Tool("core/load_save", Description = "Load a save game by name. Returns immediately — use core/wait_for_state (expectedState='campaign_map') to block until loading completes.")]
    public partial Task<object> LoadSave(
        [ToolParameter(Description = "Save file name (from list_saves)")] string saveName)
    {
        return MainThreadDispatcher.EnqueueAsync<object>(() =>
        {
            if (string.IsNullOrWhiteSpace(saveName))
                return new { error = "Save name cannot be empty" };

            if (!MBSaveLoad.IsSaveGameFileExists(saveName))
                return new { error = $"Save file not found: {saveName}" };

            var loadResult = MBSaveLoad.LoadSaveGameData(saveName);
            if (loadResult == null)
                return new { error = $"Failed to load save data: {saveName}" };

            MBGameManager.StartNewGame(new SandBoxGameManager(loadResult));

            return new
            {
                /// Status message
                message = $"Loading save: {saveName}",
            };
        });
    }

    [Tool("core/save_game", Description = "Save the current game with a given name. Returns immediately — use core/wait_for_save to block until the save file is written.")]
    public partial Task<object> SaveGame(
        [ToolParameter(Description = "Save file name")] string saveName)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            return Task.FromResult<object>(new { error = "Save name cannot be empty" });

        // SaveAs blocks the main thread (serialization + I/O) and deadlocks when called
        // inside ProcessQueue because it may re-enter the game loop or wait for tick events.
        // Schedule it outside the dispatcher queue via the deferred save mechanism.
        MainThreadDispatcher.ScheduleSave(saveName);

        return Task.FromResult<object>(new
        {
            /// Status message
            message = $"Saving game as: {saveName}",
        });
    }

    [Tool("core/wait_for_save", Description = "Wait until a save file appears in the save list. Use after core/save_game to confirm the save completed. Pass timeout via games.call_tool timeout parameter.")]
    public partial Task<object> WaitForSave(
        [ToolParameter(Description = "Save file name to wait for")] string saveName,
        [ToolParameter(Description = "Poll interval in milliseconds (default 1000).", Required = false)] int pollIntervalMs = 1000)
    {
        if (string.IsNullOrWhiteSpace(saveName))
            return Task.FromResult<object>(new { error = "saveName is required" });

        if (pollIntervalMs < 500) pollIntervalMs = 500;
        if (pollIntervalMs > 5000) pollIntervalMs = 5000;

        return Task.Run<object>(async () =>
        {
            var startTime = DateTime.UtcNow;
            var maxWait = TimeSpan.FromSeconds(60);

            while (DateTime.UtcNow - startTime < maxWait)
            {
                var found = await MainThreadDispatcher.EnqueueAsync(() =>
                    MBSaveLoad.IsSaveGameFileExists(saveName));

                if (found)
                {
                    return new
                    {
                        /// Whether the save file was found
                        found = true,
                        /// Save file name
                        saveName,
                        /// Milliseconds spent waiting
                        waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
                    };
                }

                await Task.Delay(pollIntervalMs);
            }

            return new
            {
                /// Whether the save file was found
                found = false,
                /// Save file name
                saveName,
                /// Milliseconds spent waiting
                waitedMs = (int) (DateTime.UtcNow - startTime).TotalMilliseconds,
            };
        });
    }
}