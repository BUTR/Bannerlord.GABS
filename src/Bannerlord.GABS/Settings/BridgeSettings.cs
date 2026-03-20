using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace Bannerlord.GABS.Settings;

public class BridgeSettings : AttributeGlobalSettings<BridgeSettings>
{
    public override string Id => "Bannerlord.GABS_v1";
    public override string DisplayName => "Bannerlord GABS";
    public override string FolderName => "Bannerlord.GABS";
    public override string FormatType => "json2";

    [SettingPropertyGroup("Server")]
    [SettingPropertyInteger("Port", 1024, 65535, HintText = "GABP server listen port", Order = 0, RequireRestart = true)]
    public int Port { get; set; } = SubModule.DefaultPort;

    [SettingPropertyGroup("Server")]
    [SettingPropertyBool("Auto-Start", HintText = "Start server automatically when game loads", Order = 1, RequireRestart = true)]
    public bool AutoStart { get; set; } = true;

    [SettingPropertyGroup("Events")]
    [SettingPropertyBool("Push Events", HintText = "Push campaign events to connected GABS clients", Order = 0, RequireRestart = false)]
    public bool EnableEvents { get; set; } = true;
}