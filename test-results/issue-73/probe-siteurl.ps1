# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

# Manual repro for issue-73: drives the SiteUrl search flow + reset steps and
# reports the process state after every action.
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes
$p = Get-Process Reporter -ErrorAction Stop
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)

function Alive() { $p.Refresh(); Write-Output ("process alive: " + (-not $p.HasExited) + " hwnd=" + $p.MainWindowHandle) }
function ByName($name) {
    $c = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
}
function ByNameCt($name, $ct) {
    $nc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $cc = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ct)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object System.Windows.Automation.AndCondition($nc, $cc)))
}
function Act($el) {
    if ($null -eq $el) { Write-Output 'element missing'; return }
    $inv = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    if ($inv) { $inv.Invoke(); Write-Output ("invoked '" + $el.Current.Name + "'"); return }
    $sel = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    if ($sel) { $sel.Select(); Write-Output ("selected '" + $el.Current.Name + "'"); return }
    Write-Output ('no pattern for ' + $el.Current.Name + ' [' + $el.Current.ControlType.ProgrammaticName + ']')
}

$tab = ByNameCt 'Feeds' ([System.Windows.Automation.ControlType]::TabItem)
Act $tab
Start-Sleep -Seconds 2
Alive

$add = ByNameCt '+ Feed per URL hinzufügen' ([System.Windows.Automation.ControlType]::Button)
if (-not $add) { $add = ByName '+ Feed per URL hinzufügen' }
Act $add
Start-Sleep -Seconds 2
Alive

$editC = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
$entry = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editC)
$entry.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('http://127.0.0.1:8877/site')
Write-Output 'url set'
Alive

$search = ByNameCt 'Suchen' ([System.Windows.Automation.ControlType]::Button)
Act $search
Start-Sleep -Seconds 5
Alive

$hit = ByName 'Stub Site Feed'
Write-Output ('result card found: ' + ($null -ne $hit))
Alive

# ResetUiState steps
$cancel = ByNameCt 'Abbrechen' ([System.Windows.Automation.ControlType]::Button)
if ($cancel) { Act $cancel } else { Write-Output 'no Abbrechen' }
Start-Sleep -Milliseconds 800
Alive

$back = ByNameCt 'Zurück zu meinen Feeds' ([System.Windows.Automation.ControlType]::Button)
if (-not $back) { $back = ByName 'Zurück zu meinen Feeds' }
if ($back) { Act $back } else { Write-Output 'no Zurück' }
Start-Sleep -Milliseconds 800
Alive

$dismiss = ByName 'Schließen'
if ($dismiss) { Act $dismiss } else { Write-Output 'no Schließen' }
Start-Sleep -Milliseconds 800
Alive

$tab2 = ByNameCt 'Feeds' ([System.Windows.Automation.ControlType]::TabItem)
Write-Output ('Feeds tab after reset: ' + ($null -ne $tab2))
Alive
