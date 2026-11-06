param([switch]$ValidateOnly)
$ErrorActionPreference = 'Stop'
$bleachProjectRoot = Split-Path -Parent $PSScriptRoot
$bleachEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $bleachEditor)) { throw 'Unity 6000.3.27f1 is required.' }
New-Item -ItemType Directory -Force -Path (Join-Path $bleachProjectRoot 'BuildLogs') | Out-Null
$bleachMethod = if ($ValidateOnly) {'SpiritTide.EditorTools.TideBuilder.ValidateOnly'} else {'SpiritTide.EditorTools.TideBuilder.PrepareAndBuild'}
$bleachLog = Join-Path $bleachProjectRoot 'BuildLogs\unity-build.log'
$bleachArguments = @('-batchmode','-quit','-projectPath',('"' + $bleachProjectRoot + '"'),'-executeMethod',$bleachMethod,'-logFile',('"' + $bleachLog + '"'))
$bleachProcess = Start-Process -FilePath $bleachEditor -ArgumentList $bleachArguments -WindowStyle Hidden -PassThru
$bleachProcess.WaitForExit()
if ($bleachProcess.ExitCode -ne 0) { throw "Unity failed ($($bleachProcess.ExitCode)). See $bleachLog" }
$bleachModelReport = Get-Content -LiteralPath (Join-Path $bleachProjectRoot 'BuildLogs\model-validation.json') -Raw | ConvertFrom-Json
Write-Output ([pscustomobject]@{passed=$bleachModelReport.passed; modelChecks=$bleachModelReport.checks.Count; log=$bleachLog} | ConvertTo-Json)
