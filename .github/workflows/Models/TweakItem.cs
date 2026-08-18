using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PCOptimizerApp.Models;

/// <summary>
/// Satu baris tweak (misal "Disable Core Parking") lengkap dengan cara membaca
/// status saat ini dari sistem dan cara menerapkan perubahan saat toggle di-klik.
/// </summary>
public class TweakItem : INotifyPropertyChanged
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string[] Tags { get; init; } = Array.Empty<string>();
    public bool Risky { get; init; }

    /// <summary>Membaca status nyata dari registry/service. Dipanggil sekali saat load.</summary>
    public required Func<bool> ReadCurrentState { get; init; }

    /// <summary>Menerapkan perubahan ke sistem (registry, service, atau perintah lain).</summary>
    public required Action<bool> Apply { get; init; }

    private bool _isOn;
    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn == value) return;
            _isOn = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Set IsOn dari kode tanpa memicu apply (dipakai saat inisialisasi awal).</summary>
    public void InitializeFromSystem()
    {
        try
        {
            _isOn = ReadCurrentState();
        }
        catch
        {
            _isOn = false;
        }
        OnPropertyChanged(nameof(IsOn));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
