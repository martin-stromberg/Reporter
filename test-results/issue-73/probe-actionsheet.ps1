# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

# Diagnostic: drives the running Reporter.exe through add-feed + feed action sheet
# and dumps the UIA tree while the action sheet is open (issue-73 debugging).
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class P13 {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, int d, UIntPtr e);
}
"@
$p = Get-Process Reporter -ErrorAction Stop
[P13]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function FindButton($substr) {
    $btns = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Button)))
    foreach ($b in $btns) { if ($b.Current.Name -like "*$substr*") { return $b } }
    return $null
}
function ClickCenter($el) {
    $r = $el.Current.BoundingRectangle
    $x = [int]($r.X + $r.Width / 2); $y = [int]($r.Y + $r.Height / 2)
    [P13]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 120
    [P13]::mouse_event(0x02, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 60
    [P13]::mouse_event(0x04, 0, 0, 0, [UIntPtr]::Zero)
    Write-Output "clicked ($x,$y) [$($el.Current.ControlType.ProgrammaticName)]"
}

$add = FindButton 'URL'
if (-not $add) { Write-Output 'ADD BUTTON NOT FOUND'; exit 1 }
ClickCenter $add
Start-Sleep -Seconds 2

$editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$entry = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond)
if (-not $entry) { Write-Output 'NewUrlEntry NOT FOUND'; exit 1 }
Write-Output ("entry: " + $entry.Current.ControlType.ProgrammaticName + " name='" + $entry.Current.Name + "'")
$vp = $entry.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
if ($vp) { $vp.SetValue('http://127.0.0.1:1/probe.xml'); Write-Output 'value set' } else { Write-Output 'NO ValuePattern on entry'; exit 1 }

$da = FindButton 'direkt'
if (-not $da) { Write-Output 'DIRECT ADD NOT FOUND'; exit 1 }
ClickCenter $da
Start-Sleep -Seconds 4

$nc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, 'probe.xml')
$card = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $nc)
if (-not $card) { Write-Output 'CARD NOT FOUND'; exit 1 }
Write-Output ("card: " + $card.Current.ControlType.ProgrammaticName + " helptext='" + $card.Current.HelpText + "'")
ClickCenter $card
Start-Sleep -Seconds 2

Write-Output '=== TREE WITH ACTION SHEET ==='
function Dump($el, $depth, $max) {
    if ($depth -gt $max -or $null -eq $el) { return }
    $ct = $el.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
    Write-Output ("{0}[{1}] name='{2}' aid='{3}' off={4}" -f ('  ' * $depth), $ct, $el.Current.Name, $el.Current.AutomationId, $el.Current.IsOffscreen)
    $children = $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($c in $children) { Dump $c ($depth + 1) $max }
}
Dump $root 0 8
$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
$tops = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
Write-Output "--- top-level: $($tops.Count) ---"
foreach ($t in $tops) { Dump $t 0 8 }
