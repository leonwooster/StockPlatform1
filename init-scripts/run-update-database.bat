@echo off
REM Applies the latest EF Core migrations to the database.

REM Resolve script directory and project root
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%.."

pushd "%PROJECT_ROOT%" >nul

echo Applying EF Core migrations...

dotnet ef database update ^
  --project backend\src\StockSensePro.Infrastructure\StockSensePro.Infrastructure.csproj ^
  --startup-project backend\src\StockSensePro.API\StockSensePro.API.csproj %*

set "EXIT_CODE=%ERRORLEVEL%"

if "%EXIT_CODE%"=="0" (
    echo Database update completed successfully.
) else (
    echo Database update failed with exit code %EXIT_CODE%.
)

popd >nul

exit /b %EXIT_CODE%
