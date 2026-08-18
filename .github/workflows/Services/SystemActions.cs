using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace PCOptimizerApp.Services;

/// <summary>
/// Semua aksi yang benar-benar menyentuh sistem hidup di sini, terpisah dari UI,
/// supaya gampang diaudit/ditest dan supaya MainWindow tidak perlu tahu detail Win32/registry.
/// App harus jalan sebagai Administrator (lihat app.manifest) untuk sebagian besar aksi ini.
/// </summary>
public static class SystemActions
{
    // ---------- Registry helpers ----------------------------------------

    public static void SetDword(RegistryHive hive, string subKey, string valueName, int onValue, int offValue, bool isOn)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(subKey, writable: true)
            ?? throw new InvalidOperationException($"Tidak bisa membuka/membuat key: {subKey}");
        key.SetValue(valueName, isOn ? onValue : offValue, RegistryValueKind.DWord);
    }

    public static bool ReadDwordEquals(RegistryHive hive, string subKey, string valueName, int onValue, bool defaultIfMissing = false)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey);
            var v = key?.GetValue(valueName);
            if (v == null) return defaultIfMissing;
            return Convert.ToInt32(v) == onValue;
        }
        catch
        {
            return defaultIfMissing;
        }
    }

    // ---------- Process / command helpers --------------------------------

    public static (int ExitCode, string Output) RunCommand(string exe, string args, int timeoutMs = 8000)
    {
        var psi = new ProcessStartInfo(exe, args)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Gagal menjalankan {exe}");
        string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(timeoutMs);
        return (p.ExitCode, output.Trim());
    }

    // ---------- Services ----------------------------------------------

    public static void SetServiceStartupType(string serviceName, bool enabled)
    {
        RunCommand("sc.exe", $"config \"{serviceName}\" start= {(enabled ? "auto" : "disabled")}");
        if (!enabled)
            RunCommand("net.exe", $"stop \"{serviceName}\"");
        else
            RunCommand("net.exe", $"start \"{serviceName}\"");
    }

    public static bool IsServiceDisabled(string serviceName)
    {
        var (_, output) = RunCommand("sc.exe", $"qc \"{serviceName}\"");
        return output.Contains("DISABLED", StringComparison.OrdinalIgnoreCase);
    }

    // ---------- Power plan / CPU ----------------------------------------

    private const string GuidHighPerformance = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    private const string GuidBalanced = "381b4222-f694-41f0-9685-ff5bb260df2e";

    public static void SetHighPerformancePowerPlan(bool isOn)
    {
        RunCommand("powercfg.exe", $"/setactive {(isOn ? GuidHighPerformance : GuidBalanced)}");
    }

    public static bool IsHighPerformanceActive()
    {
        var (_, output) = RunCommand("powercfg.exe", "/getactivescheme");
        return output.Contains(GuidHighPerformance, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Menjaga semua core CPU tetap aktif (minimum processor state tinggi) supaya
    /// tidak ada delay saat core "parked" harus di-wake-up. Reversibel lewat powercfg.
    /// </summary>
    public static void SetCoreParkingDisabled(bool disableParking)
    {
        string minState = disableParking ? "100" : "5";
        RunCommand("powercfg.exe", $"/setacvalueindex scheme_current sub_processor PROCTHROTTLEMIN {minState}");
        RunCommand("powercfg.exe", "/setactive scheme_current");
    }

    // ---------- GameDVR / Game Bar (FPS category) ------------------------

    public static void SetGameDvrEnabled(bool enabled)
    {
        SetDword(RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", onValue: 1, offValue: 0, isOn: enabled);
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", onValue: 1, offValue: 0, isOn: enabled);
    }

    // ---------- Visual effects / animations (Memory category) ------------

    public static void SetAnimationsEnabled(bool enabled)
    {
        // MinAnimate="1" (default) / "0" (mati) - dibaca sebagai string di registry ini
        using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop\WindowMetrics", writable: true);
        key?.SetValue("MinAnimate", enabled ? "1" : "0", RegistryValueKind.String);
    }

    public static bool AreAnimationsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop\WindowMetrics");
        var v = key?.GetValue("MinAnimate") as string;
        return v != "0"; // default Windows = enabled
    }

    public static void SetTransparencyEnabled(bool enabled)
    {
        SetDword(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "EnableTransparency", onValue: 1, offValue: 0, isOn: enabled);
    }

    // ---------- Network -------------------------------------------------

    public static void FlushDns() => RunCommand("ipconfig.exe", "/flushdns");

    /// <summary>Nonaktifkan Nagle's algorithm di semua interface TCP/IP untuk kurangi latency paket kecil.</summary>
    public static void SetNagleDisabled(bool disableNagle)
    {
        const string basePath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
        using var baseKey = Registry.LocalMachine.OpenSubKey(basePath, writable: true);
        if (baseKey == null) return;
        foreach (var subName in baseKey.GetSubKeyNames())
        {
            using var iface = baseKey.OpenSubKey(subName, writable: true);
            if (iface == null) continue;
            iface.SetValue("TcpAckFrequency", disableNagle ? 1 : 2, RegistryValueKind.DWord);
            iface.SetValue("TCPNoDelay", disableNagle ? 1 : 0, RegistryValueKind.DWord);
        }
    }

    // ---------- Disk / indexing ------------------------------------------

    public static void SetScheduledDefragEnabled(bool enabled)
    {
        string arg = enabled
            ? "/Change /TN \"Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Enable"
            : "/Change /TN \"Microsoft\\Windows\\Defrag\\ScheduledDefrag\" /Disable";
        RunCommand("schtasks.exe", arg);
    }

    // ---------- Restart ---------------------------------------------------

    public static void RestartComputer(int delaySeconds = 10)
    {
        RunCommand("shutdown.exe", $"/r /t {delaySeconds} /c \"Restart oleh Sigma Optimizer\"");
    }

    public static void CancelRestart()
    {
        RunCommand("shutdown.exe", "/a");
    }
}
