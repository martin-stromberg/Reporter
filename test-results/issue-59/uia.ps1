# Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.

param(
    [Parameter(Mandatory=$true)][string]$Action,
    [string]$Path = "",
    [int]$X = 0,
    [int]$Y = 0,
    [string]$Name = "",
    [string]$Text = ""
)
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing,System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32u {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, UIntPtr dwExtraInfo);
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    public const uint LEFTDOWN = 0x02; public const uint LEFTUP = 0x04;
    public const uint WHEEL = 0x0800;
}
"@
$p = Get-Process Reporter -ErrorAction Stop
[Win32u]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 300

function Find-Element($name) {
    $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

switch ($Action) {
    "shot" {
        $r = New-Object Win32u+RECT
        [Win32u]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
        $w = $r.Right - $r.Left; $h = $r.Bottom - $r.Top
        $bmp = New-Object System.Drawing.Bitmap $w, $h
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.CopyFromScreen($r.Left, $r.Top, 0, 0, $bmp.Size)
        New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
        $bmp.Save($Path)
        $g.Dispose(); $bmp.Dispose()
        Write-Output "saved $Path ($w x $h)"
    }
    "rect" {
        $r = New-Object Win32u+RECT
        [Win32u]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
        Write-Output ("window: {0}x{1} at {2},{3}" -f ($r.Right - $r.Left), ($r.Bottom - $r.Top), $r.Left, $r.Top)
    }
    "click" {
        [Win32u]::SetCursorPos($X, $Y) | Out-Null
        Start-Sleep -Milliseconds 150
        [Win32u]::mouse_event([Win32u]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
        Start-Sleep -Milliseconds 60
        [Win32u]::mouse_event([Win32u]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
        Write-Output "clicked $X,$Y"
    }
    "type" {
        [System.Windows.Forms.SendKeys]::SendWait($Text)
        Write-Output "typed $Text"
    }
    "setvalue" {
        # Set text on the first Edit control whose Name matches $Name (fallback: first Edit)
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        $editCond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ControlTypeProperty, [System.Windows.Automation.ControlType]::Edit)
        $el = $null
        if ($Name -ne "") {
            $cond = New-Object System.Windows.Automation.AndCondition(
                (New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)),
                $editCond)
            $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        }
        if ($null -eq $el) { $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $editCond) }
        if ($null -eq $el) { Write-Output "no edit control found"; exit 1 }
        $vp = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$vp)) {
            $vp.SetValue($Text)
            Write-Output "setvalue: $Text"
        } else {
            $b = $el.Current.BoundingRectangle
            [Win32u]::SetCursorPos([int]($b.X + 20), [int]($b.Y + $b.Height / 2)) | Out-Null
            [Win32u]::mouse_event([Win32u]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
            [Win32u]::mouse_event([Win32u]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 200
            [System.Windows.Forms.SendKeys]::SendWait("^a")
            [System.Windows.Forms.SendKeys]::SendWait($Text)
            Write-Output "typed into edit: $Text"
        }
    }
    "list" {
        $root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        $all = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        foreach ($e in $all) {
            $n = $e.Current.Name
            if ($n) {
                $b = $e.Current.BoundingRectangle
                Write-Output ("{0}: '{1}' [{2:F0},{3:F0} {4:F0}x{5:F0}]" -f $e.Current.ControlType.ProgrammaticName.Replace('ControlType.',''), $n, $b.X, $b.Y, $b.Width, $b.Height)
            }
        }
    }
    "invoke" {
        $el = Find-Element $Name
        if ($null -eq $el) { Write-Output "not found: $Name"; exit 1 }
        $ip = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$ip)) {
            $ip.Invoke()
            Write-Output "invoked: $Name"
        } else {
            $b = $el.Current.BoundingRectangle
            $cx = [int]($b.X + $b.Width / 2); $cy = [int]($b.Y + $b.Height / 2)
            [Win32u]::SetCursorPos($cx, $cy) | Out-Null
            Start-Sleep -Milliseconds 150
            [Win32u]::mouse_event([Win32u]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
            Start-Sleep -Milliseconds 60
            [Win32u]::mouse_event([Win32u]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
            Write-Output "clicked center of: $Name at $cx,$cy"
        }
    }
    "pick" {
        # Expand a ComboBox named $Name and select the item named $Text
        $el = Find-Element $Name
        if ($null -eq $el) { Write-Output "not found: $Name"; exit 1 }
        $ep = $null
        if ($el.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$ep)) {
            $ep.Expand()
            Start-Sleep -Milliseconds 500
            $item = Find-Element $Text
            if ($null -eq $item) { Write-Output "item not found: $Text"; exit 1 }
            $sp = $null
            if ($item.TryGetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern, [ref]$sp)) {
                $sp.Select()
                Write-Output "picked: $Text"
            } else {
                $b = $item.Current.BoundingRectangle
                $cx = [int]($b.X + $b.Width / 2); $cy = [int]($b.Y + $b.Height / 2)
                [Win32u]::SetCursorPos($cx, $cy) | Out-Null
                [Win32u]::mouse_event([Win32u]::LEFTDOWN, 0, 0, 0, [UIntPtr]::Zero)
                [Win32u]::mouse_event([Win32u]::LEFTUP, 0, 0, 0, [UIntPtr]::Zero)
                Write-Output "clicked item: $Text at $cx,$cy"
            }
        } else {
            Write-Output "no ExpandCollapsePattern on: $Name"; exit 1
        }
    }
}
