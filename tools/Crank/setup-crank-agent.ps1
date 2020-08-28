#!/usr/bin/pwsh

param (
    [bool]$InstallDotNet = $true,
    [bool]$InstallCrankAgent = $true
)

$ErrorActionPreference = 'Stop'

#region Utilities

function InstallDotNet {
    if ($IsWindows) {
        Invoke-WebRequest 'https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1' -OutFile 'dotnet-install.ps1'
        ./dotnet-install.ps1
    } else {
        # From https://docs.microsoft.com/dotnet/core/install/linux-ubuntu#install-the-sdk
        sudo apt-get update
        sudo apt-get install -y apt-transport-https
        sudo apt-get update
        sudo apt-get install -y dotnet-sdk-3.1
    }
}

function InstallCrankAgent {
    if ($IsWindows) {
        dotnet tool install --tool-path c:\dotnet-tools Microsoft.Crank.Agent --version "0.1.0-*"
    } else {
        dotnet tool install -g Microsoft.Crank.Agent --version "0.1.0-*"
    }
}

function ScheduleCrankAgentStartWindows($RunScriptPath, [pscredential]$Credential) {
    $action = New-ScheduledTaskAction -Execute 'pwsh.exe' `
                  -Argument "-NoProfile -WindowStyle Hidden -File $RunScriptPath"

    $trigger = New-ScheduledTaskTrigger -AtStartup

    $auth =
        if ($Credential) {
            @{
                User = $Credential.UserName
                Password = $Credential.GetNetworkCredential().Password
            }
        } else {
            @{
                Principal = New-ScheduledTaskPrincipal -UserID "NT AUTHORITY\NETWORKSERVICE" `
                                -LogonType ServiceAccount -RunLevel Highest
            }
        }

    $null = Register-ScheduledTask `
                -TaskName "CrankAgent" -Description "Start crank-agent" `
                -Action $action -Trigger $trigger `
                @auth
}

function ScheduleCrankAgentStartLinux($RunScriptPath) {
    $currentCrontabContent = (crontab -l) ?? $null
    if (-not ($currentCrontabContent -match '\bcrank-agent\b')) {
        $currentCrontabContent, "@reboot $RunScriptPath" | crontab -
    }
}

function ScheduleCrankAgentStart {
    $scriptPath = Join-Path -Path (Split-Path $PSCommandPath -Parent) -ChildPath 'run-crank-agent.ps1'

    if ($IsWindows) {
        ScheduleCrankAgentStartWindows -RunScriptPath $scriptPath -Credential (Get-Credential)
    } else {
        ScheduleCrankAgentStartLinux -RunScriptPath $scriptPath
    }
}

#endregion

#region Main

if ($InstallDotNet) { InstallDotNet }
if ($InstallCrankAgent) { InstallCrankAgent }
ScheduleCrankAgentStart

#endregion
