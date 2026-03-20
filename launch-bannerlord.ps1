# Bannerlord GABS Launch Script
# Resets the crash sentinel to prevent the "Safe Mode" dialog after unexpected shutdowns

$configPath = Join-Path $env:USERPROFILE "Documents\Mount and Blade II Bannerlord\Configs\engine_config.txt"

if (Test-Path $configPath) {
    $content = Get-Content $configPath -Raw
    if ($content -match 'safely_exited\s*=\s*0') {
        $content = $content -replace 'safely_exited\s*=\s*0', 'safely_exited = 1'
        Set-Content $configPath $content -NoNewline
        Write-Host "[GABS] Reset safely_exited flag to prevent Safe Mode dialog"
    }
}

# Launch the game
$gameDir = "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client"
$exe = Join-Path $gameDir "Bannerlord.BLSE.Standalone.exe"
$modules = "_MODULES_*Bannerlord.Harmony*Bannerlord.ButterLib*Bannerlord.UIExtenderEx*Bannerlord.MBOptionScreen*Bannerlord.GABS*Native*SandBoxCore*CustomBattle*Sandbox*StoryMode*_MODULES_"

Set-Location $gameDir
& $exe /singleplayer $modules
