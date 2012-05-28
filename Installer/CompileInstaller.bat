@echo off

echo Compiling installer...
"%PROGRAMFILES(X86)%\NSIS\MakeNSIS.exe" /V2 Installer.nsi

rem If an error occurred, pause so the user can see it
if %errorlevel% neq 0 (pause) else (echo Done.)
