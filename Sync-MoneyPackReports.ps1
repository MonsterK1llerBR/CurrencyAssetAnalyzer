$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

$SourceDirectory = "B:\SteamLibrary\steamapps\common\Supermarket Simulator\BepInEx\plugins\CurrencyAssetAnalyzer\AnalyzerV7\MoneyPack"

$DestinationDirectory = Join-Path $ProjectRoot "Reports\MoneyPack"

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " CurrencyAssetAnalyzer - Sync MoneyPack" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $SourceDirectory)) {
    Write-Host "ERRO: diretório de origem não encontrado:" -ForegroundColor Red
    Write-Host $SourceDirectory -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $DestinationDirectory)) {
    Write-Host "Criando diretório de destino..." -ForegroundColor Yellow

    New-Item `
        -ItemType Directory `
        -Force `
        -Path $DestinationDirectory | Out-Null
}

$Files = Get-ChildItem `
    -Path $SourceDirectory `
    -File `
    -Filter "*.txt"

if ($Files.Count -eq 0) {
    Write-Host "Nenhum relatório .txt encontrado." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Execute o jogo e faça o CurrencyAssetAnalyzer gerar os relatórios." -ForegroundColor Yellow
    exit 0
}

Write-Host "Origem:" -ForegroundColor DarkGray
Write-Host $SourceDirectory
Write-Host ""

Write-Host "Destino:" -ForegroundColor DarkGray
Write-Host $DestinationDirectory
Write-Host ""

Write-Host "Relatórios encontrados: $($Files.Count)" -ForegroundColor Green
Write-Host ""

foreach ($File in $Files) {

    $DestinationFile = Join-Path `
        $DestinationDirectory `
        $File.Name

    Copy-Item `
        -Path $File.FullName `
        -Destination $DestinationFile `
        -Force

    Write-Host "[OK] $($File.Name)" -ForegroundColor Green
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " Sincronização concluída." -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Arquivos atualmente no repositório:" -ForegroundColor Cyan
Write-Host ""

Get-ChildItem `
    -Path $DestinationDirectory `
    -File `
    -Filter "*.txt" |
    Sort-Object Name |
    Select-Object Name, Length |
    Format-Table -AutoSize

Write-Host ""
Write-Host "Agora abra o GitHub Desktop." -ForegroundColor Yellow
Write-Host "Os arquivos modificados aparecerão em Changes." -ForegroundColor Yellow
Write-Host ""