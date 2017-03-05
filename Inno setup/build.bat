@echo off
cls

if [%1] == [] goto ArgumentMissing

rem Build the x86 version
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MsBuild.exe" "..\Checksum.sln" /t:Rebuild /p:Configuration=Release /p:Platform="x86"
if ERRORLEVEL 1 exit /b 1

rem Build the x64 version
"%ProgramFiles(x86)%\MSBuild\14.0\Bin\MsBuild.exe" "..\Checksum.sln" /t:Rebuild /p:Configuration=Release /p:Platform="x64"
if ERRORLEVEL 1 exit /b 1

rem Build the x86 install
"C:\Program Files (x86)\Inno Setup 5\iscc.exe" Checksum.iss /DArch=x86 /DVersion=%1

rem Build the x64 install
"C:\Program Files (x86)\Inno Setup 5\iscc.exe" Checksum.iss /DArch=x64 /DVersion=%1

exit /b 0

:ArgumentMissing
echo Please, give the version number to the build script.
echo Example: build.bat 1.0.0.0
exit /b 1