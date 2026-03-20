# Setup Guide

How to install and configure Bannerlord.GABS so an AI agent can play Mount & Blade II: Bannerlord.

## Prerequisites

- Mount & Blade II: Bannerlord (Steam, GOG, or Epic)
- An MCP-compatible AI client (Claude, Codex, local, etc.)

## 1. Install GABS

GABS is the Go binary that bridges MCP (AI side) and GABP (game side).

### Download Pre-built Binary (Recommended)

Download the latest release for your platform from [github.com/pardeike/gabs/releases](https://github.com/pardeike/gabs/releases).

Extract the binary and add it to your PATH:

```powershell
# Windows example — move to a directory in your PATH
Move-Item gabs.exe "$env:USERPROFILE\go\bin\gabs.exe"
```

### Build from Source (Alternative)

Requires Go 1.21+:

```powershell
go install github.com/pardeike/gabs/cmd/gabs@latest
```

### Verify

```powershell
gabs version
```

## 2. Install BLSE (Recommended)

[BLSE](https://github.com/BUTR/Bannerlord.BLSE) (Bannerlord Software Extender) adds improved exception handling and assembly resolution. Download from [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/1) and extract to your game directory.

The launch script uses `Bannerlord.BLSE.Standalone.exe` (CLI mode). You can also use `Bannerlord.exe` directly — it accepts the same module list argument, but BLSE improves stability.

## 3. Configure a Bannerlord Game

GABS launches the game via a PowerShell script. The repo includes `launch-bannerlord.ps1` which is **required** because Bannerlord tracks whether the game exited cleanly. When GABS force-kills the game via `games.kill`, the crash sentinel gets set. On the next launch, Bannerlord shows a "Safe Mode" dialog that blocks execution and waits for user input — this breaks headless operation entirely. The script resets this sentinel before launching.

The script uses `Bannerlord.BLSE.Standalone.exe` by default. To use vanilla `Bannerlord.exe` instead, edit the `$exe` variable.

Edit the script to match your game install path, then configure GABS:

```powershell
gabs games add bannerlord
```

Or edit the config directly at `~/.config/gabs/config.json` (Linux/Mac) or `%APPDATA%\gabs\config.json` (Windows):

```json
{
  "games": {
    "bannerlord": {
      "launchTarget": "powershell",
      "launchArgs": [
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        "C:\\path\\to\\launch-bannerlord.ps1"
      ],
      "gabpPort": 4825
    }
  }
}
```

Verify:

```powershell
gabs games show bannerlord
```

## 4. Install the Bannerlord Mod

### Pre-built Release (Recommended)

Download Bannerlord.GABS from [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/10419) or [GitHub Releases](https://github.com/BUTR/Bannerlord.GABS/releases).

Extract to your game's `Modules/` directory:

```
<GameDir>/Modules/Bannerlord.GABS/
├── SubModule.xml
├── bin/
│   └── Win64_Shipping_Client/
│       ├── Bannerlord.GABS.v1.3.15.dll
│       ├── Lib.GAB.dll
│       └── ...
```

Also install the dependencies the same way (from NexusMods):
- [Bannerlord.Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)
- [Bannerlord.ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018)
- [Bannerlord.MCM](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) (optional but recommended)

No launcher step needed — the launch script (`launch-bannerlord.ps1`) specifies the module list directly, so mods are loaded automatically.

### Build from Source (Alternative)

Set the game path environment variable:

```powershell
# For Steam beta branch (most common):
[System.Environment]::SetEnvironmentVariable("BANNERLORD_BETA_DIR", "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord", "User")

# Or for stable branch:
[System.Environment]::SetEnvironmentVariable("BANNERLORD_STABLE_DIR", "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord", "User")

# Or generic fallback:
[System.Environment]::SetEnvironmentVariable("BANNERLORD_GAME_DIR", "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord", "User")
```

Build and deploy:

```powershell
cd src
dotnet build Bannerlord.GABS/Bannerlord.GABS.csproj -c Beta_Debug
```

The SDK automatically copies the built module to `<GameDir>/Modules/Bannerlord.GABS/`.

## 5. Add GABS as an MCP Server

### Claude

The MCP server configuration depends on your client. For Claude Code, add to your VS Code settings or Claude config:

```json
{
  "mcpServers": {
    "gabs": {
      "command": "gabs",
      "args": ["server"]
    }
  }
}
```

If your GABS config is in a non-default location:

```json
{
  "mcpServers": {
    "gabs": {
      "command": "gabs",
      "args": ["server", "--configDir", "/path/to/config"]
    }
  }
}
```

## 6. Verify the Setup

Once your AI client is running with GABS as an MCP server:

```
games.list                                          → should show "bannerlord"
games.status { gameId: "bannerlord" }               → should show "stopped"
games.start { gameId: "bannerlord" }                → launches the game
games.connect { gameId: "bannerlord" }              → connects to the mod's GABP server
games.tool_names { gameId: "bannerlord", limit: 5 } → should list game-specific tools
```

If `games.connect` fails, the mod may not have started its GABP server yet. Wait for the game to reach the main menu, then retry.

## 7. Start Playing

Load a save and start interacting:

```
bannerlord.core.load_save { saveName: "your_save" }
bannerlord.core.wait_for_state { expectedState: "campaign_map" }
bannerlord.hero.get_player {}
```

See `docs/gameplay/getting-started.md` for detailed workflows and tool usage.

## Troubleshooting

| Problem                                        | Cause                                | Fix                                                                      |
| ---------------------------------------------- | ------------------------------------ | ------------------------------------------------------------------------ |
| `games.start` fails                            | Wrong launch target                  | Check `games.show bannerlord` — verify launch script path                |
| `games.start` fails                            | "Safe Mode" dialog                   | Run `launch-bannerlord.ps1` manually once — it resets the crash sentinel |
| `games.connect` fails                          | Mod not loaded or game still loading | Ensure BLSE + mod are installed, wait for main menu, retry connect       |
| `games.connect` fails                          | Port mismatch                        | Check mod's MCM settings (default: 4825) matches GABS config             |
| `games.tool_names` returns empty               | GABP not connected                   | Call `games.connect` first                                               |
| Tools return "No active campaign"              | No save loaded                       | Call `core/load_save` first                                              |
| Build fails with missing refs                  | Game path env var not set            | Set `BANNERLORD_BETA_DIR` or `BANNERLORD_GAME_DIR`                       |
| Build fails with "supported-game-versions.txt" | File missing                         | Create it in the project root with the game version (e.g. `v1.3.15`)     |
