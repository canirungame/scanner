@echo off
setlocal
cd /d %~dp0
set MODE=%1
if "%MODE%"=="" set MODE=dev
if /I "%MODE%"=="prod" (
  set URL=https://canirungame.com
) else (
  set URL=http://localhost:3000
)
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
> Config.cs echo namespace CanIRunGame { static class Config { public const string ApiUrl = "%URL%"; } }
if not exist dist mkdir dist
"%CSC%" /nologo /target:winexe /optimize+ /platform:anycpu /out:dist\CanIRunGame-Scanner.exe /win32icon:icon.ico /win32manifest:app.manifest /resource:icon.png,icon.png /resource:icon.ico,icon.ico /r:System.Management.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll Program.cs MainForm.cs Scanner.cs Json.cs AssemblyInfo.cs Config.cs
if errorlevel 1 (
  > Config.cs echo namespace CanIRunGame { static class Config { public const string ApiUrl = "http://localhost:3000"; } }
  exit /b 1
)
if not exist ..\frontend\public\downloads mkdir ..\frontend\public\downloads
copy /Y dist\CanIRunGame-Scanner.exe ..\frontend\public\downloads\CanIRunGame-Scanner.exe
> ..\frontend\public\downloads\.mode echo %MODE%
> Config.cs echo namespace CanIRunGame { static class Config { public const string ApiUrl = "http://localhost:3000"; } }
echo Built C# scanner for %MODE% (%URL%)
