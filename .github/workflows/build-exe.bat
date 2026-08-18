@echo off
REM Build satu file .exe portable (self-contained, tidak butuh .NET runtime di PC target)
REM Jalankan dari Command Prompt / PowerShell di folder PCOptimizerApp, butuh .NET 8 SDK.

dotnet publish -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o dist

echo.
echo Selesai. File exe ada di: dist\SigmaOptimizer.exe
pause
