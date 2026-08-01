using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering;

namespace SonsOfTheForest.Infrastructure.Editor.ForestModelIntake
{
    /// <summary>
    /// Produces deterministic, project-relative provenance for the two R2-PERF1
    /// player artifacts. Runtime/run provenance and pairing are intentionally
    /// outside this B5B1 contract.
    /// </summary>
    public static class R2Perf1BuildProvenance
    {
        public const string SchemaVersion = "r2-perf1-build-provenance/1";
        public const string SidecarFileName = "r2-perf1.build-provenance.json";
        public const string SidecarTemporaryFileName = SidecarFileName + ".tmp";
        public const string ReleaseMeasurementRole = "release_performance";
        public const string DiagnosticMeasurementRole = "development_gc";
        public const string ReleaseBuildKind = "release";
        public const string DiagnosticBuildKind = "diagnostic";
        public const string LocalIgnoredContent = "local_ignored_content";
        public const string TrackedContent = "tracked_content";
        public const string UntrackedContent = "untracked_content";
        public const string ProvenanceValid = "valid";
        public const string ProvenanceInvalid = "invalid";

        private const string ZeroHash128 = "00000000000000000000000000000000";
        private const int GitTimeoutMilliseconds = 15000;

        [Serializable]
        public sealed class BuildProvenanceSidecar
        {
            public string schemaVersion;
            public string generatedUtc;
            public string provenanceStatus;
            public string statusReason;
            public string measurementRole;
            public string buildKind;
            public string sourceCommit;
            public bool sourceTreeClean;
            public string unityVersion;
            public string platform;
            public string architecture;
            public string buildOptions;
            public bool developmentBuild;
            public bool autoRunPlayer;
            public bool autoConnectProfiler;
            public bool deepProfiling;
            public bool frameTimingStatsEnabled;
            public string[] graphicsApis;
            public string benchmarkScenePath;
            public string benchmarkSceneGuid;
            public bool benchmarkContentTracked;
            public bool repositoryReproducible;
            public string contentTrackingStatus;
            public int dependencyCount;
            public string contentFingerprint;
            public string buildConfigurationFingerprint;
            public string buildArtifactId;
            public int artifactFileCount;
        }

        [Serializable]
        public sealed class ContentDependencyRecord
        {
            public string assetPath;
            public string assetGuid;
            public string rawAssetSha256;
            public string metaSha256;
            public string assetDependencyHash;
            public string dependencyKind;
            public string trackingStatus;
            public string packageIdentity;
            public string virtualIdentity;
        }

        public sealed class ContentFingerprintResult
        {
            public ContentDependencyRecord[] dependencies;
            public string canonicalData;
            public string contentFingerprint;
            public bool benchmarkContentTracked;
            public bool repositoryReproducible;
            public string contentTrackingStatus;
        }

        public sealed class SourceProvenance
        {
            public string sourceCommit;
            public bool sourceTreeClean;
        }

        public sealed class ArtifactIdentity
        {
            public string canonicalManifest;
            public string buildArtifactId;
            public int artifactFileCount;
        }

        public sealed class PreBuildContext
        {
            public SourceProvenance source;
            public ContentFingerprintResult content;
            public string measurementRole;
            public string buildKind;
            public string unityVersion;
            public string platform;
            public string architecture;
            public bool frameTimingStatsEnabled;
            public string[] graphicsApis;
            public string benchmarkScenePath;
            public string benchmarkSceneGuid;
        }

        public sealed class BuildProvenanceException : InvalidOperationException
        {
            public BuildProvenanceException(string code, string message, Exception inner = null)
                : base("[R2-PERF1 provenance:" + code + "] " + message, inner)
            {
                Code = code;
            }

            public string Code { get; }
        }

        private sealed class RepositoryInventory
        {
            public string root;
            public string sourceCommit;
            public bool sourceTreeClean;
            public HashSet<string> trackedPaths;
            public HashSet<string> ignoredPaths;
        }

        private sealed class GitResult
        {
            public int exitCode;
            public string standardOutput;
            public string standardError;
        }

        public static PreBuildContext CapturePreBuildContext(
            string benchmarkScenePath,
            string measurementRole,
            string buildKind,
            BuildTarget target,
            string architecture,
            GraphicsDeviceType[] graphicsApis)
        {
            ValidateRoleAndKind(measurementRole, buildKind);
            bool frameTimingStatsEnabled = PlayerSettings.enableFrameTimingStats;
            ValidateFrameTimingStatsEnabled(frameTimingStatsEnabled);
            string projectRoot = ProjectRoot();
            RepositoryInventory repository = CaptureRepositoryInventory(projectRoot);
            if (!repository.sourceTreeClean)
            {
                throw new BuildProvenanceException(
                    "source_tree_dirty",
                    "Official measurement builds require a clean staged, tracked, and untracked source tree.");
            }

            ContentFingerprintResult content = CaptureContentFingerprint(
                projectRoot,
                benchmarkScenePath,
                repository);
            return new PreBuildContext
            {
                source = new SourceProvenance
                {
                    sourceCommit = repository.sourceCommit,
                    sourceTreeClean = repository.sourceTreeClean,
                },
                content = content,
                measurementRole = measurementRole,
                buildKind = buildKind,
                unityVersion = UnityEngine.Application.unityVersion,
                platform = target.ToString(),
                architecture = RequireValue(architecture, "architecture"),
                frameTimingStatsEnabled = frameTimingStatsEnabled,
                graphicsApis = (graphicsApis ?? Array.Empty<GraphicsDeviceType>())
                    .Select(value => value.ToString())
                    .ToArray(),
                benchmarkScenePath = NormalizeAssetPath(benchmarkScenePath),
                benchmarkSceneGuid = RequireValue(
                    AssetDatabase.AssetPathToGUID(benchmarkScenePath),
                    "benchmark scene GUID"),
            };
        }

        public static BuildProvenanceSidecar CreateCompletedSidecar(
            PreBuildContext context,
            BuildOptions actualOptions,
            string actualPlatform,
            string artifactDirectory,
            string generatedUtc)
        {
            if (context == null || context.source == null || context.content == null)
            {
                throw new BuildProvenanceException("context_missing", "Pre-build provenance context is incomplete.");
            }

            ValidateFrameTimingStatsEnabled(context.frameTimingStatsEnabled);
            ArtifactIdentity artifact = ComputeArtifactIdentity(artifactDirectory);
            string status = ProvenanceValid;
            string reason = string.Empty;
            try
            {
                ValidateBuildRoleContract(context.measurementRole, context.buildKind, actualOptions);
            }
            catch (BuildProvenanceException exception)
            {
                status = ProvenanceInvalid;
                reason = exception.Message;
            }

            bool development = HasOption(actualOptions, BuildOptions.Development);
            bool autoRun = HasOption(actualOptions, BuildOptions.AutoRunPlayer);
            bool connectProfiler = HasOption(actualOptions, BuildOptions.ConnectWithProfiler);
            bool deepProfiling = HasOption(actualOptions, BuildOptions.EnableDeepProfilingSupport);
            string buildConfigurationFingerprint = ComputeBuildConfigurationFingerprint(
                context.unityVersion,
                actualPlatform,
                context.architecture,
                actualOptions,
                context.graphicsApis,
                context.benchmarkScenePath,
                context.benchmarkSceneGuid,
                context.measurementRole,
                context.buildKind,
                development,
                autoRun,
                connectProfiler,
                deepProfiling,
                context.frameTimingStatsEnabled);

            return new BuildProvenanceSidecar
            {
                schemaVersion = SchemaVersion,
                generatedUtc = RequireValue(generatedUtc, "generated UTC"),
                provenanceStatus = status,
                statusReason = reason,
                measurementRole = context.measurementRole,
                buildKind = context.buildKind,
                sourceCommit = context.source.sourceCommit,
                sourceTreeClean = context.source.sourceTreeClean,
                unityVersion = context.unityVersion,
                platform = RequireValue(actualPlatform, "actual platform"),
                architecture = context.architecture,
                buildOptions = CanonicalBuildOptions(actualOptions),
                developmentBuild = development,
                autoRunPlayer = autoRun,
                autoConnectProfiler = connectProfiler,
                deepProfiling = deepProfiling,
                frameTimingStatsEnabled = context.frameTimingStatsEnabled,
                graphicsApis = context.graphicsApis ?? Array.Empty<string>(),
                benchmarkScenePath = context.benchmarkScenePath,
                benchmarkSceneGuid = context.benchmarkSceneGuid,
                benchmarkContentTracked = context.content.benchmarkContentTracked,
                repositoryReproducible = context.content.repositoryReproducible,
                contentTrackingStatus = context.content.contentTrackingStatus,
                dependencyCount = context.content.dependencies.Length,
                contentFingerprint = context.content.contentFingerprint,
                buildConfigurationFingerprint = buildConfigurationFingerprint,
                buildArtifactId = artifact.buildArtifactId,
                artifactFileCount = artifact.artifactFileCount,
            };
        }

        public static BuildProvenanceSidecar WriteCompletedBuildProvenance(
            PreBuildContext context,
            BuildReport report,
            string artifactDirectory)
        {
            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildProvenanceException(
                    "build_not_successful",
                    "A successful BuildReport is required before writing build provenance.");
            }

            BuildProvenanceSidecar sidecar = CreateCompletedSidecar(
                context,
                report.summary.options,
                report.summary.platform.ToString(),
                artifactDirectory,
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            WriteSidecarAtomic(GetSidecarPath(artifactDirectory), sidecar);
            if (!string.Equals(sidecar.provenanceStatus, ProvenanceValid, StringComparison.Ordinal))
            {
                throw new BuildProvenanceException("actual_flags_invalid", sidecar.statusReason);
            }

            return sidecar;
        }

        public static void ValidateBuildRoleContract(
            string measurementRole,
            string buildKind,
            BuildOptions actualOptions)
        {
            ValidateRoleAndKind(measurementRole, buildKind);
            BuildOptions forbidden = BuildOptions.AutoRunPlayer |
                                     BuildOptions.ConnectWithProfiler |
                                     BuildOptions.EnableDeepProfilingSupport;
            if ((actualOptions & forbidden) != 0)
            {
                throw new BuildProvenanceException(
                    "forbidden_build_flag",
                    "AutoRun, ConnectWithProfiler, and DeepProfiling are forbidden for provenance builds.");
            }

            BuildOptions expected = string.Equals(
                measurementRole,
                ReleaseMeasurementRole,
                StringComparison.Ordinal)
                ? BuildOptions.None
                : BuildOptions.Development;
            if (actualOptions != expected)
            {
                throw new BuildProvenanceException(
                    "build_flags_mismatch",
                    "Actual BuildReport options '" + CanonicalBuildOptions(actualOptions) +
                    "' do not exactly match required options '" + CanonicalBuildOptions(expected) +
                    "' for " + measurementRole + ".");
            }

        }

        public static void ValidateFrameTimingStatsEnabled(bool enabled)
        {
            if (!enabled)
            {
                throw new BuildProvenanceException(
                    "frame_timing_disabled",
                    "Official R2-PERF1 builds require PlayerSettings.enableFrameTimingStats=true before provenance capture.");
            }
        }

        public static ContentDependencyRecord CreateFileDependencyRecord(
            string normalizedAssetPath,
            string physicalAssetPath,
            string assetGuid,
            string assetDependencyHash,
            string dependencyKind,
            string trackingStatus,
            string packageIdentity)
        {
            string path = NormalizeAssetPath(normalizedAssetPath);
            if (string.IsNullOrWhiteSpace(physicalAssetPath) || !File.Exists(physicalAssetPath))
            {
                throw new BuildProvenanceException(
                    "dependency_unhashable",
                    "Required dependency cannot be read: " + path);
            }

            string metaPath = physicalAssetPath + ".meta";
            return new ContentDependencyRecord
            {
                assetPath = path,
                assetGuid = assetGuid ?? string.Empty,
                rawAssetSha256 = ComputeFileSha256(physicalAssetPath),
                metaSha256 = File.Exists(metaPath) ? ComputeFileSha256(metaPath) : string.Empty,
                assetDependencyHash = RequireValue(assetDependencyHash, "asset dependency hash"),
                dependencyKind = RequireValue(dependencyKind, "dependency kind"),
                trackingStatus = RequireValue(trackingStatus, "tracking status"),
                packageIdentity = packageIdentity ?? string.Empty,
                virtualIdentity = string.Empty,
            };
        }

        public static string ComputeContentFingerprint(ContentDependencyRecord[] dependencies)
        {
            return ComputeSha256(Encoding.UTF8.GetBytes(CanonicalizeContentDependencies(dependencies)));
        }

        public static string CanonicalizeContentDependencies(ContentDependencyRecord[] dependencies)
        {
            ContentDependencyRecord[] ordered = (dependencies ?? Array.Empty<ContentDependencyRecord>())
                .OrderBy(value => value?.assetPath, StringComparer.Ordinal)
                .ToArray();
            if (ordered.Length == 0)
            {
                throw new BuildProvenanceException("dependency_closure_empty", "Benchmark dependency closure is empty.");
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var builder = new StringBuilder();
            foreach (ContentDependencyRecord dependency in ordered)
            {
                ValidateDependencyRecord(dependency);
                if (!seen.Add(dependency.assetPath))
                {
                    throw new BuildProvenanceException(
                        "dependency_duplicate",
                        "Duplicate dependency path: " + dependency.assetPath);
                }

                AppendCanonical(builder, "path", dependency.assetPath);
                AppendCanonical(builder, "guid", dependency.assetGuid);
                AppendCanonical(builder, "raw", dependency.rawAssetSha256);
                AppendCanonical(builder, "meta", dependency.metaSha256);
                AppendCanonical(builder, "import", dependency.assetDependencyHash);
                AppendCanonical(builder, "kind", dependency.dependencyKind);
                AppendCanonical(builder, "tracking", dependency.trackingStatus);
                AppendCanonical(builder, "package", dependency.packageIdentity);
                AppendCanonical(builder, "virtual", dependency.virtualIdentity);
                builder.Append('\n');
            }

            return builder.ToString();
        }

        public static string ComputeBuildConfigurationFingerprint(
            string unityVersion,
            string platform,
            string architecture,
            BuildOptions buildOptions,
            string[] graphicsApis,
            string scenePath,
            string sceneGuid,
            string measurementRole,
            string buildKind,
            bool developmentBuild,
            bool autoRunPlayer,
            bool autoConnectProfiler,
            bool deepProfiling,
            bool frameTimingStatsEnabled)
        {
            ValidateRoleAndKind(measurementRole, buildKind);
            var builder = new StringBuilder();
            AppendCanonical(builder, "unity", RequireValue(unityVersion, "Unity version"));
            AppendCanonical(builder, "platform", RequireValue(platform, "platform"));
            AppendCanonical(builder, "architecture", RequireValue(architecture, "architecture"));
            AppendCanonical(
                builder,
                "options",
                ((int)buildOptions).ToString(CultureInfo.InvariantCulture));
            foreach (string graphicsApi in graphicsApis ?? Array.Empty<string>())
            {
                AppendCanonical(builder, "graphicsApi", RequireValue(graphicsApi, "graphics API"));
            }

            AppendCanonical(builder, "scene", NormalizeAssetPath(scenePath));
            AppendCanonical(builder, "sceneGuid", RequireValue(sceneGuid, "scene GUID"));
            AppendCanonical(builder, "role", measurementRole);
            AppendCanonical(builder, "kind", buildKind);
            AppendCanonical(builder, "development", developmentBuild ? "true" : "false");
            AppendCanonical(builder, "autoRun", autoRunPlayer ? "true" : "false");
            AppendCanonical(builder, "connectProfiler", autoConnectProfiler ? "true" : "false");
            AppendCanonical(builder, "deepProfiling", deepProfiling ? "true" : "false");
            AppendCanonical(builder, "frameTimingStats", frameTimingStatsEnabled ? "true" : "false");
            return ComputeSha256(Encoding.UTF8.GetBytes(builder.ToString()));
        }

        public static ArtifactIdentity ComputeArtifactIdentity(string artifactDirectory)
        {
            string root = Path.GetFullPath(RequireValue(artifactDirectory, "artifact directory"));
            if (!Directory.Exists(root))
            {
                throw new BuildProvenanceException(
                    "artifact_directory_missing",
                    "Build artifact directory does not exist.");
            }

            string[] files = EnumerateArtifactFiles(root)
                .Where(path => !IsExcludedArtifactFile(path))
                .OrderBy(path => RelativePath(root, path), StringComparer.Ordinal)
                .ToArray();
            if (files.Length == 0)
            {
                throw new BuildProvenanceException("artifact_empty", "Build artifact tree contains no files.");
            }

            var builder = new StringBuilder();
            foreach (string file in files)
            {
                string relative = RelativePath(root, file);
                var info = new FileInfo(file);
                AppendCanonical(builder, "path", relative);
                AppendCanonical(
                    builder,
                    "length",
                    info.Length.ToString(CultureInfo.InvariantCulture));
                AppendCanonical(builder, "sha256", ComputeFileSha256(file));
                builder.Append('\n');
            }

            string canonical = builder.ToString();
            return new ArtifactIdentity
            {
                canonicalManifest = canonical,
                buildArtifactId = ComputeSha256(Encoding.UTF8.GetBytes(canonical)),
                artifactFileCount = files.Length,
            };
        }

        public static string GetSidecarPath(string artifactDirectory)
        {
            return Path.Combine(
                Path.GetFullPath(RequireValue(artifactDirectory, "artifact directory")),
                SidecarFileName);
        }

        public static void WriteSidecarAtomic(string sidecarPath, BuildProvenanceSidecar sidecar)
        {
            if (sidecar == null)
            {
                throw new ArgumentNullException(nameof(sidecar));
            }

            string fullPath = Path.GetFullPath(RequireValue(sidecarPath, "sidecar path"));
            if (!string.Equals(Path.GetFileName(fullPath), SidecarFileName, StringComparison.Ordinal))
            {
                throw new BuildProvenanceException(
                    "sidecar_name_invalid",
                    "Build provenance sidecar must use the fixed filename contract.");
            }

            string directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            string temporaryPath = Path.Combine(directory, SidecarTemporaryFileName);
            bool temporaryCreated = false;
            try
            {
                byte[] payload = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(sidecar, true));
                using (var stream = new FileStream(
                           temporaryPath,
                           FileMode.CreateNew,
                           FileAccess.Write,
                           FileShare.None))
                {
                    temporaryCreated = true;
                    stream.Write(payload, 0, payload.Length);
                    stream.Flush(true);
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(temporaryPath, fullPath, null);
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }

                temporaryCreated = false;
            }
            catch (Exception exception)
            {
                throw new BuildProvenanceException(
                    "sidecar_write_failed",
                    "Atomic build provenance sidecar write failed.",
                    exception);
            }
            finally
            {
                if (temporaryCreated && File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public static string SerializeSidecar(BuildProvenanceSidecar sidecar)
        {
            return JsonUtility.ToJson(sidecar, true);
        }

        public static string NormalizeAssetPath(string path)
        {
            string normalized = RequireValue(path, "asset path").Replace('\\', '/').Trim();
            if (Path.IsPathRooted(normalized) ||
                normalized.StartsWith("/", StringComparison.Ordinal) ||
                normalized.Split('/').Any(segment => segment == ".." || segment.Length == 0))
            {
                throw new BuildProvenanceException(
                    "asset_path_invalid",
                    "Asset paths must be normalized project/package-relative paths: " + normalized);
            }

            return normalized;
        }

        public static string ComputeFileSha256(string path)
        {
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                using (FileStream stream = new FileStream(
                           path,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.Read))
                {
                    return ToHex(sha256.ComputeHash(stream));
                }
            }
            catch (Exception exception)
            {
                throw new BuildProvenanceException(
                    "file_hash_failed",
                    "Could not hash required file: " + Path.GetFileName(path),
                    exception);
            }
        }

        public static void ValidateArtifactEntryForHash(
            string artifactRoot,
            string entryPath,
            FileAttributes attributes)
        {
            RelativePath(
                Path.GetFullPath(RequireValue(artifactRoot, "artifact root")),
                Path.GetFullPath(RequireValue(entryPath, "artifact entry")));
            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new BuildProvenanceException(
                    "artifact_reparse_point",
                    "Artifact identity rejects reparse points before reading file content.");
            }
        }

        private static ContentFingerprintResult CaptureContentFingerprint(
            string projectRoot,
            string scenePath,
            RepositoryInventory repository)
        {
            string normalizedScene = NormalizeAssetPath(scenePath);
            string[] dependencyPaths = AssetDatabase.GetDependencies(normalizedScene, true)
                .Concat(new[] { normalizedScene })
                .Select(NormalizeAssetPath)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var dependencies = dependencyPaths
                .Select(path => CaptureDependency(projectRoot, path, repository))
                .ToArray();

            bool sceneTracked = repository.trackedPaths.Contains(normalizedScene);
            bool sceneIgnored = repository.ignoredPaths.Contains(normalizedScene);
            string tracking = DetermineContentTrackingStatus(sceneTracked, sceneIgnored);
            bool reproducible = DetermineRepositoryReproducibility(sceneTracked, dependencies);
            string canonical = CanonicalizeContentDependencies(dependencies);
            return new ContentFingerprintResult
            {
                dependencies = dependencies,
                canonicalData = canonical,
                contentFingerprint = ComputeSha256(Encoding.UTF8.GetBytes(canonical)),
                benchmarkContentTracked = sceneTracked,
                repositoryReproducible = reproducible,
                contentTrackingStatus = tracking,
            };
        }

        private static ContentDependencyRecord CaptureDependency(
            string projectRoot,
            string assetPath,
            RepositoryInventory repository)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath) ?? string.Empty;
            string dependencyHash = AssetDatabase.GetAssetDependencyHash(assetPath).ToString();
            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal) || assetPath == "Assets")
            {
                string physicalPath = Path.Combine(
                    projectRoot,
                    assetPath.Replace('/', Path.DirectorySeparatorChar));
                string tracking = repository.trackedPaths.Contains(assetPath)
                    ? "tracked"
                    : repository.ignoredPaths.Contains(assetPath)
                        ? "ignored"
                        : "untracked";
                return CreateFileDependencyRecord(
                    assetPath,
                    physicalPath,
                    guid,
                    dependencyHash,
                    "project_asset",
                    tracking,
                    string.Empty);
            }

            if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                UnityEditor.PackageManager.PackageInfo package =
                    UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
                if (package == null || string.IsNullOrWhiteSpace(package.resolvedPath))
                {
                    throw new BuildProvenanceException(
                        "package_unresolved",
                        "Package dependency could not be resolved: " + assetPath);
                }

                string relative = assetPath.Substring(package.assetPath.Length).TrimStart('/');
                string physicalPath = Path.Combine(
                    package.resolvedPath,
                    relative.Replace('/', Path.DirectorySeparatorChar));
                return CreateFileDependencyRecord(
                    assetPath,
                    physicalPath,
                    guid,
                    dependencyHash,
                    "package_asset",
                    "package",
                    package.name + "@" + package.version);
            }

            if (string.IsNullOrWhiteSpace(guid) &&
                (string.IsNullOrWhiteSpace(dependencyHash) || dependencyHash == ZeroHash128))
            {
                throw new BuildProvenanceException(
                    "virtual_dependency_unstable",
                    "Virtual/built-in dependency lacks stable identity: " + assetPath);
            }

            return new ContentDependencyRecord
            {
                assetPath = assetPath,
                assetGuid = guid,
                rawAssetSha256 = string.Empty,
                metaSha256 = string.Empty,
                assetDependencyHash = dependencyHash,
                dependencyKind = "unity_virtual",
                trackingStatus = "unity_builtin",
                packageIdentity = string.Empty,
                virtualIdentity =
                    "unity:" + UnityEngine.Application.unityVersion + ":" + assetPath,
            };
        }

        public static string DetermineContentTrackingStatus(bool tracked, bool ignored)
        {
            return tracked ? TrackedContent : ignored ? LocalIgnoredContent : UntrackedContent;
        }

        public static bool DetermineRepositoryReproducibility(
            bool benchmarkContentTracked,
            ContentDependencyRecord[] dependencies)
        {
            return benchmarkContentTracked &&
                   (dependencies ?? Array.Empty<ContentDependencyRecord>())
                   .All(IsRepositoryReproducibleDependency);
        }

        private static bool IsRepositoryReproducibleDependency(ContentDependencyRecord dependency)
        {
            if (dependency.dependencyKind == "project_asset")
            {
                return dependency.trackingStatus == "tracked";
            }

            return dependency.dependencyKind == "package_asset" ||
                   dependency.dependencyKind == "unity_virtual";
        }

        private static RepositoryInventory CaptureRepositoryInventory(string projectRoot)
        {
            GitResult head = RunGit(projectRoot, "rev-parse HEAD");
            GitResult status = RunGit(projectRoot, "status --porcelain=v1 --untracked-files=all");
            GitResult tracked = RunGit(projectRoot, "ls-files -z");
            GitResult ignored = RunGit(projectRoot, "ls-files --others --ignored --exclude-standard -z");
            return new RepositoryInventory
            {
                root = projectRoot,
                sourceCommit = RequireValue(head.standardOutput.Trim(), "source commit"),
                sourceTreeClean = string.IsNullOrWhiteSpace(status.standardOutput),
                trackedPaths = SplitNullPaths(tracked.standardOutput),
                ignoredPaths = SplitNullPaths(ignored.standardOutput),
            };
        }

        private static GitResult RunGit(string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var process = new Process { StartInfo = startInfo })
            {
                if (!process.Start())
                {
                    throw new BuildProvenanceException("git_start_failed", "Could not start Git.");
                }

                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(GitTimeoutMilliseconds))
                {
                    Exception killFailure = KillProcessTree(process);
                    if (!process.WaitForExit(5000) && killFailure == null)
                    {
                        killFailure = new TimeoutException(
                            "The Git process did not exit after process-tree termination.");
                    }

                    throw new BuildProvenanceException(
                        "git_timeout",
                        killFailure == null
                            ? "Git provenance query timed out and its process tree was terminated."
                            : "Git provenance query timed out and process-tree termination failed.",
                        killFailure);
                }

                try
                {
                    if (!Task.WaitAll(new Task[] { outputTask, errorTask }, GitTimeoutMilliseconds))
                    {
                        throw new BuildProvenanceException(
                            "git_output_timeout",
                            "Git exited but redirected provenance output did not complete within the timeout.");
                    }

                    string output = outputTask.GetAwaiter().GetResult();
                    string error = errorTask.GetAwaiter().GetResult();
                    if (process.ExitCode != 0)
                    {
                        throw new BuildProvenanceException(
                            "git_failed",
                            "Git provenance query failed: " + error.Trim());
                    }

                    return new GitResult
                    {
                        exitCode = process.ExitCode,
                        standardOutput = output,
                        standardError = error,
                    };
                }
                catch (BuildProvenanceException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new BuildProvenanceException(
                        "git_output_failed",
                        "Git provenance output could not be collected.",
                        exception);
                }
            }
        }

        private static Exception KillProcessTree(Process process)
        {
            try
            {
                MethodInfo killTree = typeof(Process).GetMethod("Kill", new[] { typeof(bool) });
                if (killTree != null)
                {
                    killTree.Invoke(process, new object[] { true });
                    return null;
                }

                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    using (Process taskKill = Process.Start(new ProcessStartInfo
                           {
                               FileName = "taskkill.exe",
                               Arguments = "/PID " + process.Id.ToString(CultureInfo.InvariantCulture) + " /T /F",
                               UseShellExecute = false,
                               CreateNoWindow = true,
                           }))
                    {
                        if (taskKill == null || !taskKill.WaitForExit(5000) || taskKill.ExitCode != 0)
                        {
                            throw new InvalidOperationException("taskkill could not terminate the Git process tree.");
                        }
                    }

                    return null;
                }

                process.Kill();
                return null;
            }
            catch (Exception exception)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                    // Preserve the process-tree failure as the structured timeout cause.
                }

                return exception;
            }
        }

        private static string[] EnumerateArtifactFiles(string root)
        {
            var rootDirectory = new DirectoryInfo(root);
            if ((rootDirectory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new BuildProvenanceException(
                    "artifact_reparse_point",
                    "Artifact root must not be a reparse point.");
            }

            var files = new List<string>();
            var pending = new Stack<DirectoryInfo>();
            pending.Push(rootDirectory);
            while (pending.Count > 0)
            {
                DirectoryInfo directory = pending.Pop();
                foreach (FileInfo file in directory.GetFiles())
                {
                    ValidateArtifactEntryForHash(root, file.FullName, file.Attributes);
                    files.Add(file.FullName);
                }

                foreach (DirectoryInfo child in directory.GetDirectories())
                {
                    ValidateArtifactEntryForHash(root, child.FullName, child.Attributes);
                    pending.Push(child);
                }
            }

            return files.ToArray();
        }

        private static HashSet<string> SplitNullPaths(string value)
        {
            return new HashSet<string>(
                (value ?? string.Empty)
                    .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(path => path.Replace('\\', '/')),
                StringComparer.Ordinal);
        }

        private static void ValidateDependencyRecord(ContentDependencyRecord dependency)
        {
            if (dependency == null)
            {
                throw new BuildProvenanceException("dependency_null", "Dependency record is null.");
            }

            dependency.assetPath = NormalizeAssetPath(dependency.assetPath);
            dependency.assetGuid = dependency.assetGuid ?? string.Empty;
            dependency.rawAssetSha256 = dependency.rawAssetSha256 ?? string.Empty;
            dependency.metaSha256 = dependency.metaSha256 ?? string.Empty;
            dependency.assetDependencyHash = RequireValue(
                dependency.assetDependencyHash,
                "asset dependency hash");
            dependency.dependencyKind = RequireValue(dependency.dependencyKind, "dependency kind");
            dependency.trackingStatus = RequireValue(dependency.trackingStatus, "tracking status");
            dependency.packageIdentity = dependency.packageIdentity ?? string.Empty;
            dependency.virtualIdentity = dependency.virtualIdentity ?? string.Empty;
            if (dependency.dependencyKind != "unity_virtual" &&
                string.IsNullOrWhiteSpace(dependency.rawAssetSha256))
            {
                throw new BuildProvenanceException(
                    "dependency_raw_hash_missing",
                    "File dependency is missing its raw content hash: " + dependency.assetPath);
            }

            if (dependency.dependencyKind == "unity_virtual" &&
                string.IsNullOrWhiteSpace(dependency.virtualIdentity))
            {
                throw new BuildProvenanceException(
                    "virtual_identity_missing",
                    "Virtual dependency is missing a stable identity: " + dependency.assetPath);
            }
        }

        private static void ValidateRoleAndKind(string measurementRole, string buildKind)
        {
            bool release = string.Equals(
                measurementRole,
                ReleaseMeasurementRole,
                StringComparison.Ordinal) &&
                           string.Equals(buildKind, ReleaseBuildKind, StringComparison.Ordinal);
            bool diagnostic = string.Equals(
                measurementRole,
                DiagnosticMeasurementRole,
                StringComparison.Ordinal) &&
                              string.Equals(buildKind, DiagnosticBuildKind, StringComparison.Ordinal);
            if (!release && !diagnostic)
            {
                throw new BuildProvenanceException(
                    "role_build_mismatch",
                    "Measurement role and build kind must be the exact approved pair.");
            }
        }

        private static bool HasOption(BuildOptions value, BuildOptions option)
        {
            return (value & option) == option;
        }

        private static string CanonicalBuildOptions(BuildOptions options)
        {
            return options == BuildOptions.None ? "None" : options.ToString();
        }

        private static bool IsExcludedArtifactFile(string path)
        {
            string name = Path.GetFileName(path);
            return string.Equals(name, SidecarFileName, StringComparison.Ordinal) ||
                   string.Equals(name, SidecarTemporaryFileName, StringComparison.Ordinal);
        }

        private static string RelativePath(string root, string path)
        {
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(path);
            if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildProvenanceException(
                    "artifact_path_escape",
                    "Artifact file escapes the artifact directory.");
            }

            return fullPath.Substring(normalizedRoot.Length).Replace('\\', '/');
        }

        private static void AppendCanonical(StringBuilder builder, string name, string value)
        {
            string safe = value ?? string.Empty;
            builder.Append(name)
                .Append(':')
                .Append(safe.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(safe)
                .Append('|');
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return ToHex(sha256.ComputeHash(bytes));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            return string.Concat(bytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
        }

        private static string RequireValue(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BuildProvenanceException("required_value_missing", label + " is required.");
            }

            return value;
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        }
    }
}
