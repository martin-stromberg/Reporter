# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

# Diagnostic UIA dump for issue-73 E2E debugging (not part of the shipped test suite).
param(
    [string]$Name = "",
    [int]$MaxDepth = 6
)
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
$p = Get-Process Reporter -ErrorAction Stop
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
Write-Output "window handle $($p.MainWindowHandle) title '$($p.MainWindowTitle)'"

function Dump($el, $depth) {
    if ($depth -gt $MaxDepth -or $null -eq $el) { return }
    $ct = $el.Current.ControlType.ProgrammaticName -replace 'ControlType\.',''
    $nm = $el.Current.Name
    $aid = $el.Current.AutomationId
    $off = $el.Current.IsOffscreen
    Write-Output ("{0}[{1}] name='{2}' aid='{3}' off={4}" -f ('  ' * $depth), $ct, $nm, $aid, $off)
    $children = $el.FindAll([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    foreach ($c in $children) { Dump $c ($depth + 1) }
}
Dump $root 0

# Also dump top-level elements of the process on the desktop (popups/dialogs)
$desktop = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
$tops = $desktop.FindAll([System.Windows.Automation.TreeScope]::Children, $pidCond)
Write-Output "--- top-level elements of pid $($p.Id): $($tops.Count) ---"
foreach ($t in $tops) { Dump $t 0 }
