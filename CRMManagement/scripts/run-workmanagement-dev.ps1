Param(
  [string]$EnvFile = "C:\MainFolder\Dev\MainLdataBrain\YAMLFile\.env",
  [string]$DbHost = "localhost",
  [int]$DbPort = 5432,
  [string]$MainProjectBaseUrl = "",
  [string]$LaunchProfile = "https",
  [switch]$UseUserSecrets,
  [switch]$EnableGitHub,
  [string]$GitHubClientId = "",
  [string]$GitHubClientSecret = "",
  [object]$StopExisting = $true
)

$ErrorActionPreference = 'Stop'

function Reset-StaleCRMManagementWebBuildArtifacts {
  param(
    [Parameter(Mandatory=$true)][string]$CRMManagementRoot
  )

  $webProjDir = Join-Path $CRMManagementRoot 'CRMManagement.Web'
  $objDir = Join-Path $webProjDir 'obj'
  $binDir = Join-Path $webProjDir 'bin'

  if (-not (Test-Path -LiteralPath $objDir)) {
    return
  }

  $suspect = $false
  try {
    $manifests = Get-ChildItem -LiteralPath $objDir -Recurse -File -Filter 'staticwebassets*.json' -ErrorAction SilentlyContinue
    foreach ($m in $manifests) {
      if (Select-String -LiteralPath $m.FullName -SimpleMatch -Quiet -Pattern '\CRMManagement1\CRMManagement.Web\') {
        $suspect = $true
        break
      }
    }
  } catch {
    $suspect = $false
  }

  if ($suspect) {
    Write-Host "[CRMManagement] Detected stale static web assets manifests pointing at 'CRMManagement1'. Cleaning CRMManagement.Web bin/obj..." -ForegroundColor Yellow
    if (Test-Path -LiteralPath $objDir) { Remove-Item -LiteralPath $objDir -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path -LiteralPath $binDir) { Remove-Item -LiteralPath $binDir -Recurse -Force -ErrorAction SilentlyContinue }
  }
}

function Ensure-MainProjectRunning {
  param(
    [Parameter(Mandatory=$true)][string]$CRMManagementRoot,
    [string]$MainProjectProjectPath = "..\MainProject\MainProject.csproj",
    [string]$LaunchProfile = "RazorBasicAuthCRUD",
    [int]$HttpsPort = 5001
  )

  try {
    $ok = (Test-NetConnection -ComputerName "localhost" -Port $HttpsPort -WarningAction SilentlyContinue).TcpTestSucceeded
    if ($ok) {
      Write-Host "[CRMManagement] MainProject already listening on https://localhost:$HttpsPort" -ForegroundColor DarkGray
      return
    }

    Write-Host "[CRMManagement] MainProject not detected on :$HttpsPort. Starting it in background..." -ForegroundColor Yellow

    $projectFullPath = $MainProjectProjectPath
    try {
      $projectFullPath = (Resolve-Path -LiteralPath (Join-Path $CRMManagementRoot $MainProjectProjectPath)).Path
    } catch {
      # leave as-is; dotnet will report a useful error
    }

    $projectDir = $CRMManagementRoot
    try {
      $projectDir = Split-Path -Parent $projectFullPath
    } catch {
      $projectDir = $CRMManagementRoot
    }

    $args = @(
      'run',
      '--project', $projectFullPath,
      '--launch-profile', $LaunchProfile
    )

    Start-Process -FilePath "dotnet" -ArgumentList $args -WorkingDirectory $projectDir -WindowStyle Minimized | Out-Null
    Start-Sleep -Milliseconds 600
  }
  catch {
    Write-Host "[CRMManagement] Warning: failed to auto-start MainProject: $($_.Exception.Message)" -ForegroundColor Yellow
  }
}

function Get-DotEnvValue {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Key
  )

  if (-not (Test-Path -LiteralPath $Path)) {
    throw "Env file not found: $Path"
  }

  $line = Get-Content -LiteralPath $Path |
    Where-Object { $_ -match "^\s*$([Regex]::Escape($Key))\s*=" } |
    Select-Object -First 1

  if (-not $line) {
    return $null
  }

  $value = ($line -split "=", 2)[1]
  if ($null -eq $value) { return $null }

  $value = $value.Trim()
  if ($value.StartsWith('"') -and $value.EndsWith('"')) { $value = $value.Substring(1, $value.Length-2) }
  if ($value.StartsWith("'") -and $value.EndsWith("'")) { $value = $value.Substring(1, $value.Length-2) }

  return $value
}

function Test-PortListening {
  param(
    [Parameter(Mandatory=$true)][int]$Port
  )

  $listener = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
  return $null -ne $listener
}

function Get-AvailablePort {
  param(
    [Parameter(Mandatory=$true)][int]$StartPort
  )

  $port = $StartPort
  while (Test-PortListening -Port $port) {
    $port++
  }

  return $port
}

Write-Host "[CRMManagement] Loading DB settings from $EnvFile" -ForegroundColor Cyan

$stopExistingBool = $true
if ($StopExisting -is [bool]) {
  $stopExistingBool = $StopExisting
} elseif ($StopExisting -is [string]) {
  $stopExistingBool = @('true','1','yes','y','$true','on') -contains $StopExisting.Trim().ToLowerInvariant()
} elseif ($null -ne $StopExisting) {
  try { $stopExistingBool = [bool]$StopExisting } catch { $stopExistingBool = $true }
}

if ($stopExistingBool) {
  $existing = Get-Process -Name 'CRMManagement.Web' -ErrorAction SilentlyContinue
  if ($existing) {
    Write-Host "[CRMManagement] Stopping existing CRMManagement.Web process (PID $($existing.Id)) to avoid locked DLL build errors..." -ForegroundColor Yellow
    $existing | Stop-Process -Force
    Start-Sleep -Milliseconds 400
  }
}

$dbName = Get-DotEnvValue -Path $EnvFile -Key 'APP_DB_NAME'
$dbUser = Get-DotEnvValue -Path $EnvFile -Key 'APP_DB_USER'
$dbPass = Get-DotEnvValue -Path $EnvFile -Key 'APP_DB_PASSWORD'

# Optional: enable MainProject screen linking (Projects/Users) from .env
$mpBase = if (-not [string]::IsNullOrWhiteSpace($MainProjectBaseUrl)) { $MainProjectBaseUrl } else { (Get-DotEnvValue -Path $EnvFile -Key 'MAINPROJECT_BASE_URL') }

if ([string]::IsNullOrWhiteSpace($dbName)) { $dbName = 'ldatabrain' }
if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = 'ldataapp' }
if ([string]::IsNullOrWhiteSpace($dbPass)) { throw "APP_DB_PASSWORD is missing in $EnvFile" }

Write-Host "[CRMManagement] Loaded DB user '$dbUser' and password (length $($dbPass.Length))" -ForegroundColor Cyan

# Always prefer explicit local host/port for dev.
$env:DB_HOST = $DbHost
$env:DB_PORT = "$DbPort"
$env:APP_DB_NAME = $dbName
$env:APP_DB_USER = $dbUser
$env:APP_DB_PASSWORD = $dbPass

if (-not [string]::IsNullOrWhiteSpace($mpBase)) {
  $env:MainProject__BaseUrl = $mpBase
  Write-Host "[CRMManagement] MainProject linking ENABLED: $mpBase" -ForegroundColor Cyan

  # Start MainProject automatically (needed for embedded screens).
  $wmRoot = (Split-Path -Parent $PSScriptRoot)
  Ensure-MainProjectRunning -CRMManagementRoot $wmRoot
} else {
  # Explicitly clear for this session
  Remove-Item Env:MainProject__BaseUrl -ErrorAction SilentlyContinue
  Write-Host "[CRMManagement] MainProject linking disabled (MainProject__BaseUrl not set)." -ForegroundColor DarkGray
}

Write-Host "[CRMManagement] Testing TCP connectivity to ${DbHost}:${DbPort}" -ForegroundColor Cyan
$tcp = Test-NetConnection -ComputerName $DbHost -Port $DbPort
if (-not $tcp.TcpTestSucceeded) {
  throw "Cannot connect to Postgres at ${DbHost}:${DbPort} (TcpTestSucceeded=False). Is Docker Postgres running and port-mapped?"
}

$cs = "Host=$DbHost;Port=$DbPort;Database=$dbName;Username=$dbUser;Password=$dbPass;Search Path=public"

# Also set the standard .NET connection string env var so running from VS/launch profiles still works.
$env:ConnectionStrings__Default = $cs

# Optional: enable GitHub "Code" integration via env vars.
if ($EnableGitHub -or (-not [string]::IsNullOrWhiteSpace($GitHubClientId)) -or (-not [string]::IsNullOrWhiteSpace($GitHubClientSecret))) {
  if ([string]::IsNullOrWhiteSpace($GitHubClientId) -or [string]::IsNullOrWhiteSpace($GitHubClientSecret)) {
    throw "To enable GitHub integration, provide both -GitHubClientId and -GitHubClientSecret (or omit -EnableGitHub)."
  }

  $env:Authentication__GitHub__Enabled = 'true'
  $env:Authentication__GitHub__ClientId = $GitHubClientId
  $env:Authentication__GitHub__ClientSecret = $GitHubClientSecret
  Write-Host "[CRMManagement] GitHub integration ENABLED (Authentication:GitHub)." -ForegroundColor Cyan
} else {
  Remove-Item Env:Authentication__GitHub__Enabled -ErrorAction SilentlyContinue
  Remove-Item Env:Authentication__GitHub__ClientId -ErrorAction SilentlyContinue
  Remove-Item Env:Authentication__GitHub__ClientSecret -ErrorAction SilentlyContinue
  Write-Host "[CRMManagement] GitHub integration disabled." -ForegroundColor DarkGray
}

if ($UseUserSecrets) {
  Write-Host "[CRMManagement] Setting user-secrets ConnectionStrings:Default (local only)" -ForegroundColor Cyan
  dotnet user-secrets init --project .\CRMManagement.Web | Out-Null
  dotnet user-secrets set "ConnectionStrings:Default" $cs --project .\CRMManagement.Web | Out-Null
}

Write-Host "[CRMManagement] Starting app..." -ForegroundColor Green
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)

$wmRoot = (Split-Path -Parent $PSScriptRoot)
Reset-StaleCRMManagementWebBuildArtifacts -CRMManagementRoot $wmRoot

$urlsOverride = $null
$profileLower = if ([string]::IsNullOrWhiteSpace($LaunchProfile)) { "" } else { $LaunchProfile.Trim().ToLowerInvariant() }

if ($profileLower -eq "http") {
  if (Test-PortListening -Port 5064) {
    $httpPort = Get-AvailablePort -StartPort 5065
    $urlsOverride = "http://localhost:$httpPort"
    Write-Host "[CRMManagement] Port 5064 is in use. Using $urlsOverride" -ForegroundColor Yellow
  }
}
elseif ($profileLower -eq "https") {
  $httpPort = if (Test-PortListening -Port 5064) { Get-AvailablePort -StartPort 5065 } else { 5064 }
  $httpsPort = if (Test-PortListening -Port 7007) { Get-AvailablePort -StartPort 7008 } else { 7007 }

  if ($httpPort -ne 5064 -or $httpsPort -ne 7007) {
    $urlsOverride = "https://localhost:$httpsPort;http://localhost:$httpPort"
    Write-Host "[CRMManagement] Default launch ports are busy. Using $urlsOverride" -ForegroundColor Yellow
  }
}

if ([string]::IsNullOrWhiteSpace($LaunchProfile)) {
  if ([string]::IsNullOrWhiteSpace($urlsOverride)) {
    dotnet run --project .\CRMManagement.Web\CRMManagement.Web.csproj
  } else {
    dotnet run --project .\CRMManagement.Web\CRMManagement.Web.csproj --urls $urlsOverride
  }
} else {
  Write-Host "[CRMManagement] Using launch profile '$LaunchProfile'" -ForegroundColor DarkGray
  if ([string]::IsNullOrWhiteSpace($urlsOverride)) {
    dotnet run --project .\CRMManagement.Web\CRMManagement.Web.csproj --launch-profile $LaunchProfile
  } else {
    dotnet run --project .\CRMManagement.Web\CRMManagement.Web.csproj --launch-profile $LaunchProfile --urls $urlsOverride
  }
}
