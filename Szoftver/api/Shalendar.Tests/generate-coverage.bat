@echo off
setlocal

rem === Clean up old reports ===
rmdir /s /q coveragereport
rmdir /s /q TestResults_Unit
rmdir /s /q TestResults_Integration
rmdir /s /q TestResults_All

echo ===============================
echo Running UNIT tests with coverage...
echo ===============================
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~Shalendar.Tests.Unit" --results-directory:TestResults_Unit

echo Generating UNIT coverage report...
reportgenerator -reports:TestResults_Unit/**/coverage.cobertura.xml -targetdir:coveragereport/unit

echo ===============================
echo Running INTEGRATION tests with coverage...
echo ===============================
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~Shalendar.Tests.Integration" --results-directory:TestResults_Integration

echo Generating INTEGRATION coverage report...
reportgenerator -reports:TestResults_Integration/**/coverage.cobertura.xml -targetdir:coveragereport/integration

echo ===============================
echo Running ALL tests with coverage...
echo ===============================
dotnet test --collect:"XPlat Code Coverage" --results-directory:TestResults_All

echo Generating ALL coverage report...
reportgenerator -reports:TestResults_All/**/coverage.cobertura.xml -targetdir:coveragereport/all

echo ===============================
echo Coverage reports generated!
echo - Unit:         coveragereport\unit\index.html
echo - Integration:  coveragereport\integration\index.html
echo - All:          coveragereport\all\index.html
echo ===============================

rem === Clean up test result folders ===
rmdir /s /q TestResults_Unit
rmdir /s /q TestResults_Integration
rmdir /s /q TestResults_All

endlocal
pause
