param()
$ErrorActionPreference = 'Stop'
$tideProjectRoot = Split-Path -Parent $PSScriptRoot
$tideEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $tideEditor)) { throw 'Unity 6000.3.27f1 with iOS Build Support is required.' }
New-Item -ItemType Directory -Force -Path (Join-Path $tideProjectRoot 'BuildLogs') | Out-Null
$tideLog = Join-Path $tideProjectRoot 'BuildLogs\unity-ios-export.log'
$tideArgs = @('-batchmode','-quit','-buildTarget','iOS','-projectPath',('"'+$tideProjectRoot+'"'),'-executeMethod','SpiritTide.EditorTools.TideBuilder.ExportIOS','-logFile',('"'+$tideLog+'"'))
$tideProcess = Start-Process -FilePath $tideEditor -ArgumentList $tideArgs -WindowStyle Hidden -PassThru
$tideProcess.WaitForExit()
if ($tideProcess.ExitCode -ne 0) { throw "Unity iOS export failed. See $tideLog" }
Get-Content -LiteralPath (Join-Path $tideProjectRoot 'BuildLogs\ios-export.json')
