@echo off
REM Runs the AddTradingSignals EF Core migration from the project root.

REM Resolve script directory and project root
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%.."

pushd "%PROJECT_ROOT%" >nul

echo Running EF Core migration: AddTradingSignals

dotnet ef migrations add AddTradingSignals ^
  --project backend\src\StockSensePro.Infrastructure\StockSensePro.Infrastructure.csproj ^
  --startup-project backend\src\StockSensePro.API\StockSensePro.API.csproj ^
  --output-dir Data\Migrations %*

set "EXIT_CODE=%ERRORLEVEL%"

if "%EXIT_CODE%"=="0" (
    echo Migration completed successfully.
) else (
    echo Migration failed with exit code %EXIT_CODE%.
)

popd >nul

exit /b %EXIT_CODE%
