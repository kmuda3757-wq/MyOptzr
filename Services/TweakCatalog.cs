using System.Collections.ObjectModel;
using Microsoft.Win32;
using PCOptimizerApp.Models;

namespace PCOptimizerApp.Services;

public static class TweakCatalog
{
    public static ObservableCollection<TweakCategory> Build()
    {
        var categories = new ObservableCollection<TweakCategory>
        {
            new TweakCategory
            {
                Id = "cpu",
                Label = "CPU",
                Items = new ObservableCollection<TweakItem>
                {
                    new TweakItem
                    {
                        Id = "cpu_highperf",
                        Name = "Power Plan: High Performance",
                        Description = "Ganti power plan aktif ke High Performance, CPU tidak throttle di kondisi idle singkat.",
                        Tags = new[] { "+FPS", "Latency" },
                        ReadCurrentState = SystemActions.IsHighPerformanceActive,
                        Apply = isOn => SystemActions.SetHighPerformancePowerPlan(isOn),
                    },
                    new TweakItem
                    {
                        Id = "cpu_coreparking",
                        Name = "Matikan Core Parking",
                        Description = "Semua core CPU tetap aktif, tidak ada delay wake-up core saat load naik mendadak.",
                        Tags = new[] { "+FPS", "Latency" },
                        Risky = true,
                        ReadCurrentState = () => false, // sulit dibaca reliable lintas vendor, default off
                        Apply = isOn => SystemActions.SetCoreParkingDisabled(isOn),
                    },
                },
            },
            new TweakCategory
            {
                Id = "fps",
                Label = "FPS",
                Items = new ObservableCollection<TweakItem>
                {
                    new TweakItem
                    {
                        Id = "fps_gamedvr",
                        Name = "Matikan Game Bar & Game DVR",
                        Description = "Hilangkan overhead capture background dari Xbox Game Bar saat main game.",
                        Tags = new[] { "+FPS", "Less RAM" },
                        ReadCurrentState = () => !SystemActions.ReadDwordEquals(
                            RegistryHive.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", onValue: 1, defaultIfMissing: true),
                        Apply = isOn => SystemActions.SetGameDvrEnabled(enabled: !isOn),
                    },
                },
            },
            new TweakCategory
            {
                Id = "memory",
                Label = "Memory",
                Items = new ObservableCollection<TweakItem>
                {
                    new TweakItem
                    {
                        Id = "mem_animations",
                        Name = "Matikan Semua Animasi Windows",
                        Description = "Kurangi beban compositor dari animasi buka/tutup window, menu, dsb.",
                        Tags = new[] { "+FPS", "Latency" },
                        ReadCurrentState = () => !SystemActions.AreAnimationsEnabled(),
                        Apply = isOn => SystemActions.SetAnimationsEnabled(enabled: !isOn),
                    },
                    new TweakItem
                    {
                        Id = "mem_transparency",
                        Name = "Matikan Efek Transparansi",
                        Description = "Nonaktifkan blur/acrylic pada taskbar dan jendela sistem.",
                        Tags = new[] { "+FPS" },
                        ReadCurrentState = () => !SystemActions.ReadDwordEquals(
                            RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                            "EnableTransparency", onValue: 1, defaultIfMissing: true),
                        Apply = isOn => SystemActions.SetTransparencyEnabled(enabled: !isOn),
                    },
                    new TweakItem
                    {
                        Id = "mem_sysmain",
                        Name = "Matikan SysMain (Superfetch)",
                        Description = "Hentikan service prefetch background yang memakai RAM & disk I/O saat idle.",
                        Tags = new[] { "Less RAM" },
                        ReadCurrentState = () => SystemActions.IsServiceDisabled("SysMain"),
                        Apply = isOn => SystemActions.SetServiceStartupType("SysMain", enabled: !isOn),
                    },
                },
            },
            new TweakCategory
            {
                Id = "network",
                Label = "Network",
                Items = new ObservableCollection<TweakItem>
                {
                    new TweakItem
                    {
                        Id = "net_nagle",
                        Name = "Matikan Algoritma Nagle",
                        Description = "Kirim paket kecil langsung tanpa buffering TCP, kurangi delay untuk game online.",
                        Tags = new[] { "Latency" },
                        Risky = true,
                        ReadCurrentState = () => false,
                        Apply = isOn => SystemActions.SetNagleDisabled(disableNagle: isOn),
                    },
                    new TweakItem
                    {
                        Id = "net_dns",
                        Name = "Flush DNS Cache",
                        Description = "Bersihkan cache DNS lama yang bisa bikin resolusi domain lambat/stale.",
                        Tags = new[] { "Network" },
                        ReadCurrentState = () => false,
                        Apply = isOn => { if (isOn) SystemActions.FlushDns(); },
                    },
                },
            },
            new TweakCategory
            {
                Id = "disk",
                Label = "Disk",
                Items = new ObservableCollection<TweakItem>
                {
                    new TweakItem
                    {
                        Id = "disk_defrag",
                        Name = "Matikan Scheduled Defrag (SSD)",
                        Description = "Hindari write-cycle tidak perlu ke SSD dari jadwal defrag otomatis Windows.",
                        Tags = new[] { "Stability" },
                        ReadCurrentState = () => false,
                        Apply = isOn => SystemActions.SetScheduledDefragEnabled(enabled: !isOn),
                    },
                },
            },
        };

        foreach (var cat in categories)
            foreach (var item in cat.Items)
                item.InitializeFromSystem();

        return categories;
    }
}
