; Opsional: kalau mau installer proper (bukan cuma exe portable) dengan shortcut Start Menu,
; uninstaller, dsb. Butuh Inno Setup (gratis): https://jrsoftware.org/isinfo.php
;
; Cara pakai:
;   1. Jalankan build-exe.bat dulu supaya dist\SigmaOptimizer.exe ada.
;   2. Buka file ini di Inno Setup Compiler, klik Compile.
;   3. Installer jadi ada di Output\SigmaOptimizerSetup.exe

#define MyAppName "Sigma Optimizer"
#define MyAppVersion "1.0.0"
#define MyAppExeName "SigmaOptimizer.exe"

[Setup]
AppId={{9E1B7B3B-6C3E-4B7A-9F2E-1F5C7A6C0A11}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=SigmaOptimizerSetup
Compression=lzma2
SolidCompression=yes
; App butuh admin tiap dijalankan (lihat app.manifest), jadi installer juga minta admin.
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "dist\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Buat shortcut di Desktop"; GroupDescription: "Shortcut tambahan:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan {#MyAppName} sekarang"; Flags: nowait postinstall skipifsilent
