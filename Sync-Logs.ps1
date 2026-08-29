$ErrorActionPreference = "Continue"

$projectRoot = "C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer"

$gameLog = "B:\SteamLibrary\steamapps\common\Supermarket Simulator\BepInEx\LogOutput.log"

$pluginRoot = "B:\SteamLibrary\steamapps\common\Supermarket Simulator\BepInEx\plugins\CurrencyAssetAnalyzer"

$logsDirectory = "$projectRoot\Logs"

New-Item `
    -ItemType Directory `
    -Path $logsDirectory `
    -Force |
    Out-Null

$source1 = $gameLog
$destination1 = "$logsDirectory\LogOutput.log"

$source2 = "$pluginRoot\BrazilianMoneyTextureReplacer\BrazilianMoneyTextureReplacerReport.txt"
$destination2 = "$logsDirectory\BrazilianMoneyTextureReplacerReport.txt"

if (Test-Path $source1)
{
    Copy-Item `
        -Path $source1 `
        -Destination $destination1 `
        -Force

    Write-Host "SINCRONIZADO: LogOutput.log"
}
else
{
    Write-Warning "NAO ENCONTRADO: $source1"
}

if (Test-Path $source2)
{
    Copy-Item `
        -Path $source2 `
        -Destination $destination2 `
        -Force

    Write-Host "SINCRONIZADO: BrazilianMoneyTextureReplacerReport.txt"
}
else
{
    Write-Warning "NAO ENCONTRADO: $source2"
}

Write-Host ""
Write-Host "========================================"
Write-Host "LOGS SINCRONIZADOS"
Write-Host "========================================"

Get-ChildItem `
    -Path $logsDirectory `
    -File |
    Where-Object {
        $_.Name -eq "LogOutput.log" -or
        $_.Name -eq "BrazilianMoneyTextureReplacerReport.txt"
    } |
    Select-Object Name, Length, LastWriteTime
