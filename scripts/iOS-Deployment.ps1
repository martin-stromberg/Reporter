<#
    iOS-Deployment.ps1

    Build und Deployment des .NET MAUI iOS-Teils von Reporter.

    Moegliche Aktionen:
        build     -> Signiertes .ipa erzeugen (device, Release)
        simulator -> App bauen und im iOS-Simulator starten
        device    -> App bauen und auf einem echten iOS-Geraet starten
        list      -> Verfuegbare Simulatoren/Geraete anzeigen (nur macOS)
        menu      -> Interaktives Menue

    Windows mit Pair-to-Mac:
        -ServerAddress, -ServerUser, -ServerPassword setzen (oder Umgebungsvariablen)
        -_DotNetRootRemoteDirectory ist fuer VS 2022:
            /Users/<user>/Library/Caches/Xamarin/XMA/SDKs/dotnet/
         (fuer VS 2026:
            /Users/<user>/Library/Caches/maui/PairToMac/SDKs/dotnet/)

    Umgebungsvariablen:
        IOS_CODESIGN_KEY
        IOS_PROVISIONING_PROFILE
        IOS_MAC_SERVER_ADDRESS
        IOS_MAC_SERVER_USER
        IOS_MAC_SERVER_PASSWORD
        IOS_MAC_DOTNET_ROOT
#>

param(
    [ValidateSet("build", "simulator", "device", "list", "menu")]
    [string]$Action = "menu",

    [Parameter(HelpMessage = "UDID oder Name des Simulators / Geraets. Beispiel Simulator: E25BBE37-69BA-4720-B6FD-D54C97791E79")]
    [string]$Device = "",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "",

    [Parameter(HelpMessage = "iossimulator-arm64 | iossimulator-x64 | ios-arm64")]
    [string]$RuntimeIdentifier = "",

    [string]$ServerAddress = $env:IOS_MAC_SERVER_ADDRESS,
    [string]$ServerUser = $env:IOS_MAC_SERVER_USER,
    [string]$ServerPassword = $env:IOS_MAC_SERVER_PASSWORD,
    [string]$TcpPort = "58181",
    [string]$DotNetRootRemoteDirectory = $env:IOS_MAC_DOTNET_ROOT,

    [string]$CodesignKey = $env:IOS_CODESIGN_KEY,
    [string]$CodesignProvision = $env:IOS_PROVISIONING_PROFILE,
    [string]$CodesignEntitlements = "",

    [switch]$NoPrompt
)

$projectPath = "src/Reporter/Reporter.csproj"
$framework = "net10.0-ios"

$isWindows = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$isMacOS = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)

function Get-RuntimeIdentifier {
    param([string]$Action)

    if ($RuntimeIdentifier) { return $RuntimeIdentifier }
    if ($Action -eq "simulator") {
        if ($isMacOS) {
            $arch = & uname -m
            if ($arch -eq "arm64") { return "iossimulator-arm64" }
        }
        return "iossimulator-x64"
    }
    return "ios-arm64"
}

function Get-Configuration {
    param([string]$Action)
    if ($Configuration) { return $Configuration }
    if ($Action -eq "simulator") { return "Debug" }
    return "Release"
}

function Get-PairToMacArgs {
    if ($isMacOS -or -not $ServerAddress) { return @() }

    $args = @(
        "-p:ServerAddress=$ServerAddress",
        "-p:ServerUser=$ServerUser",
        "-p:TcpPort=$TcpPort"
    )
    if ($ServerPassword) { $args += "-p:ServerPassword=$ServerPassword" }
    if ($DotNetRootRemoteDirectory) { $args += "-p:_DotNetRootRemoteDirectory=$DotNetRootRemoteDirectory" }
    return $args
}

function Assert-PairToMacAvailable {
    if ($isMacOS) { return }
    if (-not $ServerAddress) {
        Write-Host "Fehler: Auf Windows wird ein Pair-to-Mac Build-Host benoetigt." -ForegroundColor Red
        Write-Host "Setze -ServerAddress / -ServerUser / -ServerPassword oder die Umgebungsvariablen:" -ForegroundColor Yellow
        Write-Host "IOS_MAC_SERVER_ADDRESS, IOS_MAC_SERVER_USER, IOS_MAC_SERVER_PASSWORD" -ForegroundColor Yellow
        exit 1
    }
}

function Assert-CodesigningForAction {
    param([string]$Action)
    if ($Action -eq "simulator") { return }
    if (-not $CodesignKey) {
        Write-Host "Fehler: -CodesignKey bzw. IOS_CODESIGN_KEY ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
    if (-not $CodesignProvision) {
        Write-Host "Fehler: -CodesignProvision bzw. IOS_PROVISIONING_PROFILE ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
}

function Invoke-Build {
    $rid = Get-RuntimeIdentifier -Action "build"
    $config = Get-Configuration -Action "build"
    $pairArgs = Get-PairToMacArgs
    $buildArgs = @(
        "publish", $projectPath,
        "-f", $framework,
        "-c", $config,
        "-p:RuntimeIdentifier=$rid",
        "-p:ArchiveOnBuild=true"
    )
    $buildArgs += $pairArgs
    if ($CodesignKey) { $buildArgs += "-p:CodesignKey=$CodesignKey" }
    if ($CodesignProvision) { $buildArgs += "-p:CodesignProvision=$CodesignProvision" }
    if ($CodesignEntitlements) { $buildArgs += "-p:CodesignEntitlements=$CodesignEntitlements" }

    Write-Host "Erzeuge signiertes iOS-Kompilat (.ipa)..." -ForegroundColor Cyan
    Write-Host "dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
    & dotnet @buildArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build fehlgeschlagen." -ForegroundColor Red
        exit 1
    }

    $ipa = Get-ChildItem -Path "src/Reporter/bin/$config/$framework/$rid" -Recurse -Filter "*.ipa" | Select-Object -First 1
    if (-not $ipa) {
        Write-Host "Keine IPA-Datei gefunden." -ForegroundColor Red
        exit 1
    }
    Write-Host "IPA gefunden: $($ipa.FullName)" -ForegroundColor Green
}

function Invoke-Run {
    param([string]$Action)
    $rid = Get-RuntimeIdentifier -Action $Action
    $config = Get-Configuration -Action $Action
    $pairArgs = Get-PairToMacArgs
    $buildArgs = @(
        "build", $projectPath,
        "-t:Run",
        "-f", $framework,
        "-c", $config,
        "-p:RuntimeIdentifier=$rid"
    )
    $buildArgs += $pairArgs

    if ($Action -eq "simulator") {
        if ($Device) {
            $udid = $Device
            if (-not $udid.StartsWith(":v2:udid=")) { $udid = ":v2:udid=$udid" }
            $buildArgs += "-p:_DeviceName=$udid"
        }
    }
    elseif ($Action -eq "device") {
        if (-not $Device) {
            Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID) angegeben werden." -ForegroundColor Red
            exit 1
        }
        $buildArgs += "-p:_DeviceName=$Device"
        if ($CodesignKey) { $buildArgs += "-p:CodesignKey=$CodesignKey" }
        if ($CodesignProvision) { $buildArgs += "-p:CodesignProvision=$CodesignProvision" }
        if ($CodesignEntitlements) { $buildArgs += "-p:CodesignEntitlements=$CodesignEntitlements" }
    }

    Write-Host "Starte iOS $Action ..." -ForegroundColor Cyan
    Write-Host "dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
    & dotnet @buildArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "$Action fehlgeschlagen." -ForegroundColor Red
        exit 1
    }
    Write-Host "iOS $Action erfolgreich." -ForegroundColor Green
}

function Invoke-List {
    if ($isMacOS) {
        Write-Host "Verfuegbare iOS-Simulatoren:" -ForegroundColor Cyan
        & xcrun simctl list devices
    }
    else {
        Write-Host "Auflistung von Simulatoren/Geraeten ist nur auf macOS verfuegbar." -ForegroundColor Yellow
        Write-Host "Auf Windows koennen die Geraete im Xcode-Fenster des Pair-to-Mac Build-Hosts eingesehen werden." -ForegroundColor Yellow
    }
}

function Show-Menu {
    Write-Host ""
    Write-Host "==============================="
    Write-Host "   iOS Build & Deployment Menue"
    Write-Host "==============================="
    Write-Host "1) Nur Build (.ipa erzeugen)"
    Write-Host "2) Build + iOS-Simulator starten"
    Write-Host "3) Build + echtes Geraet deployen"
    Write-Host "4) Simulatoren/Geraete anzeigen"
    Write-Host "==============================="
    $choice = Read-Host "Bitte waehlen (1-4)"

    switch ($choice) {
        "1" { $script:Action = "build" }
        "2" { $script:Action = "simulator" }
        "3" { $script:Action = "device" }
        "4" { $script:Action = "list" }
        default {
            Write-Host "Ungueltige Auswahl." -ForegroundColor Red
            exit 1
        }
    }
}

if ($Action -eq "menu" -and -not $NoPrompt) { Show-Menu }

switch ($Action) {
    "build" {
        Assert-PairToMacAvailable
        Assert-CodesigningForAction -Action "build"
        Invoke-Build
    }
    "simulator" {
        Assert-PairToMacAvailable
        Invoke-Run -Action "simulator"
    }
    "device" {
        Assert-PairToMacAvailable
        Assert-CodesigningForAction -Action "device"
        Invoke-Run -Action "device"
    }
    "list" { Invoke-List }
    default {
        Write-Host "Unbekannte Aktion: $Action" -ForegroundColor Red
        exit 1
    }
}

exit 0
