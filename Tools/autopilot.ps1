# Runs the Windows build's autopilot without disturbing whoever uses the PC: the game window opens without taking
# focus and is moved off-screen, where it keeps rendering (a hidden window would give black screenshots).
# usage: powershell -File Tools/autopilot.ps1 <screenshot dir> [-Server http://127.0.0.1:8080]
param([Parameter(Mandatory = $true)][string]$Out, [string]$Server)

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class QuietLaunch {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO { public int cb; public string r, d, t; public int x, y, w, h, xc, yc, fill, flags; public short show, r2; public IntPtr r3, i, o, e; }
    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION { public IntPtr hProcess, hThread; public int pid, tid; }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcess(string app, string cmd, IntPtr pa, IntPtr ta, bool inherit, int flags, IntPtr env, string dir, ref STARTUPINFO si, out PROCESS_INFORMATION pi);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
    public static int Start(string exe, string cmd, string dir) {
        var si = new STARTUPINFO(); si.cb = Marshal.SizeOf(si);
        si.flags = 1;      // STARTF_USESHOWWINDOW
        si.show = 4;       // SW_SHOWNOACTIVATE
        PROCESS_INFORMATION pi;
        if (!CreateProcess(exe, cmd, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, dir, ref si, out pi)) return -1;
        return pi.pid;
    }
}
"@

$exe = (Resolve-Path (Join-Path $PSScriptRoot "..\Builds\Windows\Mythwake.exe")).Path
$cmd = "`"$exe`" -screen-width 540 -screen-height 960 -screen-fullscreen 0 -autopilot `"$Out`""
if ($Server) { $cmd += " -server $Server" } else { $cmd += " -offline" }
$before = [QuietLaunch]::GetForegroundWindow()
$id = [QuietLaunch]::Start($exe, $cmd, (Split-Path $exe))
if ($id -lt 0) { Write-Error "could not start $exe"; exit 1 }
$p = Get-Process -Id $id
# move the window off-screen as soon as it exists: SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
for ($i = 0; $i -lt 200 -and $p.MainWindowHandle -eq 0 -and -not $p.HasExited; $i++) { Start-Sleep -Milliseconds 50; $p.Refresh() }
if ($p.MainWindowHandle -ne 0) { [void][QuietLaunch]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, -4000, 0, 0, 0, 0x0001 -bor 0x0004 -bor 0x0010) }
# the game must never be the active window; if Windows still activates it, hand focus back
$stolen = 0
while (-not $p.HasExited) {
    if ($p.MainWindowHandle -ne 0 -and [QuietLaunch]::GetForegroundWindow() -eq $p.MainWindowHandle) {
        $stolen++
        [void][QuietLaunch]::SetForegroundWindow($before)
    }
    Start-Sleep -Milliseconds 250
}
Write-Output ("game window was active: " + ($stolen -gt 0))
exit $p.ExitCode
