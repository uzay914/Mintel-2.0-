@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo ERROR: .NET Framework 4.x compiler was not found.
  pause
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /win32icon:"MintelOS.ico" /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:"MintelOSControl.exe" "MintelOSControl.cs"
if errorlevel 1 (
  echo Compilation failed. Send a screenshot of the errors.
  pause
  exit /b 1
)
echo Built MintelOSControl.exe successfully.
pause
