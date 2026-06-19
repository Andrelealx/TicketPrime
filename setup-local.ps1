param(
    [switch]$AutoInstall,
    [switch]$SkipRun,
    [switch]$SkipNpmInstall
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "[setup] $Message" -ForegroundColor Cyan
}

function Test-Tool {
    param([string]$CommandName)
    return [bool](Get-Command $CommandName -ErrorAction SilentlyContinue)
}

function Refresh-PathFromSystem {
    $machinePath = [System.Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [System.Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machinePath;$userPath"
}

function Install-WithWinget {
    param(
        [string]$Id,
        [string]$Label
    )

    if (-not (Test-Tool "winget")) {
        throw "winget nao encontrado. Instale o App Installer da Microsoft Store para continuar."
    }

    Write-Step "Instalando $Label via winget..."
    winget install --id $Id --exact --silent --accept-source-agreements --accept-package-agreements
}

function Ensure-Requirement {
    param(
        [string]$ToolName,
        [string]$Label,
        [string]$WingetId,
        [ScriptBlock]$Check
    )

    $ok = & $Check
    if ($ok) {
        Write-Step "$Label OK."
        return
    }

    if (-not $AutoInstall) {
        throw "$Label nao encontrado. Rode novamente com -AutoInstall ou instale manualmente."
    }

    Install-WithWinget -Id $WingetId -Label $Label
    Refresh-PathFromSystem

    $ok = & $Check
    if (-not $ok) {
        throw "$Label ainda nao esta disponivel no terminal. Feche e abra o PowerShell e rode o script novamente."
    }

    Write-Step "$Label instalado com sucesso."
}

function Ensure-Dotnet10 {
    if (-not (Test-Tool "dotnet")) {
        return $false
    }

    $versionText = (& dotnet --version).Trim()
    if (-not $versionText) {
        return $false
    }

    $majorText = $versionText.Split(".")[0]
    $major = 0
    if (-not [int]::TryParse($majorText, [ref]$major)) {
        return $false
    }

    return $major -ge 10
}

function Ensure-DockerRunning {
    Write-Step "Validando Docker..."

    $dockerReady = $false
    try {
        docker info | Out-Null
        $dockerReady = $true
    } catch {
        $dockerReady = $false
    }

    if ($dockerReady) {
        Write-Step "Docker em execucao."
        return
    }

    $dockerDesktopExe = Join-Path $env:ProgramFiles "Docker\Docker\Docker Desktop.exe"
    if (Test-Path $dockerDesktopExe) {
        Write-Step "Iniciando Docker Desktop..."
        Start-Process $dockerDesktopExe | Out-Null
    } else {
        throw "Docker Desktop nao encontrado em '$dockerDesktopExe'."
    }

    Write-Step "Aguardando Docker subir..."
    for ($i = 1; $i -le 90; $i++) {
        Start-Sleep -Seconds 2
        try {
            docker info | Out-Null
            Write-Step "Docker pronto."
            return
        } catch {
            # continua aguardando
        }
    }

    throw "Docker nao ficou pronto a tempo. Abra o Docker Desktop manualmente e rode novamente."
}

Set-Location -Path $PSScriptRoot

Write-Step "Iniciando setup local..."

Ensure-Requirement -ToolName "git" -Label "Git" -WingetId "Git.Git" -Check { Test-Tool "git" }
Ensure-Requirement -ToolName "node" -Label "Node.js" -WingetId "OpenJS.NodeJS.LTS" -Check { Test-Tool "node" }
Ensure-Requirement -ToolName "npm" -Label "npm" -WingetId "OpenJS.NodeJS.LTS" -Check { Test-Tool "npm" }
Ensure-Requirement -ToolName "dotnet" -Label ".NET SDK 10+" -WingetId "Microsoft.DotNet.SDK.10" -Check { Ensure-Dotnet10 }
Ensure-Requirement -ToolName "docker" -Label "Docker Desktop" -WingetId "Docker.DockerDesktop" -Check { Test-Tool "docker" }

Ensure-DockerRunning

if (-not $SkipNpmInstall) {
    Write-Step "Executando npm install..."
    npm install
}

if ($SkipRun) {
    Write-Step "Setup concluido. Para subir tudo rode: npm run dev"
    exit 0
}

Write-Step "Subindo ambiente completo..."
npm run dev
