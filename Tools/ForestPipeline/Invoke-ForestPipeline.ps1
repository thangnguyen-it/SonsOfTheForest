[CmdletBinding()]
param(
    [ValidateSet('Audit','Build','Validate','Turntable')]
    [string]$Mode = 'Audit',
    [string]$BlenderPath = 'E:\knee_project\Tools\Blender\4.5.11\blender-4.5.11-windows-x64\blender.exe',
    [string]$ManifestPath = 'Tools/ForestPipeline/forest-harvest-manifest.json'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$manifest = (Resolve-Path (Join-Path $root $ManifestPath)).Path
if (-not (Test-Path -LiteralPath $BlenderPath -PathType Leaf)) {
    throw "Blender executable not found: $BlenderPath"
}

$reportRoot = Join-Path $root 'Artifacts/ForestPipeline'
New-Item -ItemType Directory -Force -Path $reportRoot | Out-Null
$scriptName = switch ($Mode) {
    'Audit' { 'audit_sources.py' }
    'Build' { 'build_felling_kits.py' }
    'Validate' { 'validate_outputs.py' }
    'Turntable' { 'render_turntable.py' }
}
$script = Join-Path $PSScriptRoot $scriptName
$output = Join-Path $reportRoot ("{0}.json" -f $Mode.ToLowerInvariant())

$arguments = @('--background','--factory-startup','--python',$script,'--',
    '--project-root',$root,'--manifest',$manifest,'--output',$output)
& $BlenderPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Forest pipeline $Mode failed with Blender exit code $LASTEXITCODE."
}
Write-Output $output
