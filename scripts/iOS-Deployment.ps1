<#
    iOS-Deployment.ps1

    Build und Deployment des .NET MAUI iOS-Teils von Reporter.

    Moegliche Aktionen:
        build     -> iOS-App bauen (mit Codesigning: .ipa)
        simulator -> App bauen und im iOS-Simulator starten (nur auf macOS)
        device    -> App bauen und auf einem echten iOS-Geraet starten (nur auf macOS)
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
    [Parameter(HelpMessage = "Der macOS-Kurzname (z. B. martin), nicht der volle Benutzername.")]
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

# Default fuer den Remote-.NET-Pfad auf dem Mac (VS 2022 Pair-to-Mac Cache).
# VS 2026 / neuere Pair-to-Mac-Versionen nutzen ggf.
# /Users/<user>/Library/Caches/maui/PairToMac/SDKs/dotnet/
if ($isWindows -and $ServerUser -and -not $DotNetRootRemoteDirectory) {
    $DotNetRootRemoteDirectory = "/Users/$ServerUser/Library/Caches/maui/PairToMac/SDKs/dotnet/"
}

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

function Assert-PairToMacAvailable {
    if ($isMacOS) { return }
    if (-not $ServerAddress) {
        Write-Host "Fehler: Auf Windows wird ein Pair-to-Mac Build-Host benoetigt." -ForegroundColor Red
        Write-Host "Setze -ServerAddress / -ServerUser / -ServerPassword oder die Umgebungsvariablen:" -ForegroundColor Yellow
        Write-Host "IOS_MAC_SERVER_ADDRESS, IOS_MAC_SERVER_USER, IOS_MAC_SERVER_PASSWORD" -ForegroundColor Yellow
        exit 1
    }
    if ($ServerUser -match '\s') {
        Write-Host "Warnung: -ServerUser enthaelt ein Leerzeichen." -ForegroundColor Yellow
        Write-Host "Pair-to-Mac erwartet den macOS-Kurznamen (z. B. 'martin'), nicht den vollstaendigen Namen." -ForegroundColor Yellow
        Write-Host "Wenn der Pfad _DotNetRootRemoteDirectory ($DotNetRootRemoteDirectory) falsch ist, setze IOS_MAC_DOTNET_ROOT." -ForegroundColor Yellow
    }
}

function Assert-CodesigningForAction {
    param([string]$Action)
    if ($Action -eq "simulator" -or $Action -eq "build") { return }
    if (-not $CodesignKey) {
        Write-Host "Fehler: -CodesignKey bzw. IOS_CODESIGN_KEY ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
    if (-not $CodesignProvision) {
        Write-Host "Fehler: -CodesignProvision bzw. IOS_PROVISIONING_PROFILE ist nicht gesetzt." -ForegroundColor Red
        exit 1
    }
}

function Add-PropertyLine {
    param(
        [System.Collections.Generic.List[string]]$List,
        [string]$Name,
        [string]$Value
    )
    if (-not $Value) { return }
    $List.Add("-p:$Name=`"$Value`"")
}

function Add-MaskedDisplay {
    param(
        [System.Collections.Generic.List[string]]$List,
        [string]$Name,
        [string]$Value
    )
    if (-not $Value) { return }
    if ($Name -eq "ServerPassword") {
        $List.Add("-p:$Name=***")
    }
    else {
        $List.Add("-p:$Name=`"$Value`"")
    }
}

function New-ResponseFile {
    param([string[]]$Lines)
    $rspName = ".ios-deploy-$([Guid]::NewGuid().ToString('n')).rsp"
    $rspPath = Join-Path (Get-Location).Path $rspName
    [System.IO.File]::WriteAllLines($rspPath, $Lines)
    return $rspPath
}

function Invoke-Build {
    $rid = Get-RuntimeIdentifier -Action "build"
    $config = Get-Configuration -Action "build"
    $hasCodesigning = $CodesignKey -and $CodesignProvision

    $rspLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $rspLines -Name "RuntimeIdentifier" -Value $rid

    if ($isWindows) {
        Add-PropertyLine -List $rspLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $rspLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $rspLines -Name "TcpPort" -Value $TcpPort
        Add-PropertyLine -List $rspLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $rspLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    $displayLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $displayLines -Name "RuntimeIdentifier" -Value $rid
    if ($isWindows) {
        Add-PropertyLine -List $displayLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $displayLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $displayLines -Name "TcpPort" -Value $TcpPort
        Add-MaskedDisplay -List $displayLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $displayLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    if ($hasCodesigning) {
        $verb = "publish"
        Add-PropertyLine -List $rspLines -Name "ArchiveOnBuild" -Value "true"
        Add-PropertyLine -List $rspLines -Name "CodesignKey" -Value $CodesignKey
        Add-PropertyLine -List $rspLines -Name "CodesignProvision" -Value $CodesignProvision
        Add-PropertyLine -List $rspLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        Add-PropertyLine -List $displayLines -Name "ArchiveOnBuild" -Value "true"
        Add-PropertyLine -List $displayLines -Name "CodesignKey" -Value $CodesignKey
        Add-PropertyLine -List $displayLines -Name "CodesignProvision" -Value $CodesignProvision
        Add-PropertyLine -List $displayLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        Write-Host "Erzeuge signiertes iOS-Kompilat (.ipa)..." -ForegroundColor Cyan
    }
    else {
        $verb = "build"
        Write-Host "Baue iOS-App ohne Codesigning..." -ForegroundColor Cyan
    }

    $rspPath = New-ResponseFile -Lines $rspLines
    try {
        Write-Host "dotnet $verb $projectPath -f $framework -c $config @$rspPath" -ForegroundColor Gray
        $baseArgs = @($verb, $projectPath, "-f", $framework, "-c", $config, "@$rspPath")
        $displayArgs = @($verb, $projectPath, "-f", $framework, "-c", $config) + $displayLines
        Write-Host "Eigenschaften:" -ForegroundColor Gray
        foreach ($line in $displayLines) { Write-Host "   $line" -ForegroundColor Gray }

        & dotnet @baseArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Build fehlgeschlagen (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
            exit 1
        }

        if ($hasCodesigning) {
            $ipa = Get-ChildItem -Path "src/Reporter/bin/$config/$framework/$rid" -Recurse -Filter "*.ipa" | Select-Object -First 1
            if (-not $ipa) {
                Write-Host "Keine IPA-Datei gefunden." -ForegroundColor Red
                exit 1
            }
            Write-Host "IPA gefunden: $($ipa.FullName)" -ForegroundColor Green
        }
        else {
            Write-Host "Build erfolgreich (ohne Codesigning, keine .ipa)." -ForegroundColor Green
        }
    }
    finally {
        Remove-Item -Path $rspPath -ErrorAction SilentlyContinue
    }
}

function Invoke-Run {
    param([string]$Action)
    if ($isWindows) {
        Write-Host "Fehler: iOS-Simulator-/Geraete-Deployment via 'dotnet build -t:Run' wird von Microsoft auf Windows nicht unterstuetzt." -ForegroundColor Red
        Write-Host "Moeglichkeiten:" -ForegroundColor Yellow
        Write-Host "  1) Skript auf dem Mac ausfuehren: ./scripts/iOS-Deployment.ps1 -Action $Action" -ForegroundColor Yellow
        Write-Host "  2) Visual Studio verwenden." -ForegroundColor Yellow
        Write-Host "  3) Zuerst 'build' ausfuehren und die .app/.ipa manuell auf dem Mac deployen." -ForegroundColor Yellow
        exit 1
    }
    $rid = Get-RuntimeIdentifier -Action $Action
    $config = Get-Configuration -Action $Action

    $rspLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $rspLines -Name "RuntimeIdentifier" -Value $rid

    if ($isWindows) {
        Add-PropertyLine -List $rspLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $rspLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $rspLines -Name "TcpPort" -Value $TcpPort
        Add-PropertyLine -List $rspLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $rspLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    $displayLines = New-Object System.Collections.Generic.List[string]
    Add-PropertyLine -List $displayLines -Name "RuntimeIdentifier" -Value $rid
    if ($isWindows) {
        Add-PropertyLine -List $displayLines -Name "ServerAddress" -Value $ServerAddress
        Add-PropertyLine -List $displayLines -Name "ServerUser" -Value $ServerUser
        Add-PropertyLine -List $displayLines -Name "TcpPort" -Value $TcpPort
        Add-MaskedDisplay -List $displayLines -Name "ServerPassword" -Value $ServerPassword
        Add-PropertyLine -List $displayLines -Name "_DotNetRootRemoteDirectory" -Value $DotNetRootRemoteDirectory
    }

    if ($Action -eq "simulator") {
        if ($Device) {
            $udid = $Device
            if (-not $udid.StartsWith(":v2:udid=")) { $udid = ":v2:udid=$udid" }
            Add-PropertyLine -List $rspLines -Name "_DeviceName" -Value $udid
            Add-PropertyLine -List $displayLines -Name "_DeviceName" -Value $udid
        }
    }
    elseif ($Action -eq "device") {
        if (-not $Device) {
            Write-Host "Fehler: Fuer Device-Deployment muss -Device (UDID) angegeben werden." -ForegroundColor Red
            exit 1
        }
        Add-PropertyLine -List $rspLines -Name "_DeviceName" -Value $Device
        Add-PropertyLine -List $displayLines -Name "_DeviceName" -Value $Device
        if ($CodesignKey) {
            Add-PropertyLine -List $rspLines -Name "CodesignKey" -Value $CodesignKey
            Add-PropertyLine -List $displayLines -Name "CodesignKey" -Value $CodesignKey
        }
        if ($CodesignProvision) {
            Add-PropertyLine -List $rspLines -Name "CodesignProvision" -Value $CodesignProvision
            Add-PropertyLine -List $displayLines -Name "CodesignProvision" -Value $CodesignProvision
        }
        if ($CodesignEntitlements) {
            Add-PropertyLine -List $rspLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
            Add-PropertyLine -List $displayLines -Name "CodesignEntitlements" -Value $CodesignEntitlements
        }
    }

    $rspPath = New-ResponseFile -Lines $rspLines
    try {
        Write-Host "Starte iOS $Action ..." -ForegroundColor Cyan
        $baseArgs = @("build", $projectPath, "-t:Run", "-f", $framework, "-c", $config, "@$rspPath")
        Write-Host "dotnet build $projectPath -t:Run -f $framework -c $config @$rspPath" -ForegroundColor Gray
        Write-Host "Eigenschaften:" -ForegroundColor Gray
        foreach ($line in $displayLines) { Write-Host "   $line" -ForegroundColor Gray }

        & dotnet @baseArgs

        if ($LASTEXITCODE -ne 0) {
            Write-Host "$Action fehlgeschlagen (Exit-Code: $LASTEXITCODE)." -ForegroundColor Red
            exit 1
        }
        Write-Host "iOS $Action erfolgreich." -ForegroundColor Green
    }
    finally {
        Remove-Item -Path $rspPath -ErrorAction SilentlyContinue
    }
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
    Write-Host "1) Build (iOS-App, optional .ipa mit Codesigning)"
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
