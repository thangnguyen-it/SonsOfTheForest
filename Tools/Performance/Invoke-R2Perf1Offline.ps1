[CmdletBinding()]
param(
    [string]$ExecutablePath = "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe",
    [ValidateSet("High Fidelity", "Balanced", "Performant")]
    [string]$Quality = "Balanced",
    [ValidateSet("empty_hdrp_camera", "ground_only", "full_forest")]
    [string]$Scenario = "full_forest",
    [ValidateSet("None", "FXAA", "SMAA", "TAA")]
    [string]$Antialiasing = "TAA",
    [ValidateRange(50, 100)]
    [int]$RenderScalePercent = 100,
    [string]$Upscaler = "CatmullRom",
    [ValidateSet("diagnostic", "release")]
    [string]$BuildKind = "release",
    [Parameter(Mandatory = $true)]
    [ValidateSet("release_performance", "development_gc")]
    [string]$MeasurementRole,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_-]{1,96}$')]
    [string]$MeasurementSetId,
    [ValidateRange(1, 20)]
    [int]$Runs = 3,
    [ValidateRange(30, 1800)]
    [int]$TimeoutSeconds = 180,
    [ValidateRange(3, 60)]
    [int]$WarmupSeconds = 10,
    [ValidateRange(3, 60)]
    [int]$SampleSeconds = 8,
    [ValidateRange(320, 7680)]
    [int]$Width = 1280,
    [ValidateRange(240, 4320)]
    [int]$Height = 720,
    [string]$ExpectedGpu = "NVIDIA GeForce MX550",
    [string]$OutputRoot = "Benchmarks/R2_PERF1_Offline",
    [switch]$CaptureScreenshot,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ProjectRoot = [System.IO.Path]::GetFullPath(
    (Join-Path -Path $PSScriptRoot -ChildPath "../.."))

function Resolve-ProjectPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $Path))
}

function Get-Sha256Text {
    param([Parameter(Mandatory = $true)][string]$Text)

    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($Text)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "")
    }
    finally {
        $sha.Dispose()
    }
}

function Add-CanonicalField {
    param(
        [Parameter(Mandatory = $true)][System.Text.StringBuilder]$Builder,
        [Parameter(Mandatory = $true)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    $safe = if ($null -eq $Value) { "" } else { $Value }
    [void]$Builder.Append($Name).Append(":").Append(
        $safe.Length.ToString([System.Globalization.CultureInfo]::InvariantCulture)).Append(":").Append($safe).Append("|")
}

function Convert-ToCanonicalSourceCommit {
    param([Parameter(Mandatory = $true)][string]$SourceCommit)

    if ($SourceCommit -cnotmatch '^[0-9A-Fa-f]{40}$') {
        throw "sourceCommit must contain exactly 40 hexadecimal characters."
    }
    return $SourceCommit.ToUpperInvariant()
}

function Convert-ToBoundedRunIdToken {
    param(
        [AllowEmptyString()][Parameter(Mandatory = $true)][string]$Value,
        [ValidateRange(1, 32)][int]$MaximumLength
    )

    $token = ($Value -replace '[^A-Za-z0-9_-]', '_').Trim('_', '-')
    if ([string]::IsNullOrEmpty($token)) {
        $token = "x"
    }
    if ($token.Length -gt $MaximumLength) {
        $token = $token.Substring(0, $MaximumLength)
    }
    return $token
}

function New-BoundedRunId {
    param(
        [Parameter(Mandatory = $true)][string]$MeasurementRole,
        [Parameter(Mandatory = $true)][string]$BuildKind,
        [AllowEmptyString()][Parameter(Mandatory = $true)][string]$Quality,
        [AllowEmptyString()][Parameter(Mandatory = $true)][string]$Scenario,
        [int]$RenderScalePercent,
        [int]$RunNumber,
        [Parameter(Mandatory = $true)][string]$Timestamp
    )

    $canonical = [System.Text.StringBuilder]::new()
    Add-CanonicalField -Builder $canonical -Name "measurementRole" -Value $MeasurementRole
    Add-CanonicalField -Builder $canonical -Name "buildKind" -Value $BuildKind
    Add-CanonicalField -Builder $canonical -Name "quality" -Value $Quality
    Add-CanonicalField -Builder $canonical -Name "scenario" -Value $Scenario
    Add-CanonicalField -Builder $canonical -Name "renderScale" -Value (
        $RenderScalePercent.ToString([System.Globalization.CultureInfo]::InvariantCulture))
    Add-CanonicalField -Builder $canonical -Name "runNumber" -Value (
        $RunNumber.ToString([System.Globalization.CultureInfo]::InvariantCulture))
    Add-CanonicalField -Builder $canonical -Name "timestamp" -Value $Timestamp

    $roleToken = switch -CaseSensitive ($MeasurementRole) {
        "release_performance" { "releaseperf"; break }
        "development_gc" { "developmentgc"; break }
        default { Convert-ToBoundedRunIdToken -Value $MeasurementRole -MaximumLength 13 }
    }
    $buildToken = Convert-ToBoundedRunIdToken -Value $BuildKind -MaximumLength 10
    $runToken = Convert-ToBoundedRunIdToken -Value (
        $RunNumber.ToString([System.Globalization.CultureInfo]::InvariantCulture)) -MaximumLength 10
    $timestampToken = Convert-ToBoundedRunIdToken -Value $Timestamp -MaximumLength 20
    $hashSuffix = (Get-Sha256Text -Text $canonical.ToString()).Substring(0, 12).ToLowerInvariant()
    $runId = "{0}-{1}-r{2}-{3}-{4}" -f $roleToken, $buildToken, $runToken, $timestampToken, $hashSuffix
    if ($runId.Length -gt 80 -or $runId -cnotmatch '^[A-Za-z0-9_-]+$') {
        throw "Bounded run-id generation violated the runtime run-id contract."
    }
    return $runId
}

function Get-BuildArtifactIdentity {
    param([Parameter(Mandatory = $true)][string]$ArtifactRoot)

    $root = [System.IO.Path]::GetFullPath($ArtifactRoot).TrimEnd('\', '/')
    if (-not (Test-Path -LiteralPath $root -PathType Container)) {
        throw "Build artifact directory does not exist: $root"
    }

    $allEntries = @(Get-ChildItem -LiteralPath $root -Recurse -Force)
    $reparse = @($allEntries | Where-Object {
        ($_.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0
    })
    if ($reparse.Count -gt 0) {
        throw "Build artifact contains a reparse point: $($reparse[0].FullName)"
    }

    $records = @($allEntries | Where-Object {
        -not $_.PSIsContainer -and
        $_.Name -cne "r2-perf1.build-provenance.json" -and
        $_.Name -cne "r2-perf1.build-provenance.json.tmp"
    } | ForEach-Object {
        $full = [System.IO.Path]::GetFullPath($_.FullName)
        $prefix = $root + [System.IO.Path]::DirectorySeparatorChar
        if (-not $full.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Build artifact path escaped artifact root: $full"
        }
        [pscustomobject]@{
            RelativePath = $full.Substring($prefix.Length).Replace('\', '/')
            FullPath = $full
            Length = $_.Length
        }
    })
    if ($records.Count -eq 0) { throw "Build artifact tree contains no files." }

    $recordByPath = @{}
    foreach ($record in $records) { $recordByPath[[string]$record.RelativePath] = $record }
    [string[]]$orderedPaths = @($records | ForEach-Object { [string]$_.RelativePath })
    [System.Array]::Sort($orderedPaths, [System.StringComparer]::Ordinal)
    $builder = [System.Text.StringBuilder]::new()
    foreach ($relativePath in $orderedPaths) {
        $record = $recordByPath[$relativePath]
        Add-CanonicalField -Builder $builder -Name "path" -Value $record.RelativePath
        Add-CanonicalField -Builder $builder -Name "length" -Value (
            $record.Length.ToString([System.Globalization.CultureInfo]::InvariantCulture))
        Add-CanonicalField -Builder $builder -Name "sha256" -Value (
            (Get-FileHash -LiteralPath $record.FullPath -Algorithm SHA256).Hash)
        [void]$builder.Append("`n")
    }
    return [pscustomobject]@{
        BuildArtifactId = Get-Sha256Text -Text $builder.ToString()
        ArtifactFileCount = $records.Count
    }
}

function Read-VerifiedBuildProvenance {
    param(
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string]$ExpectedRole,
        [Parameter(Mandatory = $true)][string]$ExpectedBuildKind
    )

    $artifactRoot = Split-Path -Parent $Executable
    $sidecarPath = Join-Path $artifactRoot "r2-perf1.build-provenance.json"
    if (-not (Test-Path -LiteralPath $sidecarPath -PathType Leaf)) {
        throw "Build provenance sidecar does not exist: $sidecarPath"
    }
    $sidecar = Get-Content -LiteralPath $sidecarPath -Raw | ConvertFrom-Json
    foreach ($name in @(
        "schemaVersion", "provenanceStatus", "measurementRole", "buildKind", "sourceCommit",
        "contentFingerprint", "buildConfigurationFingerprint", "buildArtifactId", "contentTrackingStatus")) {
        if ($sidecar.PSObject.Properties.Name -cnotcontains $name -or
            $sidecar.$name -isnot [string]) {
            throw "Build provenance field '$name' is missing or has the wrong type."
        }
    }
    foreach ($name in @(
        "sourceTreeClean", "developmentBuild", "autoRunPlayer", "autoConnectProfiler",
        "deepProfiling", "frameTimingStatsEnabled", "repositoryReproducible")) {
        if ($sidecar.PSObject.Properties.Name -cnotcontains $name -or
            $sidecar.$name -isnot [bool]) {
            throw "Build provenance field '$name' is missing or has the wrong type."
        }
    }
    if ($sidecar.PSObject.Properties.Name -cnotcontains "artifactFileCount" -or
        $sidecar.artifactFileCount -isnot [int]) {
        throw "Build provenance artifactFileCount is missing or has the wrong type."
    }
    if ([string]$sidecar.schemaVersion -cne "r2-perf1-build-provenance/1" -or
        [string]$sidecar.provenanceStatus -cne "valid" -or
        [string]$sidecar.measurementRole -cne $ExpectedRole -or
        [string]$sidecar.buildKind -cne $ExpectedBuildKind) {
        throw "Build provenance sidecar does not match the requested role/build contract."
    }
    if ([bool]$sidecar.autoRunPlayer -or [bool]$sidecar.autoConnectProfiler -or [bool]$sidecar.deepProfiling -or
        -not [bool]$sidecar.frameTimingStatsEnabled) {
        throw "Build provenance sidecar contains forbidden or incomplete build flags."
    }
    $expectedDevelopment = $ExpectedRole -ceq "development_gc"
    if ([bool]$sidecar.developmentBuild -ne $expectedDevelopment) {
        throw "Build provenance development flag does not match measurement role."
    }
    foreach ($name in @("sourceCommit", "contentFingerprint", "buildConfigurationFingerprint", "buildArtifactId")) {
        if ([string]$sidecar.$name -cnotmatch '^[0-9A-Fa-f]{64}$' -and $name -cne "sourceCommit") {
            throw "Build provenance field '$name' is not a SHA-256 value."
        }
    }
    if ([string]$sidecar.sourceCommit -cnotmatch '^[0-9A-Fa-f]{40}$') {
        throw "Build provenance sourceCommit is invalid."
    }
    $sidecar.sourceCommit = Convert-ToCanonicalSourceCommit -SourceCommit ([string]$sidecar.sourceCommit)
    $artifact = Get-BuildArtifactIdentity -ArtifactRoot $artifactRoot
    if ([string]$sidecar.buildArtifactId -cne $artifact.BuildArtifactId -or
        [int]$sidecar.artifactFileCount -ne $artifact.ArtifactFileCount) {
        throw "Build artifact identity no longer matches its provenance sidecar."
    }
    $scope = "repository_comparable"
    if ([string]$sidecar.contentTrackingStatus -ceq "local_ignored_content") {
        if ([bool]$sidecar.repositoryReproducible) {
            throw "local_ignored_content must not be repository reproducible."
        }
        $scope = "local_comparable"
    }
    return [pscustomobject]@{ Sidecar = $sidecar; Artifact = $artifact; ComparisonScope = $scope }
}

function Get-RuntimeConfigurationFingerprint {
    param([string]$Quality, [string]$Scenario, [string]$Antialiasing, [int]$RenderScalePercent,
        [string]$Upscaler, [int]$Width, [int]$Height, [int]$WarmupSeconds, [int]$SampleSeconds)
    $builder = [System.Text.StringBuilder]::new()
    foreach ($pair in @(
        @("quality", $Quality), @("scenario", $Scenario), @("aa", $Antialiasing),
        @("renderScale", [string]$RenderScalePercent), @("upscaler", $Upscaler),
        @("width", [string]$Width), @("height", [string]$Height),
        @("warmup", [string]$WarmupSeconds), @("sample", [string]$SampleSeconds),
        @("graphicsApi", "Direct3D11"), @("fullscreen", "false"))) {
        Add-CanonicalField -Builder $builder -Name $pair[0] -Value $pair[1]
    }
    return Get-Sha256Text -Text $builder.ToString()
}

function Get-HardwareFingerprint {
    param([string]$GpuName, [string]$DriverVersion)
    $builder = [System.Text.StringBuilder]::new()
    Add-CanonicalField -Builder $builder -Name "gpu" -Value $GpuName
    Add-CanonicalField -Builder $builder -Name "driver" -Value $DriverVersion
    Add-CanonicalField -Builder $builder -Name "cpu" -Value ([Environment]::GetEnvironmentVariable("PROCESSOR_IDENTIFIER"))
    Add-CanonicalField -Builder $builder -Name "machine" -Value ([Environment]::MachineName)
    Add-CanonicalField -Builder $builder -Name "os" -Value ([Environment]::OSVersion.VersionString)
    return Get-Sha256Text -Text $builder.ToString()
}

function Assert-BenchmarkApplicationsClosed {
    $blocked = @(Get-Process -Name "Unity", "Code" -ErrorAction SilentlyContinue)
    if ($blocked.Count -gt 0) {
        $details = $blocked |
            Sort-Object ProcessName, Id -Unique |
            ForEach-Object { "{0}.exe (PID {1})" -f $_.ProcessName, $_.Id }
        throw (
            "Offline benchmark preflight failed. Close Unity Editor and VS Code manually, " +
            "then run this script again. Active: " + ($details -join ", "))
    }
}

function Convert-ToArgumentString {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    return (($Arguments | ForEach-Object { Convert-ToWindowsCommandLineArgument $_ }) -join " ")
}

function Convert-ToWindowsCommandLineArgument {
    param([AllowEmptyString()][Parameter(Mandatory = $true)][string]$Argument)

    if ($Argument.IndexOf([char]0) -ge 0 -or
        $Argument.IndexOf("`r") -ge 0 -or
        $Argument.IndexOf("`n") -ge 0) {
        throw "Command-line arguments must not contain NUL or newline characters."
    }
    if ($Argument.Length -gt 0 -and $Argument -notmatch '[\s"]') {
        return $Argument
    }

    $builder = [System.Text.StringBuilder]::new()
    [void]$builder.Append('"')
    $backslashes = 0
    foreach ($character in $Argument.ToCharArray()) {
        if ($character -eq '\') {
            $backslashes++
            continue
        }
        if ($character -eq '"') {
            [void]$builder.Append(('\' * (($backslashes * 2) + 1)))
            [void]$builder.Append('"')
            $backslashes = 0
            continue
        }

        if ($backslashes -gt 0) {
            [void]$builder.Append(('\' * $backslashes))
            $backslashes = 0
        }
        [void]$builder.Append($character)
    }
    if ($backslashes -gt 0) {
        [void]$builder.Append(('\' * ($backslashes * 2)))
    }
    [void]$builder.Append('"')
    return $builder.ToString()
}

function Get-PowerPlanName {
    try {
        $line = (& powercfg /GETACTIVESCHEME 2>$null | Select-Object -First 1)
        return [string]$line
    }
    catch {
        return "UNKNOWN"
    }
}

function Get-PowerOnline {
    try {
        $states = @(Get-CimInstance -Namespace root/WMI -ClassName BatteryStatus)
        if ($states.Count -eq 0) {
            return $false
        }

        return [bool]($states | Where-Object { $_.PowerOnline } | Select-Object -First 1)
    }
    catch {
        return $false
    }
}

function Get-WindowsGpuPreference {
    param([Parameter(Mandatory = $true)][string]$Executable)

    try {
        $key = Get-ItemProperty -LiteralPath "HKCU:\Software\Microsoft\DirectX\UserGpuPreferences"
        $property = $key.PSObject.Properties[$Executable]
        if ($null -ne $property) {
            return [string]$property.Value
        }
    }
    catch {
    }

    return "UNKNOWN"
}

$NvidiaQuery = @(
    "timestamp",
    "index",
    "name",
    "driver_version",
    "pstate",
    "utilization.gpu",
    "utilization.memory",
    "temperature.gpu",
    "clocks.current.graphics",
    "clocks.current.memory",
    "power.draw",
    "clocks_event_reasons.sw_power_cap",
    "clocks_event_reasons.sw_thermal_slowdown",
    "clocks_event_reasons.hw_slowdown",
    "clocks_event_reasons.hw_thermal_slowdown",
    "clocks_event_reasons.hw_power_brake_slowdown"
)

function Get-NvidiaSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][string]$RunId
    )

    $query = $NvidiaQuery -join ","
    $rawLines = @(& nvidia-smi "--query-gpu=$query" "--format=csv,noheader,nounits" 2>$null)
    if ($LASTEXITCODE -ne 0 -or $rawLines.Count -eq 0) {
        throw "nvidia-smi telemetry query failed."
    }

    $records = @()
    foreach ($line in $rawLines) {
        $values = @($line | ConvertFrom-Csv -Header $NvidiaQuery)
        foreach ($value in $values) {
            $record = [ordered]@{
                runner_timestamp_utc = [DateTime]::UtcNow.ToString("O")
                phase = $Phase
                run_id = $RunId
            }
            foreach ($field in $NvidiaQuery) {
                $record[$field] = ([string]$value.$field).Trim()
            }
            $records += [pscustomobject]$record
        }
    }

    return $records
}

function Write-NvidiaSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $records = @(Get-NvidiaSnapshot -Phase $Phase -RunId $RunId)
    $records | Export-Csv -LiteralPath $Path -NoTypeInformation -Append -Encoding UTF8
    return $records
}

function Convert-ToNumber {
    param([object]$Value)

    $number = 0.0
    if ([double]::TryParse(
            [string]$Value,
            [System.Globalization.NumberStyles]::Float,
            [System.Globalization.CultureInfo]::InvariantCulture,
            [ref]$number)) {
        if ([double]::IsNaN($number) -or [double]::IsInfinity($number)) {
            return $null
        }
        return $number
    }

    return $null
}

function Get-NvidiaPmonSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Phase,
        [Parameter(Mandatory = $true)][string]$RunId
    )

    $rawLines = @(& nvidia-smi pmon -c 1 2>&1)
    if ($LASTEXITCODE -ne 0 -or $rawLines.Count -eq 0) {
        throw "nvidia-smi pmon query failed."
    }

    $processes = @()
    foreach ($line in $rawLines) {
        $text = ([string]$line).Trim()
        if ([string]::IsNullOrWhiteSpace($text) -or $text.StartsWith("#")) {
            continue
        }

        $columns = @($text -split '\s+')
        if ($columns.Count -lt 6) {
            continue
        }

        $pidValue = 0
        if (-not [int]::TryParse(
                $columns[1],
                [System.Globalization.NumberStyles]::Integer,
                [System.Globalization.CultureInfo]::InvariantCulture,
                [ref]$pidValue)) {
            continue
        }

        $processName = $columns[$columns.Count - 1]
        try {
            $processName = (Get-Process -Id $pidValue -ErrorAction Stop).ProcessName + ".exe"
        }
        catch {
        }

        $processes += [pscustomobject]@{
            runner_timestamp_utc = [DateTime]::UtcNow.ToString("O")
            phase = $Phase
            run_id = $RunId
            gpu_index = $columns[0]
            pid = $pidValue
            type = $columns[2]
            sm_utilization = Convert-ToNumber $columns[3]
            memory_utilization = Convert-ToNumber $columns[4]
            process_name = $processName
        }
    }

    return [pscustomobject]@{
        RawLines = $rawLines
        Processes = $processes
    }
}

function Get-SignificantExternalProcesses {
    param(
        [Parameter(Mandatory = $true)][object[]]$Processes,
        [int]$AllowedProcessId = -1
    )

    return @($Processes | Where-Object {
        $sm = Convert-ToNumber $_.sm_utilization
        $memory = Convert-ToNumber $_.memory_utilization
        [int]$_.pid -ne $AllowedProcessId -and
            (($null -ne $sm -and $sm -ge 5) -or
             ($null -ne $memory -and $memory -ge 5))
    } | Sort-Object pid, process_name -Unique)
}

function Invoke-GpuIdleGate {
    param(
        [Parameter(Mandatory = $true)][string]$GpuName,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$TelemetryPath,
        [Parameter(Mandatory = $true)][string]$PmonPath
    )

    $records = @()
    $processes = @()
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $sampleIndex = 0
    do {
        $records += @(Write-NvidiaSnapshot `
            -Phase "idle_gate" `
            -RunId $RunId `
            -Path $TelemetryPath)
        $pmon = Get-NvidiaPmonSnapshot -Phase "idle_gate" -RunId $RunId
        ("--- sample {0} at {1} ---" -f
            $sampleIndex,
            [DateTime]::UtcNow.ToString("O")) |
            Out-File -LiteralPath $PmonPath -Encoding utf8 -Append
        $pmon.RawLines |
            Out-File -LiteralPath $PmonPath -Encoding utf8 -Append
        $processes += @($pmon.Processes)
        $sampleIndex++
        if ($stopwatch.Elapsed.TotalSeconds -lt 5.0) {
            Start-Sleep -Milliseconds 250
        }
    } while ($stopwatch.Elapsed.TotalSeconds -lt 5.0 -or $sampleIndex -lt 5)
    $stopwatch.Stop()

    $gpuRecords = @($records | Where-Object { $_.name -like "*$GpuName*" })
    if ($gpuRecords.Count -lt 5) {
        return [pscustomobject]@{
            Valid = $false
            Reason = "GPU idle gate did not collect at least five expected-adapter samples"
            Records = $records
            Processes = $processes
            DurationSeconds = $stopwatch.Elapsed.TotalSeconds
        }
    }

    $throttleFields = @(
        "clocks_event_reasons.sw_thermal_slowdown",
        "clocks_event_reasons.hw_slowdown",
        "clocks_event_reasons.hw_thermal_slowdown",
        "clocks_event_reasons.hw_power_brake_slowdown"
    )
    foreach ($record in $gpuRecords) {
        foreach ($field in $throttleFields) {
            if ([string]$record.$field -eq "Active") {
                return [pscustomobject]@{
                    Valid = $false
                    Reason = "GPU idle gate found an active thermal/hardware throttle reason: $field"
                    Records = $records
                    Processes = $processes
                    DurationSeconds = $stopwatch.Elapsed.TotalSeconds
                }
            }
        }
    }

    $utilizations = @()
    foreach ($record in $gpuRecords) {
        $utilization = Convert-ToNumber $record.'utilization.gpu'
        if ($null -eq $utilization) {
            return [pscustomobject]@{
                Valid = $false
                Reason = "GPU idle gate contained a null or non-finite utilization value"
                Records = $records
                Processes = $processes
                DurationSeconds = $stopwatch.Elapsed.TotalSeconds
            }
        }
        $utilizations += $utilization
    }

    $averageUtilization = ($utilizations | Measure-Object -Average).Average
    $peakUtilization = ($utilizations | Measure-Object -Maximum).Maximum
    $busySamples = @($utilizations | Where-Object { $_ -gt 20 }).Count
    $external = @(Get-SignificantExternalProcesses -Processes $processes)
    if ($averageUtilization -gt 10 -or $busySamples -ge 3 -or $external.Count -gt 0) {
        $externalText = if ($external.Count -gt 0) {
            ($external | ForEach-Object {
                "{0} (PID {1}, sm {2}%, mem {3}%)" -f
                    $_.process_name,
                    $_.pid,
                    $_.sm_utilization,
                    $_.memory_utilization
            }) -join ", "
        }
        else {
            "no process was attributable through nvidia-smi pmon"
        }
        return [pscustomobject]@{
            Valid = $false
            Reason = (
                "MX550 was not idle for the five-second gate: average {0:F1}%, peak {1:F1}%, " +
                "busy samples {2}; external: {3}" -f
                $averageUtilization,
                $peakUtilization,
                $busySamples,
                $externalText)
            Records = $records
            Processes = $processes
            DurationSeconds = $stopwatch.Elapsed.TotalSeconds
        }
    }

    $temperatures = @()
    $graphicsClocks = @()
    foreach ($record in $gpuRecords) {
        $temperature = Convert-ToNumber $record.'temperature.gpu'
        $graphicsClock = Convert-ToNumber $record.'clocks.current.graphics'
        if ($null -eq $temperature -or $null -eq $graphicsClock) {
            return [pscustomobject]@{
                Valid = $false
                Reason = "GPU idle gate contained null or non-finite temperature/clock telemetry"
                Records = $records
                Processes = $processes
                DurationSeconds = $stopwatch.Elapsed.TotalSeconds
            }
        }
        $temperatures += $temperature
        $graphicsClocks += $graphicsClock
    }
    $pstates = @($gpuRecords | Select-Object -ExpandProperty pstate -Unique)
    return [pscustomobject]@{
        Valid = $true
        Reason = (
            "idle accepted: average {0:F1}%, peak {1:F1}%, temperature {2}-{3} C, " +
            "graphics clock {4}-{5} MHz, P-state {6}; P8/300 MHz is allowed while idle" -f
            $averageUtilization,
            $peakUtilization,
            ($temperatures | Measure-Object -Minimum).Minimum,
            ($temperatures | Measure-Object -Maximum).Maximum,
            ($graphicsClocks | Measure-Object -Minimum).Minimum,
            ($graphicsClocks | Measure-Object -Maximum).Maximum,
            ($pstates -join ","))
        Records = $records
        Processes = $processes
        DurationSeconds = $stopwatch.Elapsed.TotalSeconds
    }
}

function Test-Telemetry {
    param(
        [Parameter(Mandatory = $true)][object[]]$Records,
        [Parameter(Mandatory = $true)][string]$GpuName,
        [object[]]$ExternalProcesses = @(),
        [int]$ChildProcessId = -1
    )

    $during = @($Records | Where-Object {
        $_.phase -eq "during" -and $_.name -like "*$GpuName*"
    })
    if ($during.Count -eq 0) {
        return [pscustomobject]@{ Valid = $false; Reason = "missing NVIDIA telemetry during child process" }
    }

    $throttleFields = @(
        "clocks_event_reasons.sw_thermal_slowdown",
        "clocks_event_reasons.hw_slowdown",
        "clocks_event_reasons.hw_thermal_slowdown",
        "clocks_event_reasons.hw_power_brake_slowdown"
    )
    foreach ($record in $during) {
        foreach ($field in $throttleFields) {
            if ([string]$record.$field -eq "Active") {
                return [pscustomobject]@{
                    Valid = $false
                    Reason = "GPU throttle active during child process: $field"
                }
            }
        }

        $utilization = Convert-ToNumber $record.'utilization.gpu'
        $graphicsClock = Convert-ToNumber $record.'clocks.current.graphics'
        $memoryClock = Convert-ToNumber $record.'clocks.current.memory'
        $temperature = Convert-ToNumber $record.'temperature.gpu'
        $powerDraw = Convert-ToNumber $record.'power.draw'
        if ($null -eq $utilization -or
            $null -eq $graphicsClock -or
            $null -eq $memoryClock -or
            $null -eq $temperature -or
            $null -eq $powerDraw -or
            [string]::IsNullOrWhiteSpace([string]$record.pstate)) {
            return [pscustomobject]@{
                Valid = $false
                Reason = "NVIDIA telemetry contained a null, NaN, Infinity, or missing P-state"
            }
        }
        if ($utilization -ge 50 -and
            ($record.pstate -eq "P8" -or $graphicsClock -le 350)) {
            return [pscustomobject]@{
                Valid = $false
                Reason = "GPU remained at an abnormal P-state/low clock under high utilization"
            }
        }
    }

    $external = @(
        Get-SignificantExternalProcesses `
            -Processes $ExternalProcesses `
            -AllowedProcessId $ChildProcessId)
    if ($external.Count -gt 0) {
        $details = ($external | ForEach-Object {
            "{0} (PID {1}, sm {2}%, mem {3}%)" -f
                $_.process_name,
                $_.pid,
                $_.sm_utilization,
                $_.memory_utilization
        }) -join ", "
        return [pscustomobject]@{
            Valid = $false
            Reason = "external NVIDIA workload detected during child warm-up: $details"
        }
    }

    return [pscustomobject]@{ Valid = $true; Reason = "telemetry accepted" }
}

function Test-PathWithinDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Directory
    )

    try {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        $fullDirectory = [System.IO.Path]::GetFullPath($Directory)
        $prefix = $fullDirectory.TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar) +
            [System.IO.Path]::DirectorySeparatorChar
        return $fullPath.StartsWith(
            $prefix,
            [System.StringComparison]::OrdinalIgnoreCase)
    }
    catch {
        return $false
    }
}

function Get-DescendantProcessIds {
    param([Parameter(Mandatory = $true)][int]$RootProcessId)

    $all = @(Get-CimInstance Win32_Process |
        Select-Object ProcessId, ParentProcessId)
    $descendants = @()
    $parents = @($RootProcessId)
    do {
        $children = @($all | Where-Object {
            $parents -contains [int]$_.ParentProcessId -and
            $descendants -notcontains [int]$_.ProcessId
        } | ForEach-Object { [int]$_.ProcessId })
        if ($children.Count -eq 0) {
            break
        }
        $descendants += $children
        $parents = $children
    } while ($true)

    return $descendants
}

function Stop-BenchmarkProcessTree {
    param([Parameter(Mandatory = $true)][System.Diagnostics.Process]$Process)

    $descendants = @(Get-DescendantProcessIds -RootProcessId $Process.Id)
    foreach ($processId in @($descendants | Sort-Object -Descending)) {
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
    }
    Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue

    [void]$Process.WaitForExit(10000)
    $remaining = @(@($Process.Id) + $descendants) | Where-Object {
        $null -ne (Get-Process -Id $_ -ErrorAction SilentlyContinue)
    }
    return @($remaining)
}

function Wait-ForStableRunOutputs {
    param(
        [Parameter(Mandatory = $true)][string]$RunDirectory,
        [ValidateRange(1, 30)][int]$TimeoutSeconds = 5
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $temporaryFiles = @(Get-ChildItem `
            -LiteralPath $RunDirectory `
            -Filter "*.tmp" `
            -File `
            -ErrorAction SilentlyContinue)
        $manifestFiles = @(Get-ChildItem `
            -LiteralPath $RunDirectory `
            -Filter "*.manifest.json" `
            -File `
            -ErrorAction SilentlyContinue)
        if ($temporaryFiles.Count -eq 0 -and $manifestFiles.Count -eq 1) {
            try {
                $manifest = Get-Content -LiteralPath $manifestFiles[0].FullName -Raw |
                    ConvertFrom-Json
                if ($manifest.status -eq "awaiting_offline_validation" -and
                    (Test-PathWithinDirectory -Path $manifestFiles[0].FullName -Directory $RunDirectory) -and
                    (Test-PathWithinDirectory -Path $manifest.reportJsonPath -Directory $RunDirectory) -and
                    (Test-PathWithinDirectory -Path $manifest.reportMarkdownPath -Directory $RunDirectory) -and
                    (Test-Path -LiteralPath $manifest.reportJsonPath -PathType Leaf) -and
                    (Test-Path -LiteralPath $manifest.reportMarkdownPath -PathType Leaf)) {
                    $jsonLength = (Get-Item -LiteralPath $manifest.reportJsonPath).Length
                    $markdownLength = (Get-Item -LiteralPath $manifest.reportMarkdownPath).Length
                    Start-Sleep -Milliseconds 100
                    if ($jsonLength -eq (Get-Item -LiteralPath $manifest.reportJsonPath).Length -and
                        $markdownLength -eq (Get-Item -LiteralPath $manifest.reportMarkdownPath).Length) {
                        return $true
                    }
                }
            }
            catch {
            }
        }
        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    return $false
}

function Set-ManifestResult {
    param(
        [Parameter(Mandatory = $true)][string]$ManifestPath,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$Status,
        [Parameter(Mandatory = $true)][string]$Reason,
        [Nullable[int]]$ExitCode,
        [bool]$OutputsVerified,
        [bool]$TelemetryValid,
        [string]$EvidenceValidity = "INVALID",
        [string]$PerformanceBudgetStatus = "INCOMPLETE",
        [string]$GcBudgetStatus = "INCOMPLETE",
        [string]$AggregateProductGate = "INCOMPLETE",
        [bool]$PairingEligible = $false
    )

    if (Test-Path -LiteralPath $ManifestPath) {
        $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    }
    else {
        $manifest = [pscustomobject]@{
            schemaVersion = "r2-perf1-offline-manifest/1"
            runId = $RunId
        }
    }

    $manifest | Add-Member -NotePropertyName status -NotePropertyValue $Status -Force
    $manifest | Add-Member -NotePropertyName statusReason -NotePropertyValue $Reason -Force
    $manifest | Add-Member -NotePropertyName offlineValidatedUtc `
        -NotePropertyValue ([DateTime]::UtcNow.ToString("O")) -Force
    $manifest | Add-Member -NotePropertyName processExitCode -NotePropertyValue $ExitCode -Force
    $manifest | Add-Member -NotePropertyName outputsVerified -NotePropertyValue $OutputsVerified -Force
    $manifest | Add-Member -NotePropertyName telemetryValid -NotePropertyValue $TelemetryValid -Force
    $manifest | Add-Member -NotePropertyName evidenceValidity -NotePropertyValue $EvidenceValidity -Force
    $manifest | Add-Member -NotePropertyName performanceBudgetStatus -NotePropertyValue $PerformanceBudgetStatus -Force
    $manifest | Add-Member -NotePropertyName gcBudgetStatus -NotePropertyValue $GcBudgetStatus -Force
    $manifest | Add-Member -NotePropertyName aggregateProductGate -NotePropertyValue $AggregateProductGate -Force
    $manifest | Add-Member -NotePropertyName pairingEligible -NotePropertyValue $PairingEligible -Force
    $temporaryPath = $ManifestPath + ".offline.tmp"
    $manifest | ConvertTo-Json -Depth 12 |
        Set-Content -LiteralPath $temporaryPath -Encoding UTF8
    Move-Item -LiteralPath $temporaryPath -Destination $ManifestPath -Force
}

function New-ValidatorExceptionRunResult {
    param(
        [Parameter(Mandatory = $true)][string]$RunDirectory,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][System.Exception]$Exception,
        [Nullable[int]]$ExitCode,
        [Parameter(Mandatory = $true)][string]$Scenario,
        [Parameter(Mandatory = $true)][string]$Quality,
        [Parameter(Mandatory = $true)][string]$Antialiasing,
        [Parameter(Mandatory = $true)][int]$RenderScalePercent,
        [Parameter(Mandatory = $true)][string]$BuildKind,
        [Parameter(Mandatory = $true)][string]$MeasurementRole,
        [Parameter(Mandatory = $true)][bool]$MeasurementEligible
    )

    $reason = "offline validator exception: {0}: {1}" -f
        $Exception.GetType().FullName,
        $Exception.Message
    $offlineManifestPath = Join-Path $RunDirectory "offline.invalid.manifest.json"
    try {
        Set-ManifestResult `
            -ManifestPath $offlineManifestPath `
            -RunId $RunId `
            -Status "invalid" `
            -Reason $reason `
            -ExitCode $ExitCode `
            -OutputsVerified $false `
            -TelemetryValid $false
    }
    catch {
        $reason += "; failed to persist offline invalid manifest: {0}: {1}" -f
            $_.Exception.GetType().FullName,
            $_.Exception.Message
    }

    return [pscustomobject]@{
        runId = $RunId
        status = "invalid"
        exitCode = $ExitCode
        reason = $reason
        scenario = $Scenario
        quality = $Quality
        antialiasing = $Antialiasing
        renderScalePercent = $RenderScalePercent
        buildKind = $BuildKind
        measurementRole = $MeasurementRole
        measurementEligible = $MeasurementEligible
        evidenceValidity = "INVALID"
        performanceBudgetStatus = "INCOMPLETE"
        gcBudgetStatus = "INCOMPLETE"
        aggregateProductGate = "INCOMPLETE"
        pairingEligible = $false
        productBudgetStatus = "NOT_EVALUATED"
        productBudgetPassed = $null
        productBudgetFailure = ""
        report = ""
    }
}

function Test-FiniteMeasurementReport {
    param(
        [Parameter(Mandatory = $true)][object]$ScenarioResult,
        [Parameter(Mandatory = $true)][string]$SchemaVersion
    )

    $fields = @(
        "avgMs",
        "medianMs",
        "p95Ms",
        "p99Ms",
        "avgFps",
        "onePercentLowFps",
        "cpuTotalAvgMs",
        "cpuMainThreadAvgMs",
        "cpuRenderThreadAvgMs",
        "gpuAvgMs",
        "totalUsedMemoryAverageMb",
        "gfxUsedMemoryAverageMb",
        "textureMemoryAverageMb"
    )
    if ($SchemaVersion -eq "r2-perf1/2") {
        $fields += "globalGcAllocatedAverageBytes"
    }
    else {
        $fields += "gcAllocatedAverageBytes"
    }
    foreach ($field in $fields) {
        $property = $ScenarioResult.PSObject.Properties[$field]
        if ($null -eq $property -or $null -eq (Convert-ToNumber $property.Value)) {
            return [pscustomobject]@{
                Valid = $false
                Reason = "report metric is missing, null, NaN, or Infinity: $field"
            }
        }
    }

    return [pscustomobject]@{ Valid = $true; Reason = "finite metrics accepted" }
}

function New-ValidationCheck {
    param(
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Reason
    )

    return [pscustomobject]@{
        Passed = $Passed
        Reason = $Reason
    }
}

function New-JsonPropertyCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)]
        [ValidateSet("string", "boolean", "number")]
        [string]$ExpectedType,
        [switch]$MatchValue,
        [AllowNull()][object]$ExpectedValue
    )

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return New-ValidationCheck `
            -Passed $false `
            -Reason "missing required JSON property: $Name"
    }

    $value = $property.Value
    $typeMatches = switch ($ExpectedType) {
        "string" { $value -is [string] }
        "boolean" { $value -is [bool] }
        "number" {
            $value -is [byte] -or $value -is [sbyte] -or
            $value -is [int16] -or $value -is [uint16] -or
            $value -is [int32] -or $value -is [uint32] -or
            $value -is [int64] -or $value -is [uint64] -or
            $value -is [single] -or $value -is [double] -or
            $value -is [decimal]
        }
    }
    if (-not $typeMatches) {
        $actualType = if ($null -eq $value) { "null" } else { $value.GetType().FullName }
        return New-ValidationCheck `
            -Passed $false `
            -Reason "JSON property '$Name' has wrong type; expected $ExpectedType, found $actualType"
    }

    if ($ExpectedType -eq "number" -and $null -eq (Convert-ToNumber $value)) {
        return New-ValidationCheck `
            -Passed $false `
            -Reason "JSON property '$Name' must be a finite number"
    }

    if ($MatchValue) {
        $matches = if ($ExpectedType -eq "string") {
            [string]$value -ceq [string]$ExpectedValue
        }
        else {
            $value -eq $ExpectedValue
        }
        if (-not $matches) {
            return New-ValidationCheck `
                -Passed $false `
                -Reason "JSON property '$Name' has an unexpected exact value"
        }
    }

    return New-ValidationCheck -Passed $true -Reason "JSON property accepted: $Name"
}

function New-SourceCommitCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$ExpectedSourceCommit
    )

    $typeCheck = New-JsonPropertyCheck -Object $Object -Name "sourceCommit" -ExpectedType "string"
    if (-not $typeCheck.Passed) {
        return $typeCheck
    }
    try {
        $actual = Convert-ToCanonicalSourceCommit -SourceCommit ([string]$Object.sourceCommit)
        $expected = Convert-ToCanonicalSourceCommit -SourceCommit $ExpectedSourceCommit
    }
    catch {
        return New-ValidationCheck -Passed $false -Reason $_.Exception.Message
    }
    return New-ValidationCheck `
        -Passed ($actual -ceq $expected) `
        -Reason $(if ($actual -ceq $expected) {
            "canonical sourceCommit accepted"
        } else {
            "sourceCommit does not match the expected Git commit"
        })
}

function New-RequiredJsonStringCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return New-JsonPropertyCheck -Object $Object -Name $Name -ExpectedType "string"
}

function New-RequiredJsonBooleanCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return New-JsonPropertyCheck -Object $Object -Name $Name -ExpectedType "boolean"
}

function New-RequiredJsonNumberCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name
    )

    return New-JsonPropertyCheck -Object $Object -Name $Name -ExpectedType "number"
}

function New-RequiredJsonIntegerCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][long]$Minimum,
        [Parameter(Mandatory = $true)][long]$Maximum
    )

    $numberCheck = New-RequiredJsonNumberCheck -Object $Object -Name $Name
    if (-not $numberCheck.Passed) {
        return $numberCheck
    }

    $value = [Convert]::ToDouble(
        $Object.PSObject.Properties[$Name].Value,
        [Globalization.CultureInfo]::InvariantCulture)
    if ([Math]::Truncate($value) -ne $value -or
        $value -lt $Minimum -or
        $value -gt $Maximum) {
        return New-ValidationCheck `
            -Passed $false `
            -Reason "JSON property '$Name' must be an integral number in range [$Minimum, $Maximum]"
    }

    return New-ValidationCheck -Passed $true -Reason "JSON integer accepted: $Name"
}

function New-JsonStringEnumCheck {
    param(
        [Parameter(Mandatory = $true)][object]$Object,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$AllowedValues
    )

    $typeCheck = New-JsonPropertyCheck `
        -Object $Object `
        -Name $Name `
        -ExpectedType "string"
    if (-not $typeCheck.Passed) {
        return $typeCheck
    }

    $value = [string]$Object.PSObject.Properties[$Name].Value
    if (-not ($AllowedValues -ccontains $value)) {
        return New-ValidationCheck `
            -Passed $false `
            -Reason "JSON property '$Name' has unsupported or wrong-case value: $value"
    }

    return New-ValidationCheck -Passed $true -Reason "JSON enum accepted: $Name"
}

function Get-FirstFailedValidationCheck {
    param([Parameter(Mandatory = $true)][object[]]$Checks)

    foreach ($check in @($Checks)) {
        if ($null -eq $check -or
            $null -eq $check.PSObject.Properties["Passed"] -or
            $null -eq $check.PSObject.Properties["Reason"]) {
            return [pscustomobject]@{
                Passed = $false
                Reason = "validator produced a malformed named check"
            }
        }
        if (-not [bool]$check.Passed) {
            return $check
        }
    }

    return $null
}

function New-OutputValidationResult {
    param(
        [Parameter(Mandatory = $true)][bool]$Valid,
        [Parameter(Mandatory = $true)][string]$Reason,
        [AllowNull()][string]$ManifestPath,
        [AllowNull()][object]$Report,
        [string]$ProductBudgetStatus = "NOT_EVALUATED",
        [AllowNull()][Nullable[bool]]$ProductBudgetPassed,
        [string]$ProductBudgetFailure = "",
        [string]$EvidenceValidity = $(if ($Valid) { "VALID" } else { "INVALID" }),
        [string]$PerformanceBudgetStatus = "INCOMPLETE",
        [string]$PerformanceBudgetFailure = "",
        [string]$GcBudgetStatus = "INCOMPLETE",
        [string]$GcBudgetFailure = "",
        [string]$AggregateProductGate = "INCOMPLETE",
        [bool]$PairingEligible = $false,
        [string]$SchemaVersion = ""
    )

    return [pscustomobject]@{
        Valid = $Valid
        Reason = $Reason
        ManifestPath = $ManifestPath
        Report = $Report
        ProductBudgetStatus = $ProductBudgetStatus
        ProductBudgetPassed = $ProductBudgetPassed
        ProductBudgetFailure = $ProductBudgetFailure
        EvidenceValidity = $EvidenceValidity
        PerformanceBudgetStatus = $PerformanceBudgetStatus
        PerformanceBudgetFailure = $PerformanceBudgetFailure
        GcBudgetStatus = $GcBudgetStatus
        GcBudgetFailure = $GcBudgetFailure
        AggregateProductGate = $AggregateProductGate
        PairingEligible = $PairingEligible
        SchemaVersion = $SchemaVersion
    }
}

function Test-RunOutputs {
    param(
        [Parameter(Mandatory = $true)][string]$RunDirectory,
        [Parameter(Mandatory = $true)][string]$RunId,
        [Parameter(Mandatory = $true)][string]$ExpectedScenario,
        [Parameter(Mandatory = $true)][string]$ExpectedBuildKind,
        [string]$ExpectedMeasurementRole = "",
        [string]$ExpectedMeasurementSetId = "",
        [string]$ExpectedSourceCommit = "",
        [bool]$ExpectedSourceTreeClean = $false,
        [bool]$ExpectedSourceTreeCleanAvailable = $false,
        [string]$ExpectedBuildArtifactId = "",
        [string]$ExpectedContentFingerprint = "",
        [string]$ExpectedConfigurationFingerprint = "",
        [string]$ExpectedHardwareFingerprint = "",
        [bool]$ExpectedRepositoryReproducible = $false,
        [string]$ExpectedContentTrackingStatus = "",
        [string]$ExpectedComparisonScope = "",
        [Parameter(Mandatory = $true)][string]$ExpectedQuality,
        [Parameter(Mandatory = $true)][string]$ExpectedAntialiasing,
        [Parameter(Mandatory = $true)][int]$ExpectedRenderScalePercent,
        [Parameter(Mandatory = $true)][string]$ExpectedGpuName,
        [Parameter(Mandatory = $true)][int]$ExpectedWidth,
        [Parameter(Mandatory = $true)][int]$ExpectedHeight,
        [Parameter(Mandatory = $true)][bool]$ScreenshotExpected
    )

    $manifestFiles = @(Get-ChildItem -LiteralPath $RunDirectory -Filter "*.manifest.json" -File)
    if ($manifestFiles.Count -ne 1) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "expected exactly one runtime manifest, found $($manifestFiles.Count)"
    }

    $manifestPath = $manifestFiles[0].FullName
    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    }
    catch {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "runtime manifest is not valid JSON"
    }
    $manifestSchemaCheck = New-JsonPropertyCheck `
        -Object $manifest `
        -Name "schemaVersion" `
        -ExpectedType "string"
    if (-not $manifestSchemaCheck.Passed) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason ([string]$manifestSchemaCheck.Reason) `
            -ManifestPath $manifestPath
    }
    $manifestSchema = [string]$manifest.schemaVersion
    $isV1 = $manifestSchema -eq "r2-perf1-manifest/1"
    $isV2 = $manifestSchema -eq "r2-perf1-manifest/2"
    if (-not $isV1 -and -not $isV2) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "unsupported runtime manifest schema: $manifestSchema" `
            -ManifestPath $manifestPath `
            -SchemaVersion $manifestSchema
    }
    if ($isV2) {
        if ($ExpectedMeasurementRole -cnotin @("release_performance", "development_gc")) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "schema v2 requires an explicit supported measurement role" `
                -ManifestPath $manifestPath `
                -SchemaVersion $manifestSchema
        }
        $expectedRoleBuildKind = if ($ExpectedMeasurementRole -ceq "release_performance") {
            "release"
        }
        else {
            "diagnostic"
        }
        if ($ExpectedBuildKind -cne $expectedRoleBuildKind) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "measurement role and build kind do not match" `
                -ManifestPath $manifestPath `
                -SchemaVersion $manifestSchema
        }

        $manifestContractChecks = @(
            (New-JsonPropertyCheck -Object $manifest -Name "schemaVersion" -ExpectedType "string" -MatchValue -ExpectedValue "r2-perf1-manifest/2")
            (New-JsonPropertyCheck -Object $manifest -Name "status" -ExpectedType "string" -MatchValue -ExpectedValue "awaiting_offline_validation")
            (New-JsonPropertyCheck -Object $manifest -Name "runId" -ExpectedType "string" -MatchValue -ExpectedValue $RunId)
            (New-JsonPropertyCheck -Object $manifest -Name "scenario" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedScenario)
            (New-JsonStringEnumCheck -Object $manifest -Name "buildKind" -AllowedValues @("release", "diagnostic"))
            (New-JsonPropertyCheck -Object $manifest -Name "buildKind" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedBuildKind)
            (New-JsonPropertyCheck -Object $manifest -Name "quality" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedQuality)
            (New-JsonStringEnumCheck -Object $manifest -Name "antialiasing" -AllowedValues @("None", "FXAA", "SMAA", "TAA"))
            (New-JsonPropertyCheck -Object $manifest -Name "antialiasing" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedAntialiasing)
            (New-RequiredJsonIntegerCheck -Object $manifest -Name "renderScalePercent" -Minimum 50 -Maximum 100)
            (New-RequiredJsonStringCheck -Object $manifest -Name "upscaler")
            (New-RequiredJsonBooleanCheck -Object $manifest -Name "screenshotRequested")
            (New-RequiredJsonStringCheck -Object $manifest -Name "screenshotPath")
            (New-JsonPropertyCheck -Object $manifest -Name "measurementRole" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedMeasurementRole)
            (New-JsonPropertyCheck -Object $manifest -Name "measurementSetId" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedMeasurementSetId)
            (New-SourceCommitCheck -Object $manifest -ExpectedSourceCommit $ExpectedSourceCommit)
            (New-JsonPropertyCheck -Object $manifest -Name "sourceTreeClean" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedSourceTreeClean)
            (New-JsonPropertyCheck -Object $manifest -Name "sourceTreeCleanAvailable" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedSourceTreeCleanAvailable)
            (New-JsonPropertyCheck -Object $manifest -Name "buildArtifactId" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedBuildArtifactId)
            (New-JsonPropertyCheck -Object $manifest -Name "contentFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedContentFingerprint)
            (New-JsonPropertyCheck -Object $manifest -Name "configurationFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedConfigurationFingerprint)
            (New-JsonPropertyCheck -Object $manifest -Name "hardwareFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedHardwareFingerprint)
            (New-JsonPropertyCheck -Object $manifest -Name "repositoryReproducible" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedRepositoryReproducible)
            (New-JsonPropertyCheck -Object $manifest -Name "contentTrackingStatus" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedContentTrackingStatus)
            (New-JsonPropertyCheck -Object $manifest -Name "comparisonScope" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedComparisonScope)
            (New-JsonPropertyCheck -Object $manifest -Name "pairingEligible" -ExpectedType "boolean" -MatchValue -ExpectedValue $false)
            (New-JsonPropertyCheck -Object $manifest -Name "evidenceValidity" -ExpectedType "string" -MatchValue -ExpectedValue "PENDING_OFFLINE_VALIDATION")
            (New-JsonStringEnumCheck -Object $manifest -Name "performanceBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
            (New-JsonStringEnumCheck -Object $manifest -Name "gcBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
            (New-JsonPropertyCheck -Object $manifest -Name "aggregateProductGate" -ExpectedType "string" -MatchValue -ExpectedValue "INCOMPLETE")
            (New-RequiredJsonStringCheck -Object $manifest -Name "reportJsonPath")
            (New-RequiredJsonStringCheck -Object $manifest -Name "reportMarkdownPath")
        )
        $failedManifestContractCheck = Get-FirstFailedValidationCheck -Checks $manifestContractChecks
        if ($null -ne $failedManifestContractCheck) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason ([string]$failedManifestContractCheck.Reason) `
                -ManifestPath $manifestPath `
                -SchemaVersion $manifestSchema
        }
    }
    if ($manifest.runId -ne $RunId) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "manifest run-id mismatch; foreign manifest was not modified"
    }
    if ($manifest.status -ne "awaiting_offline_validation") {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "runtime manifest is not awaiting validation" `
            -ManifestPath $manifestPath
    }
    $manifestChecks = @(
        (New-ValidationCheck -Passed ($manifest.scenario -eq $ExpectedScenario) -Reason "manifest scenario mismatch")
        (New-ValidationCheck -Passed ($manifest.buildKind -eq $ExpectedBuildKind) -Reason "manifest build-kind mismatch")
        (New-ValidationCheck -Passed ($manifest.quality -eq $ExpectedQuality) -Reason "manifest quality mismatch")
        (New-ValidationCheck -Passed ($manifest.antialiasing -eq $ExpectedAntialiasing) -Reason "manifest AA mismatch")
        (New-ValidationCheck -Passed ([int]$manifest.renderScalePercent -eq $ExpectedRenderScalePercent) -Reason "manifest render-scale mismatch")
        (New-ValidationCheck -Passed ([bool]$manifest.screenshotRequested -eq $ScreenshotExpected) -Reason "manifest screenshot mode mismatch")
    )
    if ($isV2) {
        $manifestChecks += @(
            (New-ValidationCheck -Passed ([string]$manifest.measurementRole -ceq $ExpectedMeasurementRole) -Reason "manifest measurement-role mismatch")
            (New-ValidationCheck -Passed (-not [bool]$manifest.pairingEligible) -Reason "runtime member must await offline pairing eligibility")
            (New-ValidationCheck -Passed ([string]$manifest.aggregateProductGate -eq "INCOMPLETE") -Reason "member aggregate product gate must be INCOMPLETE")
            (New-ValidationCheck -Passed ([string]$manifest.evidenceValidity -eq "PENDING_OFFLINE_VALIDATION") -Reason "runtime manifest must await offline evidence validation")
        )
    }
    $failedManifestCheck = Get-FirstFailedValidationCheck -Checks $manifestChecks
    if ($null -ne $failedManifestCheck) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason ([string]$failedManifestCheck.Reason) `
            -ManifestPath $manifestPath
    }
    if ([string]::IsNullOrWhiteSpace([string]$manifest.reportJsonPath) -or
        [string]::IsNullOrWhiteSpace([string]$manifest.reportMarkdownPath)) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "manifest report path is missing" `
            -ManifestPath $manifestPath
    }
    if (-not (Test-PathWithinDirectory -Path $manifestPath -Directory $RunDirectory) -or
        -not (Test-PathWithinDirectory -Path $manifest.reportJsonPath -Directory $RunDirectory) -or
        -not (Test-PathWithinDirectory -Path $manifest.reportMarkdownPath -Directory $RunDirectory)) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "manifest or report path escapes the current run directory" `
            -ManifestPath $manifestPath
    }
    if (-not (Test-Path -LiteralPath $manifest.reportJsonPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $manifest.reportMarkdownPath -PathType Leaf)) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "missing JSON report" `
            -ManifestPath $manifestPath
    }

    try {
        $report = Get-Content -LiteralPath $manifest.reportJsonPath -Raw | ConvertFrom-Json
    }
    catch {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "runtime report is not valid JSON" `
            -ManifestPath $manifestPath
    }
    $reportSchemaCheck = New-JsonPropertyCheck `
        -Object $report `
        -Name "schemaVersion" `
        -ExpectedType "string"
    if (-not $reportSchemaCheck.Passed) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason ([string]$reportSchemaCheck.Reason) `
            -ManifestPath $manifestPath
    }
    $reportSchema = [string]$report.schemaVersion
    if (($isV1 -and $reportSchema -ne "r2-perf1/1") -or
        ($isV2 -and $reportSchema -ne "r2-perf1/2")) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason "runtime report/manifest schema mismatch" `
            -ManifestPath $manifestPath `
            -SchemaVersion $reportSchema
    }
    $expectedDevelopment = $ExpectedBuildKind -eq "diagnostic"
    if ($isV2) {
        $reportContractChecks = @(
            (New-JsonPropertyCheck -Object $report -Name "schemaVersion" -ExpectedType "string" -MatchValue -ExpectedValue "r2-perf1/2")
            (New-JsonPropertyCheck -Object $report -Name "benchmarkRunId" -ExpectedType "string" -MatchValue -ExpectedValue $RunId)
            (New-JsonPropertyCheck -Object $report -Name "benchmarkScenario" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedScenario)
            (New-JsonStringEnumCheck -Object $report -Name "buildKind" -AllowedValues @("release", "diagnostic"))
            (New-JsonPropertyCheck -Object $report -Name "buildKind" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedBuildKind)
            (New-JsonPropertyCheck -Object $report -Name "qualityLevel" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedQuality)
            (New-JsonStringEnumCheck -Object $report -Name "antialiasingMode" -AllowedValues @("None", "FXAA", "SMAA", "TAA"))
            (New-JsonPropertyCheck -Object $report -Name "antialiasingMode" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedAntialiasing)
            (New-RequiredJsonIntegerCheck -Object $report -Name "renderScalePercent" -Minimum 50 -Maximum 100)
            (New-RequiredJsonBooleanCheck -Object $report -Name "developmentBuild")
            (New-RequiredJsonIntegerCheck -Object $report -Name "width" -Minimum 1 -Maximum 32768)
            (New-RequiredJsonIntegerCheck -Object $report -Name "height" -Minimum 1 -Maximum 32768)
            (New-RequiredJsonStringCheck -Object $report -Name "graphicsDeviceName")
            (New-RequiredJsonStringCheck -Object $report -Name "graphicsDeviceType")
            (New-RequiredJsonBooleanCheck -Object $report -Name "profilerEnabled")
            (New-RequiredJsonBooleanCheck -Object $report -Name "profilerBinaryLogEnabled")
            (New-RequiredJsonBooleanCheck -Object $report -Name "deepProfilingBuild")
            (New-RequiredJsonBooleanCheck -Object $report -Name "measurementEligible")
            (New-RequiredJsonBooleanCheck -Object $report -Name "screenshotRequested")
            (New-JsonPropertyCheck -Object $report -Name "measurementRole" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedMeasurementRole)
            (New-JsonPropertyCheck -Object $report -Name "measurementSetId" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedMeasurementSetId)
            (New-SourceCommitCheck -Object $report -ExpectedSourceCommit $ExpectedSourceCommit)
            (New-JsonPropertyCheck -Object $report -Name "sourceTreeClean" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedSourceTreeClean)
            (New-JsonPropertyCheck -Object $report -Name "sourceTreeCleanAvailable" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedSourceTreeCleanAvailable)
            (New-JsonPropertyCheck -Object $report -Name "buildArtifactId" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedBuildArtifactId)
            (New-JsonPropertyCheck -Object $report -Name "contentFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedContentFingerprint)
            (New-JsonPropertyCheck -Object $report -Name "configurationFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedConfigurationFingerprint)
            (New-JsonPropertyCheck -Object $report -Name "hardwareFingerprint" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedHardwareFingerprint)
            (New-JsonPropertyCheck -Object $report -Name "repositoryReproducible" -ExpectedType "boolean" -MatchValue -ExpectedValue $ExpectedRepositoryReproducible)
            (New-JsonPropertyCheck -Object $report -Name "contentTrackingStatus" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedContentTrackingStatus)
            (New-JsonPropertyCheck -Object $report -Name "comparisonScope" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedComparisonScope)
            (New-JsonPropertyCheck -Object $report -Name "pairingEligible" -ExpectedType "boolean" -MatchValue -ExpectedValue $false)
            (New-JsonPropertyCheck -Object $report -Name "evidenceValidity" -ExpectedType "string" -MatchValue -ExpectedValue "PENDING_OFFLINE_VALIDATION")
            (New-JsonStringEnumCheck -Object $report -Name "performanceBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
            (New-JsonStringEnumCheck -Object $report -Name "gcBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
            (New-JsonPropertyCheck -Object $report -Name "aggregateProductGate" -ExpectedType "string" -MatchValue -ExpectedValue "INCOMPLETE")
        )
        $failedReportContractCheck = Get-FirstFailedValidationCheck -Checks $reportContractChecks
        if ($null -ne $failedReportContractCheck) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason ([string]$failedReportContractCheck.Reason) `
                -ManifestPath $manifestPath `
                -SchemaVersion $reportSchema
        }
    }
    $checks = @(
        (New-ValidationCheck -Passed ([string]$report.benchmarkRunId -ceq $RunId) -Reason "report run-id mismatch")
        (New-ValidationCheck -Passed ([string]$report.benchmarkScenario -ceq $ExpectedScenario) -Reason "report scenario mismatch")
        (New-ValidationCheck -Passed ([string]$report.buildKind -ceq $ExpectedBuildKind) -Reason "report build-kind mismatch")
        (New-ValidationCheck -Passed ([string]$report.qualityLevel -ceq $ExpectedQuality) -Reason "report quality mismatch")
        (New-ValidationCheck -Passed ([string]$report.antialiasingMode -ceq $ExpectedAntialiasing) -Reason "report AA mismatch")
        (New-ValidationCheck -Passed ([int]$report.renderScalePercent -eq $ExpectedRenderScalePercent) -Reason "report render-scale mismatch")
        (New-ValidationCheck -Passed ([bool]$report.developmentBuild -eq $expectedDevelopment) -Reason "actual build type does not match build-kind")
        (New-ValidationCheck -Passed ([int]$report.width -eq $ExpectedWidth) -Reason "standalone width mismatch")
        (New-ValidationCheck -Passed ([int]$report.height -eq $ExpectedHeight) -Reason "standalone height mismatch")
        (New-ValidationCheck -Passed ([string]$report.graphicsDeviceName -like "*$ExpectedGpuName*") -Reason "standalone GPU mismatch")
        (New-ValidationCheck -Passed ([string]$report.graphicsDeviceType -ceq "Direct3D11") -Reason "standalone graphics API mismatch")
    )
    if ($isV2) {
        $checks += @(
            (New-ValidationCheck -Passed ([string]$report.measurementRole -ceq $ExpectedMeasurementRole) -Reason "report measurement-role mismatch")
            (New-ValidationCheck -Passed (-not [bool]$report.pairingEligible) -Reason "runtime report must await offline pairing eligibility")
            (New-ValidationCheck -Passed ([string]$report.aggregateProductGate -ceq "INCOMPLETE") -Reason "report aggregate product gate must be INCOMPLETE")
            (New-ValidationCheck -Passed ([string]$report.evidenceValidity -ceq "PENDING_OFFLINE_VALIDATION") -Reason "runtime report must await offline evidence validation")
            (New-ValidationCheck -Passed ([string]$report.measurementSetId -ceq $ExpectedMeasurementSetId) -Reason "report measurement-set mismatch")
            (New-ValidationCheck -Passed (
                (Convert-ToCanonicalSourceCommit -SourceCommit ([string]$report.sourceCommit)) -ceq
                (Convert-ToCanonicalSourceCommit -SourceCommit $ExpectedSourceCommit)) -Reason "report source-commit mismatch")
            (New-ValidationCheck -Passed ([bool]$report.sourceTreeCleanAvailable -eq $ExpectedSourceTreeCleanAvailable) -Reason "report source-tree availability mismatch")
        )
    }
    if ($ExpectedBuildKind -eq "release" -or $isV2) {
        $checks += @(
            (New-ValidationCheck -Passed (-not [bool]$report.profilerEnabled) -Reason "Profiler must be disabled for authoritative member evidence")
            (New-ValidationCheck -Passed (-not [bool]$report.profilerBinaryLogEnabled) -Reason "Profiler binary logging must be disabled for authoritative member evidence")
            (New-ValidationCheck -Passed (-not [bool]$report.deepProfilingBuild) -Reason "Deep Profiling must be disabled for authoritative member evidence")
        )
    }
    $failedReportCheck = Get-FirstFailedValidationCheck -Checks $checks
    if ($null -ne $failedReportCheck) {
        return New-OutputValidationResult `
            -Valid $false `
            -Reason ([string]$failedReportCheck.Reason) `
            -ManifestPath $manifestPath
    }

    $productBudgetStatus = "NOT_APPLICABLE"
    $productBudgetPassed = $null
    $productBudgetFailure = ""
    $performanceBudgetStatus = "INCOMPLETE"
    $performanceBudgetFailure = ""
    $gcBudgetStatus = "INCOMPLETE"
    $gcBudgetFailure = ""
    if ($ScreenshotExpected) {
        if ([bool]$report.measurementEligible -or @($report.scenarios).Count -ne 0) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "visual run contains product measurements" `
                -ManifestPath $manifestPath
        }
        if ([string]::IsNullOrWhiteSpace([string]$manifest.screenshotPath) -or
            -not (Test-PathWithinDirectory -Path $manifest.screenshotPath -Directory $RunDirectory) -or
            -not (Test-Path -LiteralPath $manifest.screenshotPath -PathType Leaf)) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "visual run screenshot missing" `
                -ManifestPath $manifestPath
        }
    }
    else {
        if (-not [bool]$report.measurementEligible) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "measurement run is not marked measurement-eligible" `
                -ManifestPath $manifestPath
        }

        $scenarios = @($report.scenarios)
        if ($isV2) {
            foreach ($candidateScenario in $scenarios) {
                $candidateNameCheck = New-RequiredJsonStringCheck `
                    -Object $candidateScenario `
                    -Name "name"
                if (-not $candidateNameCheck.Passed) {
                    return New-OutputValidationResult `
                        -Valid $false `
                        -Reason ([string]$candidateNameCheck.Reason) `
                        -ManifestPath $manifestPath `
                        -SchemaVersion $reportSchema
                }
            }
        }
        $matchingScenarios = @($scenarios | Where-Object {
            if ($isV2) {
                [string]$_.name -ceq $ExpectedScenario
            }
            else {
                [string]$_.name -eq $ExpectedScenario
            }
        })
        if ($matchingScenarios.Count -eq 0) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "expected measured scenario was not found: $ExpectedScenario" `
                -ManifestPath $manifestPath
        }
        if ($matchingScenarios.Count -gt 1) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "expected measured scenario is duplicated: $ExpectedScenario" `
                -ManifestPath $manifestPath
        }
        if ($scenarios.Count -ne 1) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason "measurement report contains unexpected extra scenarios" `
                -ManifestPath $manifestPath
        }

        $scenarioResult = $matchingScenarios[0]
        if ($isV2) {
            $scenarioContractChecks = @(
                (New-JsonPropertyCheck -Object $scenarioResult -Name "name" -ExpectedType "string" -MatchValue -ExpectedValue $ExpectedScenario)
                (New-JsonStringEnumCheck -Object $scenarioResult -Name "performanceBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
                (New-RequiredJsonStringCheck -Object $scenarioResult -Name "performanceBudgetFailure")
                (New-JsonStringEnumCheck -Object $scenarioResult -Name "gcBudgetStatus" -AllowedValues @("PASS", "FAIL", "NOT_AUTHORITY", "INCOMPLETE"))
                (New-RequiredJsonStringCheck -Object $scenarioResult -Name "gcBudgetFailure")
                (New-RequiredJsonStringCheck -Object $scenarioResult -Name "globalGcMetricSource")
                (New-RequiredJsonStringCheck -Object $scenarioResult -Name "globalGcMetricScope")
                (New-RequiredJsonStringCheck -Object $scenarioResult -Name "bottleneck")
                (New-RequiredJsonIntegerCheck -Object $scenarioResult -Name "frames" -Minimum 0 -Maximum ([int]::MaxValue))
                (New-RequiredJsonIntegerCheck -Object $scenarioResult -Name "timingSamples" -Minimum 0 -Maximum ([int]::MaxValue))
                (New-RequiredJsonIntegerCheck -Object $scenarioResult -Name "ignoredStartupStallFrames" -Minimum 0 -Maximum ([int]::MaxValue))
                (New-RequiredJsonIntegerCheck -Object $scenarioResult -Name "globalGcAllocatedPeakBytes" -Minimum 0 -Maximum ([long]::MaxValue))
            )
            foreach ($booleanField in @(
                    "drawCallsAvailable",
                    "batchesAvailable",
                    "setPassCallsAvailable",
                    "trianglesAvailable",
                    "verticesAvailable",
                    "globalGcAllocationAvailable",
                    "globalGcDiagnosticOnly",
                    "totalUsedMemoryAvailable",
                    "gfxUsedMemoryAvailable",
                    "textureMemoryAvailable")) {
                $scenarioContractChecks += New-RequiredJsonBooleanCheck `
                    -Object $scenarioResult `
                    -Name $booleanField
            }
            foreach ($numericField in @(
                    "avgMs",
                    "medianMs",
                    "p95Ms",
                    "p99Ms",
                    "avgFps",
                    "onePercentLowFps",
                    "cpuTotalAvgMs",
                    "cpuTotalP95Ms",
                    "cpuMainThreadAvgMs",
                    "cpuMainThreadP95Ms",
                    "cpuMainThreadPresentWaitAvgMs",
                    "cpuMainThreadWorkAvgMs",
                    "cpuRenderThreadAvgMs",
                    "cpuRenderThreadP95Ms",
                    "gpuAvgMs",
                    "gpuP95Ms",
                    "drawCalls",
                    "batches",
                    "setPassCalls",
                    "triangleMillions",
                    "vertexMillions",
                    "globalGcAllocatedAverageBytes",
                    "globalGcAllocationCountAverage",
                    "totalUsedMemoryAverageMb",
                    "totalUsedMemoryPeakMb",
                    "gfxUsedMemoryAverageMb",
                    "gfxUsedMemoryPeakMb",
                    "textureMemoryAverageMb",
                    "textureMemoryPeakMb")) {
                $scenarioContractChecks += New-RequiredJsonNumberCheck `
                    -Object $scenarioResult `
                    -Name $numericField
            }
            $failedScenarioContractCheck = Get-FirstFailedValidationCheck -Checks $scenarioContractChecks
            if ($null -ne $failedScenarioContractCheck) {
                return New-OutputValidationResult `
                    -Valid $false `
                    -Reason ([string]$failedScenarioContractCheck.Reason) `
                    -ManifestPath $manifestPath `
                    -SchemaVersion $reportSchema
            }
        }
        $finite = Test-FiniteMeasurementReport `
            -ScenarioResult $scenarioResult `
            -SchemaVersion $reportSchema
        if (-not $finite.Valid) {
            return New-OutputValidationResult `
                -Valid $false `
                -Reason ([string]$finite.Reason) `
                -ManifestPath $manifestPath
        }

        if ($isV1) {
            $requiredAvailabilityFields = @(
                "drawCallsAvailable",
                "batchesAvailable",
                "setPassCallsAvailable",
                "trianglesAvailable",
                "verticesAvailable",
                "gcAllocationAvailable",
                "totalUsedMemoryAvailable",
                "gfxUsedMemoryAvailable",
                "textureMemoryAvailable"
            )
            foreach ($availabilityField in $requiredAvailabilityFields) {
                $availabilityProperty = $scenarioResult.PSObject.Properties[$availabilityField]
                if ($null -eq $availabilityProperty -or -not [bool]$availabilityProperty.Value) {
                    return New-OutputValidationResult `
                        -Valid $false `
                        -Reason "required metric is unavailable: $availabilityField" `
                        -ManifestPath $manifestPath `
                        -SchemaVersion $reportSchema
                }
            }

            $budgetStatusProperty = $scenarioResult.PSObject.Properties["budgetStatus"]
            if ($null -eq $budgetStatusProperty) {
                return New-OutputValidationResult `
                    -Valid $false `
                    -Reason "measurement report is missing budgetStatus" `
                    -ManifestPath $manifestPath `
                    -SchemaVersion $reportSchema
            }
            $productBudgetStatus = [string]$budgetStatusProperty.Value
            $productBudgetFailure = [string]$scenarioResult.budgetFailure
            if ($productBudgetStatus -eq "INCOMPLETE") {
                return New-OutputValidationResult `
                    -Valid $false `
                    -Reason ("measurement completeness gate failed: " + $productBudgetFailure) `
                    -ManifestPath $manifestPath `
                    -ProductBudgetStatus $productBudgetStatus `
                    -ProductBudgetPassed $false `
                    -ProductBudgetFailure $productBudgetFailure `
                    -SchemaVersion $reportSchema
            }
            if ($productBudgetStatus -notin @("PASS", "FAIL")) {
                return New-OutputValidationResult `
                    -Valid $false `
                    -Reason "measurement report has unsupported budgetStatus: $productBudgetStatus" `
                    -ManifestPath $manifestPath `
                    -ProductBudgetStatus $productBudgetStatus `
                    -ProductBudgetFailure $productBudgetFailure `
                    -SchemaVersion $reportSchema
            }
            $productBudgetPassed = $productBudgetStatus -eq "PASS"
        }
        else {
            $performanceBudgetStatus = [string]$scenarioResult.performanceBudgetStatus
            $performanceBudgetFailure = [string]$scenarioResult.performanceBudgetFailure
            $gcBudgetStatus = [string]$scenarioResult.gcBudgetStatus
            $gcBudgetFailure = [string]$scenarioResult.gcBudgetFailure
            $statusChecks = @(
                (New-ValidationCheck -Passed ([string]$report.performanceBudgetStatus -ceq $performanceBudgetStatus) -Reason "report/scenario performance budget mismatch")
                (New-ValidationCheck -Passed ([string]$report.gcBudgetStatus -ceq $gcBudgetStatus) -Reason "report/scenario GC budget mismatch")
                (New-ValidationCheck -Passed ([string]$manifest.performanceBudgetStatus -ceq $performanceBudgetStatus) -Reason "manifest/scenario performance budget mismatch")
                (New-ValidationCheck -Passed ([string]$manifest.gcBudgetStatus -ceq $gcBudgetStatus) -Reason "manifest/scenario GC budget mismatch")
                (New-ValidationCheck -Passed ([string]$report.aggregateProductGate -ceq "INCOMPLETE") -Reason "individual member aggregate gate must remain INCOMPLETE")
            )
            if ($ExpectedMeasurementRole -ceq "release_performance") {
                $statusChecks += @(
                    (New-ValidationCheck -Passed (@("PASS", "FAIL") -ccontains $performanceBudgetStatus) -Reason "release performance status must be PASS or FAIL")
                    (New-ValidationCheck -Passed ($gcBudgetStatus -ceq "NOT_AUTHORITY") -Reason "release GC status must be NOT_AUTHORITY")
                )
                $requiredAvailabilityFields = @(
                    "drawCallsAvailable",
                    "batchesAvailable",
                    "setPassCallsAvailable",
                    "trianglesAvailable",
                    "verticesAvailable",
                    "totalUsedMemoryAvailable",
                    "gfxUsedMemoryAvailable",
                    "textureMemoryAvailable"
                )
            }
            else {
                $statusChecks += @(
                    (New-ValidationCheck -Passed ($performanceBudgetStatus -ceq "NOT_AUTHORITY") -Reason "development GC performance status must be NOT_AUTHORITY")
                    (New-ValidationCheck -Passed (@("PASS", "FAIL") -ccontains $gcBudgetStatus) -Reason "development GC status must be PASS or FAIL")
                    (New-ValidationCheck -Passed ([bool]$scenarioResult.globalGcAllocationAvailable) -Reason "authoritative global GC recorder is unavailable")
                    (New-ValidationCheck -Passed ([string]$scenarioResult.globalGcMetricSource -eq "unity_profiler_recorder") -Reason "global GC metric source is not authoritative")
                    (New-ValidationCheck -Passed ([string]$scenarioResult.globalGcMetricScope -eq "unity_gc_allocated_in_frame") -Reason "global GC metric scope changed")
                    (New-ValidationCheck -Passed (-not [bool]$scenarioResult.globalGcDiagnosticOnly) -Reason "authoritative global GC metric cannot be diagnostic-only")
                )
                $requiredAvailabilityFields = @("globalGcAllocationAvailable")
            }
            $failedStatusCheck = Get-FirstFailedValidationCheck -Checks $statusChecks
            if ($null -ne $failedStatusCheck) {
                return New-OutputValidationResult `
                    -Valid $false `
                    -Reason ([string]$failedStatusCheck.Reason) `
                    -ManifestPath $manifestPath `
                    -PerformanceBudgetStatus $performanceBudgetStatus `
                    -PerformanceBudgetFailure $performanceBudgetFailure `
                    -GcBudgetStatus $gcBudgetStatus `
                    -GcBudgetFailure $gcBudgetFailure `
                    -SchemaVersion $reportSchema
            }
            foreach ($availabilityField in $requiredAvailabilityFields) {
                $availabilityProperty = $scenarioResult.PSObject.Properties[$availabilityField]
                if ($null -eq $availabilityProperty -or -not [bool]$availabilityProperty.Value) {
                    return New-OutputValidationResult `
                        -Valid $false `
                        -Reason "required metric is unavailable: $availabilityField" `
                        -ManifestPath $manifestPath `
                        -PerformanceBudgetStatus $performanceBudgetStatus `
                        -PerformanceBudgetFailure $performanceBudgetFailure `
                        -GcBudgetStatus $gcBudgetStatus `
                        -GcBudgetFailure $gcBudgetFailure `
                        -SchemaVersion $reportSchema
                }
            }
        }
    }

    $acceptedReason = if ($isV2 -and
        ($performanceBudgetStatus -eq "FAIL" -or $gcBudgetStatus -eq "FAIL")) {
        "outputs accepted; authoritative member budget failed"
    }
    elseif ($productBudgetStatus -eq "FAIL") {
        "outputs accepted; legacy product budget failed: $productBudgetFailure"
    }
    else {
        "outputs accepted"
    }
    return New-OutputValidationResult `
        -Valid $true `
        -Reason $acceptedReason `
        -ManifestPath $manifestPath `
        -Report $report `
        -ProductBudgetStatus $productBudgetStatus `
        -ProductBudgetPassed $productBudgetPassed `
        -ProductBudgetFailure $productBudgetFailure `
        -EvidenceValidity "VALID" `
        -PerformanceBudgetStatus $performanceBudgetStatus `
        -PerformanceBudgetFailure $performanceBudgetFailure `
        -GcBudgetStatus $gcBudgetStatus `
        -GcBudgetFailure $gcBudgetFailure `
        -AggregateProductGate "INCOMPLETE" `
        -PairingEligible $false `
        -SchemaVersion $reportSchema
}

function Write-ResultTables {
    param(
        [Parameter(Mandatory = $true)][object[]]$Results,
        [Parameter(Mandatory = $true)][string]$Directory
    )

    $valid = @($Results | Where-Object {
        $_.status -eq "valid" -and $_.measurementEligible
    })
    $visual = @($Results | Where-Object {
        $_.status -eq "valid" -and -not $_.measurementEligible
    })
    $invalid = @($Results | Where-Object { $_.status -ne "valid" })
    $valid | Export-Csv -LiteralPath (Join-Path $Directory "valid-runs.csv") -NoTypeInformation -Encoding UTF8
    $visual | Export-Csv -LiteralPath (Join-Path $Directory "valid-visual-runs.csv") -NoTypeInformation -Encoding UTF8
    $invalid | Export-Csv -LiteralPath (Join-Path $Directory "invalid-runs.csv") -NoTypeInformation -Encoding UTF8

    $lines = @(
        "# R2-PERF1 offline result summary",
        "",
        "## Valid runs",
        "",
        "| Run | Exit | Status | Reason |",
        "|---|---:|---|---|"
    )
    foreach ($result in $valid) {
        $lines += "| $($result.runId) | $($result.exitCode) | valid | $($result.reason) |"
    }
    if ($valid.Count -eq 0) { $lines += "| none | | | |" }
    $lines += @("", "## Valid visual-only runs (excluded from product metrics)", "", "| Run | Exit | Status | Reason |", "|---|---:|---|---|")
    foreach ($result in $visual) {
        $lines += "| $($result.runId) | $($result.exitCode) | valid visual | $($result.reason) |"
    }
    if ($visual.Count -eq 0) { $lines += "| none | | | |" }
    $lines += @("", "## Invalid runs", "", "| Run | Exit | Status | Reason |", "|---|---:|---|---|")
    foreach ($result in $invalid) {
        $lines += "| $($result.runId) | $($result.exitCode) | invalid | $($result.reason) |"
    }
    if ($invalid.Count -eq 0) { $lines += "| none | | | |" }
    $lines | Set-Content -LiteralPath (Join-Path $Directory "summary.md") -Encoding UTF8
}

$Executable = Resolve-ProjectPath $ExecutablePath
$OutputDirectory = Resolve-ProjectPath $OutputRoot
if ($MeasurementRole -cnotin @("release_performance", "development_gc")) {
    throw "MeasurementRole expects exactly release_performance or development_gc."
}
$requiredBuildKind = if ($MeasurementRole -ceq "release_performance") {
    "release"
}
else {
    "diagnostic"
}
if ($BuildKind -cne $requiredBuildKind) {
    throw "MeasurementRole '$MeasurementRole' requires BuildKind '$requiredBuildKind'."
}
if (-not $DryRun -and -not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
    throw "Benchmark executable does not exist: $Executable"
}
$buildProvenance = $null
$sidecar = $null
if ($DryRun) {
    $sidecar = [pscustomobject]@{
        sourceCommit = "0000000000000000000000000000000000000000"
        sourceTreeClean = $false
        buildArtifactId = "0000000000000000000000000000000000000000000000000000000000000000"
        contentFingerprint = "0000000000000000000000000000000000000000000000000000000000000000"
        repositoryReproducible = $false
        contentTrackingStatus = "dry_run_unverified"
    }
    $buildProvenance = [pscustomobject]@{ ComparisonScope = "dry_run_unverified" }
}
else {
    $buildProvenance = Read-VerifiedBuildProvenance `
        -Executable $Executable `
        -ExpectedRole $MeasurementRole `
        -ExpectedBuildKind $BuildKind
    $sidecar = $buildProvenance.Sidecar
}
$configurationFingerprint = Get-RuntimeConfigurationFingerprint `
    -Quality $Quality `
    -Scenario $Scenario `
    -Antialiasing $Antialiasing `
    -RenderScalePercent $RenderScalePercent `
    -Upscaler $Upscaler `
    -Width $Width `
    -Height $Height `
    -WarmupSeconds $WarmupSeconds `
    -SampleSeconds $SampleSeconds
if ($CaptureScreenshot -and $Runs -ne 1) {
    throw "Visual capture is a separate run and requires -Runs 1."
}
if (-not $DryRun) {
    Assert-BenchmarkApplicationsClosed
}

$powerPlan = Get-PowerPlanName
$powerOnline = Get-PowerOnline
$gpuPreference = Get-WindowsGpuPreference -Executable $Executable
$driverVersion = "DRY_RUN_UNRECORDED"
if (-not $DryRun) {
    if (-not $powerOnline) {
        throw "Offline benchmark requires AC power. Connect the charger and retry."
    }
    if ($powerPlan -notmatch "High performance|Ultimate Performance") {
        throw "Offline benchmark requires a High Performance power plan. Active: $powerPlan"
    }

    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    $initialGpu = @(Get-NvidiaSnapshot -Phase "preflight" -RunId "preflight" |
        Where-Object { $_.name -like "*$ExpectedGpu*" } | Select-Object -First 1)
    if ($initialGpu.Count -ne 1) {
        throw "Expected NVIDIA adapter was not found: $ExpectedGpu"
    }
    $driverVersion = [string]$initialGpu[0].driver_version
}
$hardwareFingerprint = Get-HardwareFingerprint -GpuName $ExpectedGpu -DriverVersion $driverVersion
$results = @()

for ($run = 1; $run -le $Runs; $run++) {
    $timestamp = [DateTime]::UtcNow.ToString("yyyyMMddTHHmmssfffZ")
    $runId = New-BoundedRunId `
        -MeasurementRole $MeasurementRole `
        -BuildKind $BuildKind `
        -Quality $Quality `
        -Scenario $Scenario `
        -RenderScalePercent $RenderScalePercent `
        -RunNumber $run `
        -Timestamp $timestamp
    $runDirectory = Join-Path $OutputDirectory $runId
    if ((Split-Path -Leaf $runDirectory) -cne $runId) {
        throw "Run directory leaf does not exactly match its bounded run ID."
    }
    $telemetryPath = Join-Path $runDirectory "nvidia-smi.csv"
    $playerLogPath = Join-Path $runDirectory "player.log"

    $arguments = @(
        "-screen-width", [string]$Width,
        "-screen-height", [string]$Height,
        "-screen-fullscreen", "0",
        "-force-d3d11",
        "-logFile", $playerLogPath,
        "-sotf-perf1",
        "-sotf-quality", $Quality,
        "-sotf-scenario", $Scenario,
        "-sotf-run-id", $runId,
        "-sotf-build-kind", $BuildKind,
        "-sotf-measurement-role", $MeasurementRole,
        "-sotf-measurement-set-id", $MeasurementSetId,
        "-sotf-source-commit", ([string]$sidecar.sourceCommit),
        "-sotf-source-tree-clean", ([string]([bool]$sidecar.sourceTreeClean)).ToLowerInvariant(),
        "-sotf-build-artifact-id", ([string]$sidecar.buildArtifactId),
        "-sotf-content-fingerprint", ([string]$sidecar.contentFingerprint),
        "-sotf-configuration-fingerprint", $configurationFingerprint,
        "-sotf-hardware-fingerprint", $hardwareFingerprint,
        "-sotf-repository-reproducible", ([string]([bool]$sidecar.repositoryReproducible)).ToLowerInvariant(),
        "-sotf-content-tracking-status", ([string]$sidecar.contentTrackingStatus),
        "-sotf-comparison-scope", ([string]$buildProvenance.ComparisonScope),
        "-sotf-no-screenshot", ([string](-not $CaptureScreenshot)).ToLowerInvariant(),
        "-sotf-aa", $Antialiasing,
        "-sotf-render-scale", [string]$RenderScalePercent,
        "-sotf-upscaler", $Upscaler,
        "-sotf-warmup", [string]$WarmupSeconds,
        "-sotf-sample", [string]$SampleSeconds,
        "-sotf-driver", $driverVersion,
        "-sotf-power-plan", $powerPlan,
        "-sotf-power-online", ([string]$powerOnline).ToLowerInvariant(),
        "-sotf-gpu-preference", $gpuPreference,
        "-sotf-output-directory", $runDirectory
    )
    $argumentString = Convert-ToArgumentString $arguments

    if ($DryRun) {
        Write-Host "DRY RUN [$run/$Runs]"
        Write-Host ('& "{0}" {1}' -f $Executable, $argumentString)
        continue
    }

    New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
    Assert-BenchmarkApplicationsClosed
    $idlePmonPath = Join-Path $runDirectory "nvidia-pmon-idle.txt"
    $idleGate = Invoke-GpuIdleGate `
        -GpuName $ExpectedGpu `
        -RunId $runId `
        -TelemetryPath $telemetryPath `
        -PmonPath $idlePmonPath
    $idleGate |
        Select-Object Valid, Reason, DurationSeconds |
        ConvertTo-Json |
        Set-Content -LiteralPath (Join-Path $runDirectory "gpu-idle-gate.json") -Encoding UTF8
    if (-not $idleGate.Valid) {
        throw (
            "GPU idle gate failed before launching a child process: " +
            $idleGate.Reason +
            ". Evidence: " +
            (Join-Path $runDirectory "gpu-idle-gate.json"))
    }

    $telemetry = @($idleGate.Records)
    $telemetry += @(Get-NvidiaSnapshot -Phase "before" -RunId $runId)
    (& nvidia-smi pmon -c 1 2>&1) | Out-File -LiteralPath (Join-Path $runDirectory "nvidia-pmon-before.txt") -Encoding utf8

    $process = Start-Process -FilePath $Executable -ArgumentList $argumentString -PassThru
    $startedUtc = [DateTime]::UtcNow
    $timedOut = $false
    $remainingProcessIds = @()
    $duringPmon = Get-NvidiaPmonSnapshot -Phase "during" -RunId $runId
    $duringPmon.RawLines |
        Out-File -LiteralPath (Join-Path $runDirectory "nvidia-pmon-during.txt") -Encoding utf8
    while (-not $process.HasExited) {
        if (([DateTime]::UtcNow - $startedUtc).TotalSeconds -ge $TimeoutSeconds) {
            $timedOut = $true
            $remainingProcessIds = @(
                Stop-BenchmarkProcessTree -Process $process)
            break
        }

        $telemetry += @(Get-NvidiaSnapshot -Phase "during" -RunId $runId)
        Start-Sleep -Milliseconds 500
        $process.Refresh()
    }

    if (-not $timedOut) {
        [void]$process.WaitForExit(10000)
        $process.Refresh()
    }
    $exitCode = if ($timedOut -or -not $process.HasExited) {
        $null
    }
    else {
        $process.ExitCode
    }
    $telemetry += @(Get-NvidiaSnapshot -Phase "after" -RunId $runId)
    $telemetry |
        Export-Csv -LiteralPath $telemetryPath -NoTypeInformation -Encoding UTF8
    (& nvidia-smi pmon -c 1 2>&1) | Out-File -LiteralPath (Join-Path $runDirectory "nvidia-pmon-after.txt") -Encoding utf8

    $telemetryCheck = $null
    $outputCheck = $null
    $runResult = $null
    try {
        $telemetryCheck = Test-Telemetry `
            -Records $telemetry `
            -GpuName $ExpectedGpu `
            -ExternalProcesses @($duringPmon.Processes) `
            -ChildProcessId $process.Id
        $outputsStable = Wait-ForStableRunOutputs -RunDirectory $runDirectory
        if ($outputsStable) {
            $outputCheck = Test-RunOutputs `
                -RunDirectory $runDirectory `
                -RunId $runId `
                -ExpectedScenario $Scenario `
                -ExpectedBuildKind $BuildKind `
                -ExpectedMeasurementRole $MeasurementRole `
                -ExpectedMeasurementSetId $MeasurementSetId `
                -ExpectedSourceCommit ([string]$sidecar.sourceCommit) `
                -ExpectedSourceTreeClean ([bool]$sidecar.sourceTreeClean) `
                -ExpectedSourceTreeCleanAvailable $true `
                -ExpectedBuildArtifactId ([string]$sidecar.buildArtifactId) `
                -ExpectedContentFingerprint ([string]$sidecar.contentFingerprint) `
                -ExpectedConfigurationFingerprint $configurationFingerprint `
                -ExpectedHardwareFingerprint $hardwareFingerprint `
                -ExpectedRepositoryReproducible ([bool]$sidecar.repositoryReproducible) `
                -ExpectedContentTrackingStatus ([string]$sidecar.contentTrackingStatus) `
                -ExpectedComparisonScope ([string]$buildProvenance.ComparisonScope) `
                -ExpectedQuality $Quality `
                -ExpectedAntialiasing $Antialiasing `
                -ExpectedRenderScalePercent $RenderScalePercent `
                -ExpectedGpuName $ExpectedGpu `
                -ExpectedWidth $Width `
                -ExpectedHeight $Height `
                -ScreenshotExpected ([bool]$CaptureScreenshot)
        }
        else {
            $outputCheck = New-OutputValidationResult `
                -Valid $false `
                -Reason "runtime outputs did not become complete and stable after process exit"
        }

        $normalExit = -not $timedOut -and
            $remainingProcessIds.Count -eq 0 -and
            $process.HasExited -and
            $exitCode -eq 0
        $isValid = $normalExit -and $outputCheck.Valid -and $telemetryCheck.Valid
        $reasonParts = @()
        if ($timedOut) { $reasonParts += "timeout after $TimeoutSeconds seconds" }
        if ($remainingProcessIds.Count -gt 0) {
            $reasonParts += "timed-out child process tree still active: $($remainingProcessIds -join ', ')"
        }
        elseif ($exitCode -ne 0) { $reasonParts += "child exit code $exitCode" }
        if (-not $outputCheck.Valid) { $reasonParts += $outputCheck.Reason }
        if (-not $telemetryCheck.Valid) { $reasonParts += $telemetryCheck.Reason }
        if ($outputCheck.Valid -and
            ($outputCheck.PerformanceBudgetStatus -eq "FAIL" -or
             $outputCheck.GcBudgetStatus -eq "FAIL")) {
            $reasonParts += $outputCheck.Reason
        }
        $reason = if ($reasonParts.Count -eq 0) {
            "all offline validation gates passed"
        }
        else {
            $reasonParts -join "; "
        }

        $manifestPath = $outputCheck.ManifestPath
        if ([string]::IsNullOrWhiteSpace($manifestPath)) {
            $manifestPath = Join-Path $runDirectory "offline.invalid.manifest.json"
        }
        Set-ManifestResult `
            -ManifestPath $manifestPath `
            -RunId $runId `
            -Status $(if ($isValid) { "valid" } else { "invalid" }) `
            -Reason $reason `
            -ExitCode $exitCode `
            -OutputsVerified $outputCheck.Valid `
            -TelemetryValid $telemetryCheck.Valid `
            -EvidenceValidity $(if ($isValid) { "VALID" } else { "INVALID" }) `
            -PerformanceBudgetStatus $outputCheck.PerformanceBudgetStatus `
            -GcBudgetStatus $outputCheck.GcBudgetStatus `
            -AggregateProductGate "INCOMPLETE" `
            -PairingEligible ($isValid -and [bool]$sidecar.sourceTreeClean)

        $runResult = [pscustomobject]@{
            runId = $runId
            status = if ($isValid) { "valid" } else { "invalid" }
            exitCode = $exitCode
            reason = $reason
            scenario = $Scenario
            quality = $Quality
            antialiasing = $Antialiasing
            renderScalePercent = $RenderScalePercent
            buildKind = $BuildKind
            measurementRole = $MeasurementRole
            measurementEligible = -not [bool]$CaptureScreenshot
            evidenceValidity = if ($isValid) { "VALID" } else { "INVALID" }
            performanceBudgetStatus = $outputCheck.PerformanceBudgetStatus
            performanceBudgetFailure = $outputCheck.PerformanceBudgetFailure
            gcBudgetStatus = $outputCheck.GcBudgetStatus
            gcBudgetFailure = $outputCheck.GcBudgetFailure
            aggregateProductGate = "INCOMPLETE"
            pairingEligible = $isValid -and [bool]$sidecar.sourceTreeClean
            measurementSetId = $MeasurementSetId
            sourceCommit = [string]$sidecar.sourceCommit
            sourceTreeClean = [bool]$sidecar.sourceTreeClean
            buildArtifactId = [string]$sidecar.buildArtifactId
            contentFingerprint = [string]$sidecar.contentFingerprint
            configurationFingerprint = $configurationFingerprint
            hardwareFingerprint = $hardwareFingerprint
            repositoryReproducible = [bool]$sidecar.repositoryReproducible
            contentTrackingStatus = [string]$sidecar.contentTrackingStatus
            comparisonScope = [string]$buildProvenance.ComparisonScope
            productBudgetStatus = $outputCheck.ProductBudgetStatus
            productBudgetPassed = $outputCheck.ProductBudgetPassed
            productBudgetFailure = $outputCheck.ProductBudgetFailure
            report = if ($outputCheck.Valid) { $outputCheck.Report.phase } else { "" }
        }
    }
    catch {
        $runResult = New-ValidatorExceptionRunResult `
            -RunDirectory $runDirectory `
            -RunId $runId `
            -Exception $_.Exception `
            -ExitCode $exitCode `
            -Scenario $Scenario `
            -Quality $Quality `
            -Antialiasing $Antialiasing `
            -RenderScalePercent $RenderScalePercent `
            -BuildKind $BuildKind `
            -MeasurementRole $MeasurementRole `
            -MeasurementEligible (-not [bool]$CaptureScreenshot)
    }

    $results += $runResult
}

if ($DryRun) {
    Write-Host "Dry-run completed. No benchmark process was started."
    return
}

Write-ResultTables -Results $results -Directory $OutputDirectory
$invalidCount = @($results | Where-Object { $_.status -ne "valid" }).Count
if ($invalidCount -gt 0) {
    throw "$invalidCount benchmark run(s) were invalid. See $OutputDirectory\invalid-runs.csv"
}

Write-Host "All benchmark runs passed offline validation: $OutputDirectory"
