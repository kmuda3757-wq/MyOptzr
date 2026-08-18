# Sigma Optimizer

App Windows (WPF, .NET 8) untuk toggle on/off berbagai tweak performa: power plan,
Game Bar, animasi/transparansi UI, SysMain, Nagle algorithm, scheduled defrag, dll.
Tiap toggle langsung mengubah registry / service Windows yang bersangkutan — bukan simulasi.

## Struktur project

```
PCOptimizerApp/
├── PCOptimizerApp.csproj      # target net8.0-windows, WPF
├── app.manifest                # requireAdministrator (wajib untuk HKLM & service control)
├── App.xaml / App.xaml.cs
├── MainWindow.xaml             # UI: sidebar, daftar tweak, log panel
├── MainWindow.xaml.cs          # wiring UI + toggle handler + konfirmasi utk tweak Risiko
├── Models/
│   ├── TweakItem.cs            # satu baris tweak (nama, tag, cara baca & apply state)
│   └── TweakCategory.cs
└── Services/
    ├── SystemActions.cs        # SEMUA sentuhan nyata ke sistem: registry, sc.exe, powercfg, dll
    └── TweakCatalog.cs         # daftar tweak per kategori, menghubungkan UI ke SystemActions
```

## Cara paling gampang: dapat .exe jadi tanpa install apa-apa

Kalau kamu tidak mau install Visual Studio/.NET SDK sama sekali, biarkan **GitHub Actions**
yang compile-kan untuk kamu (gratis untuk repo publik):

1. Upload folder ini ke repo GitHub baru (drag-drop lewat github.com juga bisa, tidak perlu
   command line git).
2. Buka tab **Actions** di repo tersebut → workflow "Build SigmaOptimizer.exe" akan otomatis
   jalan (atau klik "Run workflow" kalau belum jalan sendiri).
3. Tunggu ± 2-3 menit sampai tanda centang hijau muncul.
4. Klik run yang selesai itu → scroll ke bawah ke bagian **Artifacts** → download
   `SigmaOptimizer-exe`. Isinya file `SigmaOptimizer.exe` siap pakai — tinggal dobel-klik
   di PC Windows manapun, tidak perlu install .NET runtime segala.

File konfigurasinya ada di `.github/workflows/build-exe.yml`, sudah disertakan di project ini.

## Cara build & jalankan (mode development)

Butuh **Windows** + **.NET 8 SDK** (https://dotnet.microsoft.com/download).

```bash
cd PCOptimizerApp
dotnet build
dotnet run
```

Atau buka folder ini di Visual Studio 2022 (workload ".NET desktop development"),
lalu tekan F5. App otomatis minta elevasi Administrator saat start (lewat `app.manifest`) —
tanpa ini, sebagian besar toggle akan gagal (registry HKLM & service control butuh admin).

## Jadikan satu file .exe (portable, tanpa perlu install .NET runtime)

Paling gampang, jalankan (Windows, di folder `PCOptimizerApp`):

```bat
build-exe.bat
```

Ini menjalankan `dotnet publish` dengan self-contained + single-file, hasilnya:

```
dist\SigmaOptimizer.exe
```

File ini sudah bisa langsung didobel-klik / dibagikan ke PC Windows lain (x64) tanpa
perlu install .NET SDK/runtime di situ. Ukurannya lebih besar (~150MB) karena runtime
ikut dibundle di dalam exe-nya.

Kalau mau jalankan manual tanpa script:

```bash
dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -o dist
```

## (Opsional) Bikin installer proper

`SigmaOptimizer.exe` di atas sudah portable, tapi kalau mau ada installer dengan
shortcut Start Menu, ikon Desktop, dan uninstaller — pakai **Inno Setup** (gratis,
https://jrsoftware.org/isinfo.php):

1. Jalankan `build-exe.bat` dulu.
2. Buka `installer.iss` di Inno Setup Compiler → klik **Compile**.
3. Installer jadi di `Output\SigmaOptimizerSetup.exe`.

## Tweak yang sudah nyata (bukan placeholder)

| Kategori | Tweak | Yang dilakukan |
|---|---|---|
| CPU | Power Plan High Performance | `powercfg /setactive` ke skema High Performance |
| CPU | Matikan Core Parking | `powercfg /setacvalueindex ... PROCTHROTTLEMIN 100` |
| FPS | Matikan Game Bar & Game DVR | Registry `GameDVR_Enabled` + `AutoGameModeEnabled` |
| Memory | Matikan Animasi | Registry `Control Panel\Desktop\WindowMetrics\MinAnimate` |
| Memory | Matikan Transparansi | Registry `Themes\Personalize\EnableTransparency` |
| Memory | Matikan SysMain | `sc config SysMain start= disabled` + stop service |
| Network | Matikan Nagle Algorithm | Registry `TcpAckFrequency` + `TCPNoDelay` per interface |
| Network | Flush DNS | `ipconfig /flushdns` |
| Disk | Matikan Scheduled Defrag | `schtasks /Change ... /Disable` |

## Menambah tweak baru

1. Tambah method baru di `Services/SystemActions.cs` (aksi nyata ke registry/service/proses).
2. Tambah `TweakItem` baru di `Services/TweakCatalog.cs`, isi `ReadCurrentState` (baca status
   sekarang, dipakai saat app dibuka) dan `Apply` (panggil method dari langkah 1).
3. Selesai — UI otomatis merender row baru, toggle, tag, dan log-nya.

## Catatan keamanan

- Tweak yang ditandai `Risky = true` di catalog akan memunculkan dialog konfirmasi
  sebelum diterapkan (lihat `MainWindow.xaml.cs` → `OnToggleClicked`).
- Semua perubahan lewat registry/service Windows bersifat reversibel — toggle lagi
  ke posisi OFF untuk mengembalikan ke default.
- Belum ada fitur "restore semua ke default pabrik" / backup registry otomatis;
  kalau mau dipakai serius, ini layak ditambahkan sebelum dipakai di banyak PC.
