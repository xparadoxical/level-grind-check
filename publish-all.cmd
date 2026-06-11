@echo off
setlocal enabledelayedexpansion

set RIDS=win-x64 linux-x64 osx-x64 osx-arm64
set OUTDIR=LevelGrindCheck\bin\release_all

set ASMNAME=lgcheck
set TF=net9.0

if not exist %OUTDIR% mkdir %OUTDIR%

for %%R in (%RIDS%) do (
    echo ============================
    echo Publishing for %%R...
    echo ============================

    dotnet publish -c Release -r %%R

    if errorlevel 1 (
        echo FAILED for %%R
        exit /b 1
    )

    set PUBLISH_DIR=LevelGrindCheck\bin\Release\%TF%\%%R\publish

    REM -------------------------
    REM Windows executable case
    REM -------------------------
    if exist "!PUBLISH_DIR!\%ASMNAME%.exe" (
        copy /Y "!PUBLISH_DIR!\%ASMNAME%.exe" "%OUTDIR%\%ASMNAME%-%%R.exe" >nul
    ) else (
        REM -------------------------
        REM Linux/macOS binary case
        REM -------------------------
        if exist "!PUBLISH_DIR!\%ASMNAME%" (
            copy /Y "!PUBLISH_DIR!\%ASMNAME%" "%OUTDIR%\%ASMNAME%-%%R" >nul
        )
    )

    REM copy pdb if present
    if exist "!PUBLISH_DIR!\%ASMNAME%.pdb" (
        copy /Y "!PUBLISH_DIR!\%ASMNAME%.pdb" "%OUTDIR%\%ASMNAME%-%%R.pdb" >nul
    )
)

echo ============================
echo Done. Output in %OUTDIR%
echo ============================

endlocal