param(
    [Parameter(Mandatory = $false)]
    [switch]$Quiet
)

if(!(Get-Module lordmilko.BuildTools))
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {
        Write-Host "Installing lordmilko.BuildTools..." -NoNewline -ForegroundColor Magenta

        Register-PackageSource -Name AppveyorBuildToolsNuGet -Location https://ci.appveyor.com/nuget/buildtools-j7nyox2i4tis -ProviderName PowerShellGet | Out-Null

        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source AppveyorBuildToolsNuGet -ErrorAction Stop | Out-Null

        Unregister-PackageSource -Name AppveyorBuildToolsNuGet

        Write-Host "Done!" -ForegroundColor Magenta
    }
    
    Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet