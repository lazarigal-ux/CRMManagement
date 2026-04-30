[CmdletBinding()]
param(
  [ValidateSet('Debug','Release')]
  [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$logsDir = Join-Path $repoRoot 'qa-logs'
New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$logPath = Join-Path $logsDir "market-readiness-$stamp.log"

function Write-Section([string]$title) {
  "`n=== $title ===`n" | Tee-Object -FilePath $logPath -Append
}

function Run([string]$title, [string]$command) {
  Write-Section $title
  "> $command" | Tee-Object -FilePath $logPath -Append
  Invoke-Expression $command 2>&1 | Tee-Object -FilePath $logPath -Append
}

try {
  Write-Section 'Environment'
  "Repo: $repoRoot" | Tee-Object -FilePath $logPath -Append
  "Time: $(Get-Date -Format o)" | Tee-Object -FilePath $logPath -Append
  Run 'dotnet --info' 'dotnet --info'

  Run "Build solution ($Configuration)" "dotnet build .\\CRMManagement.sln -c $Configuration"

  Run "Run all tests ($Configuration)" "dotnet test .\\CRMManagement.sln -c $Configuration -v minimal"

  Run 'Run QA tests (verbose minimal)' 'dotnet test .\\CRMManagement.QaTests\\CRMManagement.QaTests.csproj -c Release -v minimal'

  Run 'Dependency audit: vulnerable (include transitive)' 'dotnet list .\\CRMManagement.sln package --vulnerable --include-transitive'
  Run 'Dependency audit: outdated' 'dotnet list .\\CRMManagement.sln package --outdated'

  $publishOut = Join-Path $logsDir "publish-$stamp"
  # IMPORTANT: PowerShell escaping uses backtick, not backslash. Use explicit quoting to ensure
  # the -o argument is parsed correctly (otherwise it may resolve to the drive root).
  Run "Publish Web ($Configuration)" ('dotnet publish .\\CRMManagement.Web\\CRMManagement.Web.csproj -c {0} -o "{1}"' -f $Configuration, $publishOut)

  Write-Section 'Lightweight secrets scan (repo content)'
  $patterns = @(
    'Admin123!',
    'Password=',
    'ClientSecret',
    'BEGIN PRIVATE KEY',
    'Bearer '
  )

  $include = @('*.cs','*.cshtml','*.json','*.yml','*.yaml','*.ps1','*.md')
  $excludeDirs = @('bin','obj','.git','qa-logs','node_modules')

  $files = Get-ChildItem -Path $repoRoot -Recurse -File -Include $include |
    Where-Object {
      $p = $_.FullName
      foreach ($d in $excludeDirs) {
        if ($p -match ([regex]::Escape([IO.Path]::DirectorySeparatorChar + $d + [IO.Path]::DirectorySeparatorChar))) { return $false }
      }
      return $true
    }

  foreach ($pat in $patterns) {
    "-- Pattern: $pat" | Tee-Object -FilePath $logPath -Append
    $matches = $files | Select-String -Pattern $pat -SimpleMatch -ErrorAction SilentlyContinue
    if ($matches) {
      $matches | ForEach-Object { $_.ToString() } | Tee-Object -FilePath $logPath -Append
    } else {
      "(no matches)" | Tee-Object -FilePath $logPath -Append
    }
  }

  Write-Section 'Result'
  'OK: All readiness checks completed.' | Tee-Object -FilePath $logPath -Append
  "Log: $logPath" | Tee-Object -FilePath $logPath -Append

  Write-Host "Market readiness checks completed. Log: $logPath"
  exit 0
}
catch {
  Write-Section 'FAILED'
  $_ | Out-String | Tee-Object -FilePath $logPath -Append
  "Log: $logPath" | Tee-Object -FilePath $logPath -Append

  Write-Error "Market readiness checks FAILED. See log: $logPath"
  exit 1
}
