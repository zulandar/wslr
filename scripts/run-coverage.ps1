# Run tests with code coverage and generate reports
# Usage: .\scripts\run-coverage.ps1

param(
    [switch]$OpenReport,
    [string]$OutputDir = "tests/coverage"
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $RepoRoot
try {
    # Clean previous coverage results
    if (Test-Path $OutputDir) {
        Remove-Item -Recurse -Force $OutputDir
    }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    Write-Host "Running tests with coverage..." -ForegroundColor Cyan

    # Run tests with coverage collection
    dotnet test `
        --settings tests/coverage.runsettings `
        --collect:"XPlat Code Coverage" `
        --results-directory $OutputDir `
        --no-build

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    # Find all coverage files
    $coverageFiles = Get-ChildItem -Path $OutputDir -Recurse -Filter "coverage.cobertura.xml"

    if ($coverageFiles.Count -eq 0) {
        Write-Host "No coverage files found!" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

    # Merge coverage files and generate report
    $reports = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"

    Write-Host "Generating coverage report..." -ForegroundColor Cyan

    dotnet reportgenerator `
        -reports:$reports `
        -targetdir:"$OutputDir/report" `
        -reporttypes:"Html;Cobertura;TextSummary" `
        -title:"WSLR Code Coverage"

    # Display summary
    $summaryFile = "$OutputDir/report/Summary.txt"
    if (Test-Path $summaryFile) {
        Write-Host "`n=== Coverage Summary ===" -ForegroundColor Green
        Get-Content $summaryFile
    }

    # Open report if requested
    if ($OpenReport) {
        $reportPath = "$OutputDir/report/index.html"
        if (Test-Path $reportPath) {
            Start-Process $reportPath
        }
    }

    Write-Host "`nCoverage report generated at: $OutputDir/report/index.html" -ForegroundColor Green
}
finally {
    Pop-Location
}
