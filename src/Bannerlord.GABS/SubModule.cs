using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.GABS.Behaviors;
using Bannerlord.GABS.Patches;
using Bannerlord.GABS.Settings;
using Bannerlord.GABS.Tools;

using HarmonyLib;

using Lib.GAB.Server;

using System;
using System.Diagnostics;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.GABS;

public class SubModule : MBSubModuleBase
{
    internal const int DefaultPort = 4825;

    private Harmony? _harmony;
    private GabpServer? _server;

    private static void Log(string message)
    {
        Trace.TraceInformation(message);
    }

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();
        Log("OnSubModuleLoad called");

        try
        {
            _harmony = new Harmony("Bannerlord.GABS");
            _harmony.PatchAll(typeof(SubModule).Assembly);
            Log("Harmony patches applied");

#if v1313 || v1315
                // Manually patch Incident system (random events) — types are in module assemblies (v1.3.x+)
                IncidentPatches.Apply(_harmony);
#endif

            // Verify patches
            foreach (var method in _harmony.GetPatchedMethods())
            {
                Log($"  Patched: {method.DeclaringType?.FullName}.{method.Name}");
            }
        }
        catch (Exception ex)
        {
            Log($"Harmony patch FAILED: {ex}");
        }
    }

    protected override void OnSubModuleUnloaded()
    {
        base.OnSubModuleUnloaded();
        StopServer();
        _harmony?.UnpatchAll("Bannerlord.GABS");
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
        base.OnBeforeInitialModuleScreenSetAsRoot();
        Log("OnBeforeInitialModuleScreenSetAsRoot called");
        StartServer();
    }

    protected override void OnApplicationTick(float dt)
    {
        base.OnApplicationTick(dt);
        MainThreadDispatcher.ProcessQueue();
        MainThreadDispatcher.ProcessPendingSave();
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);

        if (gameStarterObject is CampaignGameStarter campaignStarter && _server != null)
        {
            Log("Registering event behaviors...");
            var behaviors = new BridgeEventBehaviorBase[]
            {
                new CampaignLifecycleEventBehavior(_server.Events),
                new CombatEventBehavior(_server.Events),
                new DiplomacyEventBehavior(_server.Events),
                new HeroEventBehavior(_server.Events),
                new MilitaryEventBehavior(_server.Events),
                new QuestEventBehavior(_server.Events),
                new SettlementEventBehavior(_server.Events),
                new PlayerNavigationEventBehavior(_server.Events),
                new UIEventBehavior(_server.Events),
            };
            foreach (var behavior in behaviors)
            {
                behavior.RegisterChannels();
                campaignStarter.AddBehavior(behavior);
            }
            Log("Event behaviors registered");

            // Wire up BarterTools state tracking
            if (Campaign.Current?.BarterManager != null)
            {
                Campaign.Current.BarterManager.BarterBegin += Tools.BarterTools.OnBarterBegin;
                Campaign.Current.BarterManager.Closed += Tools.BarterTools.OnBarterClosed;
            }
        }
    }

    private void StartServer()
    {
        if (_server != null) return;

        try
        {
            Log($"GABS env: GABP_SERVER_PORT={Environment.GetEnvironmentVariable("GABP_SERVER_PORT")}, GABP_TOKEN={Environment.GetEnvironmentVariable("GABP_TOKEN")?.Substring(0, Math.Min(4, Environment.GetEnvironmentVariable("GABP_TOKEN")?.Length ?? 0))}..., GABS_GAME_ID={Environment.GetEnvironmentVariable("GABS_GAME_ID")}");
            Log($"IsRunningUnderGabs={Lib.GAB.Gabp.IsRunningUnderGabs()}");

            var settings = BridgeSettings.Instance;
            Log($"BridgeSettings.Instance is null: {settings == null}");
            var port = settings?.Port ?? DefaultPort;
            var autoStart = settings?.AutoStart ?? true;

            if (!autoStart) { Log("AutoStart disabled, skipping"); return; }

            var moduleInfo = ModuleInfoHelper.GetModuleByType(typeof(SubModule));
            var modVersion = moduleInfo?.Version.ToString() ?? "unknown";
            Log($"Creating server on port {port}...");
            _server = Lib.GAB.Gabp.CreateGabsAwareServerWithInstance("Bannerlord", modVersion, new CoreTools(), fallbackPort: port);
            Log($"Server created, registering tools...");

            // Phase 2: Read tools
            _server.Tools.RegisterToolsFromInstance(new HeroTools());
            _server.Tools.RegisterToolsFromInstance(new PartyTools());
            _server.Tools.RegisterToolsFromInstance(new SettlementTools());
            _server.Tools.RegisterToolsFromInstance(new KingdomTools());
            _server.Tools.RegisterToolsFromInstance(new QuestTools());
            _server.Tools.RegisterToolsFromInstance(new HistoryTools());

            // Phase 3: Action tools
            _server.Tools.RegisterToolsFromInstance(new InventoryTools());
            _server.Tools.RegisterToolsFromInstance(new DiplomacyTools());
            _server.Tools.RegisterToolsFromInstance(new ConversationTools());
            _server.Tools.RegisterToolsFromInstance(new MenuTools());

            // Phase 5: Battle/Mission tools
            Log("Registering BattleTools...");
            _server.Tools.RegisterToolsFromInstance(new BattleTools());

            // UI tools (inquiry popups — state tracked via Harmony patches in InquiryPatches.cs)
            Log("Registering InquiryTools...");
            _server.Tools.RegisterToolsFromInstance(new InquiryTools());

            // Barter tools
            _server.Tools.RegisterToolsFromInstance(new BarterTools());

            // GauntletUI tools (generic screen/widget interaction)
            Log("Registering GauntletUITools...");
            _server.Tools.RegisterToolsFromInstance(new GauntletUITools());

            // Screenshot tools
            _server.Tools.RegisterToolsFromInstance(new ScreenshotTools());

            // Mission tools (3D scene interaction)
            _server.Tools.RegisterToolsFromInstance(new MissionTools());

            Log($"Tools registered, starting...");

            _server.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log($"StartAsync FAULTED: {task.Exception}");
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[GABS] Failed to start: {task.Exception?.InnerException?.Message}",
                        Colors.Red));
                }
                else
                {
                    MapScreenFocusPatch.IsEnabled = true;
                    Log($"StartAsync SUCCESS, listening on port {_server.Port}, token={_server.Token?.Substring(0, Math.Min(4, _server.Token?.Length ?? 0))}...");
                    if (Lib.GAB.Gabp.IsRunningUnderGabs())
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"[GABS] Connected to GABS on port {_server.Port}",
                            Colors.Green));
                    }
                    else
                    {
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"[GABS] Listening on port {_server.Port}",
                            Colors.Green));
                    }
                }
            });
        }
        catch (Exception ex)
        {
            Log($"StartServer EXCEPTION: {ex}");
            InformationManager.DisplayMessage(new InformationMessage(
                $"[GABS] Init failed: {ex.Message}", Colors.Red));
        }
    }

    private void StopServer()
    {
        if (_server == null) return;

        MapScreenFocusPatch.IsEnabled = false;

        try
        {
            _server.StopAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"[GABS] Error stopping: {task.Exception?.InnerException?.Message}",
                        Colors.Red));
                }
            });
            _server.Dispose();
            _server = null;
        }
        catch (Exception ex)
        {
            InformationManager.DisplayMessage(new InformationMessage(
                $"[GABS] Error disposing: {ex.Message}", Colors.Red));
        }
    }
}