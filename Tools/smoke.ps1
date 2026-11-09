param([int]$Width = 1560, [int]$Height = 720, [switch]$PhoneLayout, [switch]$EffectsOnly, [switch]$IconsOnly, [switch]$JourneyOnly, [switch]$MotionOnly, [switch]$RequestsOnly)
$ErrorActionPreference = 'Stop'
$bleachProjectRoot = Split-Path -Parent $PSScriptRoot
$bleachPlayer = Join-Path $bleachProjectRoot 'Builds\Windows\SpiritTide.exe'
if (-not (Test-Path -LiteralPath $bleachPlayer)) { throw 'Run Tools/build.ps1 first.' }
$bleachOutput = Join-Path $bleachProjectRoot ('BuildLogs\PlayerChecks-' + (Get-Date -Format 'yyyyMMdd-HHmmssfff') + '-' + $PID)
New-Item -ItemType Directory -Path $bleachOutput -Force | Out-Null
$bleachArgs = @('-screen-fullscreen','0','-screen-width',$Width,'-screen-height',$Height,'-logFile',('"' + (Join-Path $bleachOutput 'player.log') + '"'),'-spirit-smoke',('"' + $bleachOutput + '"'))
if ($PhoneLayout) { $bleachArgs += '-spirit-phone-layout' }
if ($EffectsOnly) { $bleachArgs += '-spirit-fx-review' }
if ($IconsOnly) { $bleachArgs += '-spirit-icons-review' }
if ($JourneyOnly) { $bleachArgs += '-spirit-journey-review' }
if ($MotionOnly) { $bleachArgs += '-spirit-motion-review' }
if ($RequestsOnly) { $bleachArgs += '-spirit-requests-review' }
$bleachPlayerProcess = Start-Process -FilePath $bleachPlayer -ArgumentList $bleachArgs -WindowStyle Hidden -PassThru
$bleachDeadline = (Get-Date).AddSeconds(900)
while (-not $bleachPlayerProcess.WaitForExit(1000) -and (Get-Date) -lt $bleachDeadline) {}
if (-not $bleachPlayerProcess.HasExited) {
    Stop-Process -Id $bleachPlayerProcess.Id -ErrorAction SilentlyContinue
    throw "Player smoke timeout. See $bleachOutput"
}
$bleachReportPath = Join-Path $bleachOutput 'report.json'
if (-not (Test-Path -LiteralPath $bleachReportPath)) { throw "Player produced no report. See $bleachOutput" }
$bleachReport = Get-Content -LiteralPath $bleachReportPath -Raw | ConvertFrom-Json
Write-Output ([pscustomobject]@{passed=$bleachReport.passed; checks=$bleachReport.checks.Count; device=$bleachReport.device; folder=$bleachOutput; errors=$bleachReport.errors} | ConvertTo-Json -Depth 4)
if ($bleachPlayerProcess.ExitCode -ne 0 -or -not $bleachReport.passed) { throw "Player checks failed. See $bleachOutput" }
