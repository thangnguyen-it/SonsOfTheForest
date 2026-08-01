[CmdletBinding()]
param(
    [string]$ReleaseExecutablePath = "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe",
    [string]$DiagnosticExecutablePath = "Builds/Benchmarks/R2_PERF1_Diagnostic/SOTF_R2_PERF1_Diagnostic.exe",
    [ValidateSet("High Fidelity", "Balanced", "Performant")][string]$Quality = "Balanced",
    [ValidateSet("empty_hdrp_camera", "ground_only", "full_forest")][string]$Scenario = "full_forest",
    [ValidateSet("None", "FXAA", "SMAA", "TAA")][string]$Antialiasing = "TAA",
    [ValidateRange(50, 100)][int]$RenderScalePercent = 100,
    [string]$Upscaler = "CatmullRom",
    [ValidateRange(30, 1800)][int]$TimeoutSeconds = 180,
    [ValidateRange(3, 60)][int]$WarmupSeconds = 10,
    [ValidateRange(3, 60)][int]$SampleSeconds = 8,
    [ValidateRange(320, 7680)][int]$Width = 1280,
    [ValidateRange(240, 4320)][int]$Height = 720,
    [string]$ExpectedGpu = "NVIDIA GeForce MX550",
    [string]$OutputRoot = "Benchmarks/R2_PERF1_Pairs",
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$offlineRunner = Join-Path $PSScriptRoot "Invoke-R2Perf1Offline.ps1"
$measurementSetId = if ($DryRun) {
    "dryrun_pair"
} else {
    "pair_" + [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssfffZ")
}
$setRoot = if ([System.IO.Path]::IsPathRooted($OutputRoot)) {
    Join-Path $OutputRoot $measurementSetId
} else {
    Join-Path (Join-Path $projectRoot $OutputRoot) $measurementSetId
}

function Convert-ToCanonicalSourceCommit {
    param([Parameter(Mandatory = $true)][string]$SourceCommit)
    if ($SourceCommit -cnotmatch '^[0-9A-Fa-f]{40}$') {
        throw "sourceCommit must contain exactly 40 hexadecimal characters."
    }
    return $SourceCommit.ToUpperInvariant()
}

function Get-PairedSourceCommitComparison {
    param(
        [Parameter(Mandatory = $true)][string]$ReleaseSourceCommit,
        [Parameter(Mandatory = $true)][string]$DevelopmentSourceCommit
    )
    $releaseCanonical = Convert-ToCanonicalSourceCommit -SourceCommit $ReleaseSourceCommit
    $developmentCanonical = Convert-ToCanonicalSourceCommit -SourceCommit $DevelopmentSourceCommit
    return [pscustomobject]@{
        CanonicalSourceCommit = $releaseCanonical
        Matches = $releaseCanonical -ceq $developmentCanonical
    }
}

function Resolve-ProjectPath {
    param([string]$Path)
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path $projectRoot $Path))
}

function Read-BuildSidecar {
    param([string]$ExecutablePath)
    $resolved = Resolve-ProjectPath $ExecutablePath
    return Get-Content -LiteralPath (
        Join-Path (Split-Path -Parent $resolved) "r2-perf1.build-provenance.json") -Raw | ConvertFrom-Json
}

function Invoke-Member {
    param([string]$ExecutablePath, [string]$BuildKind, [string]$Role, [string]$MemberRoot)
    $arguments = @{
        ExecutablePath = $ExecutablePath
        Quality = $Quality
        Scenario = $Scenario
        Antialiasing = $Antialiasing
        RenderScalePercent = $RenderScalePercent
        Upscaler = $Upscaler
        BuildKind = $BuildKind
        MeasurementRole = $Role
        MeasurementSetId = $measurementSetId
        Runs = 1
        TimeoutSeconds = $TimeoutSeconds
        WarmupSeconds = $WarmupSeconds
        SampleSeconds = $SampleSeconds
        Width = $Width
        Height = $Height
        ExpectedGpu = $ExpectedGpu
        OutputRoot = $MemberRoot
    }
    if ($DryRun) { $arguments.DryRun = $true }
    & $offlineRunner @arguments
}

$memberErrors = @()
try {
    Invoke-Member -ExecutablePath $ReleaseExecutablePath -BuildKind "release" `
        -Role "release_performance" -MemberRoot (Join-Path $setRoot "release_performance")
}
catch { $memberErrors += "release_performance: $($_.Exception.Message)" }
try {
    Invoke-Member -ExecutablePath $DiagnosticExecutablePath -BuildKind "diagnostic" `
        -Role "development_gc" -MemberRoot (Join-Path $setRoot "development_gc")
}
catch { $memberErrors += "development_gc: $($_.Exception.Message)" }

if ($DryRun) {
    Write-Host "Paired dry-run completed. No player was launched and no measurement output was created."
    if ($memberErrors.Count -gt 0) { throw ($memberErrors -join "; ") }
    return
}
if ($memberErrors.Count -gt 0) {
    New-Item -ItemType Directory -Path $setRoot -Force | Out-Null
    [ordered]@{
        schemaVersion = "r2-perf1-measurement-set/1"
        measurementSetId = $measurementSetId
        createdUtc = [DateTime]::UtcNow.ToString("O")
        evidenceValidity = "INVALID"
        performanceBudgetStatus = "INCOMPLETE"
        gcBudgetStatus = "INCOMPLETE"
        aggregateProductGate = "INCOMPLETE"
        reason = $memberErrors -join "; "
    } | ConvertTo-Json | Set-Content -LiteralPath (
        Join-Path $setRoot "measurement-set.manifest.json") -Encoding UTF8
    throw ($memberErrors -join "; ")
}

function Read-SingleMember {
    param([string]$MemberRoot, [string]$ExpectedRole)
    $manifests = @(Get-ChildItem -LiteralPath $MemberRoot -Filter "runtime.manifest.json" -File -Recurse)
    if ($manifests.Count -ne 1) { throw "Expected exactly one $ExpectedRole runtime manifest." }
    $manifest = Get-Content -LiteralPath $manifests[0].FullName -Raw | ConvertFrom-Json
    if ([string]$manifest.schemaVersion -cne "r2-perf1-manifest/2" -or
        [string]$manifest.measurementRole -cne $ExpectedRole -or
        [string]$manifest.measurementSetId -cne $measurementSetId) {
        throw "Paired member contract mismatch for $ExpectedRole."
    }
    $report = Get-Content -LiteralPath $manifest.reportJsonPath -Raw | ConvertFrom-Json
    return [pscustomobject]@{ Manifest = $manifest; Report = $report; Path = $manifests[0].FullName }
}

$release = Read-SingleMember -MemberRoot (Join-Path $setRoot "release_performance") -ExpectedRole "release_performance"
$gc = Read-SingleMember -MemberRoot (Join-Path $setRoot "development_gc") -ExpectedRole "development_gc"
$releaseSidecar = Read-BuildSidecar $ReleaseExecutablePath
$gcSidecar = Read-BuildSidecar $DiagnosticExecutablePath
$sourceCommitComparison = Get-PairedSourceCommitComparison `
    -ReleaseSourceCommit ([string]$release.Report.sourceCommit) `
    -DevelopmentSourceCommit ([string]$gc.Report.sourceCommit)
$matchingFields = @(
    "measurementSetId", "contentFingerprint", "configurationFingerprint",
    "hardwareFingerprint", "benchmarkScenario", "graphicsDeviceType", "width", "height"
)
$mismatches = @()
if (-not [bool]$sourceCommitComparison.Matches) { $mismatches += "sourceCommit" }
foreach ($field in $matchingFields) {
    if ([string]$release.Report.$field -cne [string]$gc.Report.$field) { $mismatches += $field }
}
foreach ($field in @("benchmarkScenePath", "benchmarkSceneGuid")) {
    if ([string]$releaseSidecar.$field -cne [string]$gcSidecar.$field) { $mismatches += $field }
}
if ([string]$release.Report.graphicsDeviceType -cne "Direct3D11") { $mismatches += "release graphics API" }
if ([string]$gc.Report.graphicsDeviceType -cne "Direct3D11") { $mismatches += "development graphics API" }

$membersValid = [string]$release.Manifest.evidenceValidity -ceq "VALID" -and
    [string]$gc.Manifest.evidenceValidity -ceq "VALID" -and
    [bool]$release.Manifest.pairingEligible -and [bool]$gc.Manifest.pairingEligible
$evidenceValidity = if ($membersValid -and $mismatches.Count -eq 0) { "VALID" } else { "INVALID" }
$performanceStatus = [string]$release.Manifest.performanceBudgetStatus
$gcStatus = [string]$gc.Manifest.gcBudgetStatus
$aggregate = if ($evidenceValidity -cne "VALID") { "INCOMPLETE" } `
    elseif ($performanceStatus -ceq "PASS" -and $gcStatus -ceq "PASS") { "PASS" } `
    elseif ($performanceStatus -ceq "FAIL" -or $gcStatus -ceq "FAIL") { "FAIL" } `
    else { "INCOMPLETE" }
$reason = if ($mismatches.Count -gt 0) { "pair mismatch: " + ($mismatches -join ", ") } `
    elseif (-not $membersValid) { "one or both member evidence records are invalid or ineligible" } `
    elseif ($aggregate -ceq "PASS") { "paired performance and GC budgets passed" } `
    elseif ($aggregate -ceq "FAIL") { "paired evidence valid; one or more product budgets failed" } `
    else { "paired evidence valid but product gate is incomplete" }

$pairManifest = [ordered]@{
    schemaVersion = "r2-perf1-measurement-set/1"
    measurementSetId = $measurementSetId
    createdUtc = [DateTime]::UtcNow.ToString("O")
    evidenceValidity = $evidenceValidity
    performanceBudgetStatus = $performanceStatus
    gcBudgetStatus = $gcStatus
    aggregateProductGate = $aggregate
    reason = $reason
    sourceCommit = [string]$sourceCommitComparison.CanonicalSourceCommit
    contentFingerprint = [string]$release.Report.contentFingerprint
    configurationFingerprint = [string]$release.Report.configurationFingerprint
    hardwareFingerprint = [string]$release.Report.hardwareFingerprint
    repositoryReproducible = [bool]$release.Report.repositoryReproducible -and [bool]$gc.Report.repositoryReproducible
    comparisonScope = if ([string]$release.Report.comparisonScope -ceq "local_comparable" -or
        [string]$gc.Report.comparisonScope -ceq "local_comparable") { "local_comparable" } else { "repository_comparable" }
    releasePerformanceManifest = $release.Path
    developmentGcManifest = $gc.Path
    releaseBuildArtifactId = [string]$release.Report.buildArtifactId
    developmentBuildArtifactId = [string]$gc.Report.buildArtifactId
    benchmarkScenePath = [string]$releaseSidecar.benchmarkScenePath
    benchmarkSceneGuid = [string]$releaseSidecar.benchmarkSceneGuid
}
$pairManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (
    Join-Path $setRoot "measurement-set.manifest.json") -Encoding UTF8
if ($evidenceValidity -cne "VALID") { throw $reason }
Write-Host "Paired measurement set completed: $setRoot"
