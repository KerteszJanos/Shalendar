@echo off
echo --------------------------------------
echo Shalendar setup script started...
echo --------------------------------------

:: --- Check for prerequisites ---

:: Check dotnet
where dotnet >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] .NET SDK not found. Please install it from https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

:: Check npm
where npm >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] npm (Node.js) not found. Please install it from https://nodejs.org
    pause
    exit /b 1
)

:: Check sqlcmd
where sqlcmd >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] sqlcmd not found. Please install SQL Server Command Line Utilities or SSMS.
    pause
    exit /b 1
)

echo ✅ All required tools found.

:: --- 1. Restore .NET dependencies ---
echo.
echo [1/3] Running dotnet restore...
dotnet restore "api\Shalendar\Shalendar.sln"
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to restore .NET packages.
    pause
    exit /b %ERRORLEVEL%
)

:: --- 2. Install npm packages for Vue frontend ---
echo.
echo [2/3] Installing npm packages...
cd ui
call npm install
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to install npm packages.
    pause
    exit /b %ERRORLEVEL%
)
cd ..

:: --- 3. Restore SQL Server database ---
echo.
echo [3/3] Restoring MSSQL database...

:: Prompt for SQL credentials
set /p SQL_SERVER=Enter SQL Server name (default: localhost): 
IF "%SQL_SERVER%"=="" SET SQL_SERVER=localhost

set /p SQL_DATABASE=Enter target database name (default: Shalendar): 
IF "%SQL_DATABASE%"=="" SET SQL_DATABASE=Shalendar

set /p SQL_USER=Enter SQL username: 
set /p SQL_PASSWORD=Enter SQL password: 

:: Perform the restore
sqlcmd -S %SQL_SERVER% -U %SQL_USER% -P %SQL_PASSWORD% -Q "RESTORE DATABASE [%SQL_DATABASE%] FROM DISK='db\Shalendar_backup.bak' WITH REPLACE"
IF %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Failed to restore database. Please check connection info and permissions.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo --------------------------------------
echo ✅ All setup steps completed successfully.
pause
