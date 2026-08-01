using System;
using Unity.Profiling;
using UnityEngine;

namespace SonsOfTheForest.Infrastructure.Benchmark
{
    /// <summary>
    /// Player-safe performance counters for standalone benchmark builds.
    /// Unsupported counters stay explicitly unavailable instead of being
    /// reported as zero, which would make diagnosis ambiguous.
    /// </summary>
    public sealed class RuntimePerformanceSampler : IDisposable
    {
        public struct GlobalGcMeasurement
        {
            public bool allocationAvailable;
            public double allocatedAverageBytes;
            public long allocatedPeakBytes;
            public double allocationCountAverage;
        }

        /// <summary>
        /// Accumulates only samples explicitly submitted while the authoritative
        /// steady-state window is open. Construction, warm-up and report work are
        /// therefore outside the GC gate by lifecycle rather than by tolerance.
        /// </summary>
        public sealed class SteadyStateGcWindow
        {
            private bool _active;
            private bool _completed;
            private long _allocatedSampleCount;
            private long _allocationCountSampleCount;
            private double _allocatedTotal;
            private double _allocationCountTotal;
            private long _allocatedPeak;

            public void Begin()
            {
                if (_active)
                {
                    throw new InvalidOperationException(
                        "The steady-state GC measurement window is already active.");
                }

                _active = true;
                _completed = false;
                _allocatedSampleCount = 0L;
                _allocationCountSampleCount = 0L;
                _allocatedTotal = 0d;
                _allocationCountTotal = 0d;
                _allocatedPeak = 0L;
            }

            public void RecordFrame(
                bool allocatedBytesAvailable,
                long allocatedBytes,
                bool allocationCountAvailable,
                long allocationCount)
            {
                if (!_active)
                {
                    throw new InvalidOperationException(
                        "GC samples are accepted only inside the steady-state window.");
                }

                if (allocatedBytesAvailable)
                {
                    _allocatedSampleCount++;
                    _allocatedTotal += allocatedBytes;
                    _allocatedPeak = Math.Max(_allocatedPeak, allocatedBytes);
                }

                if (allocationCountAvailable)
                {
                    _allocationCountSampleCount++;
                    _allocationCountTotal += allocationCount;
                }
            }

            public void End()
            {
                if (!_active)
                {
                    throw new InvalidOperationException(
                        "The steady-state GC measurement window is not active.");
                }

                _active = false;
                _completed = true;
            }

            public GlobalGcMeasurement GetCompletedMeasurement()
            {
                if (_active || !_completed)
                {
                    throw new InvalidOperationException(
                        "GC results are unavailable until the steady-state window has ended.");
                }

                return new GlobalGcMeasurement
                {
                    allocationAvailable = _allocatedSampleCount > 0L,
                    allocatedAverageBytes = _allocatedSampleCount > 0L
                        ? _allocatedTotal / _allocatedSampleCount
                        : 0d,
                    allocatedPeakBytes = _allocatedPeak,
                    allocationCountAverage = _allocationCountSampleCount > 0L
                        ? _allocationCountTotal / _allocationCountSampleCount
                        : 0d,
                };
            }
        }

        public struct FrameSample
        {
            public bool hasFrameTiming;
            public bool hasCpuTotalTime;
            public bool hasCpuMainThreadTime;
            public bool hasCpuMainThreadPresentWaitTime;
            public bool hasCpuRenderThreadTime;
            public bool hasGpuTime;
            public double cpuTotalMilliseconds;
            public double cpuMainThreadMilliseconds;
            public double cpuMainThreadPresentWaitMilliseconds;
            public double cpuRenderThreadMilliseconds;
            public double gpuMilliseconds;
            public bool hasDrawCalls;
            public bool hasBatches;
            public bool hasSetPassCalls;
            public bool hasTriangles;
            public bool hasVertices;
            public long drawCalls;
            public long batches;
            public long setPassCalls;
            public long triangles;
            public long vertices;
            public bool hasGcAllocatedBytes;
            public bool hasGcAllocationCount;
            public long gcAllocatedBytes;
            public long gcAllocationCount;
            public bool hasTotalUsedMemory;
            public bool hasGfxUsedMemory;
            public bool hasTextureMemory;
            public long totalUsedMemoryBytes;
            public long gfxUsedMemoryBytes;
            public long textureMemoryBytes;
        }

        private const double NanosecondsToMilliseconds = 1d / 1_000_000d;

        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        private ProfilerRecorder _cpuTotalTime;
        private ProfilerRecorder _cpuMainThreadTime;
        private ProfilerRecorder _cpuRenderThreadTime;
        private ProfilerRecorder _gpuTime;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _setPassCalls;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;
        private ProfilerRecorder _gcAllocatedBytes;
        private ProfilerRecorder _gcAllocationCount;
        private ProfilerRecorder _totalUsedMemory;
        private ProfilerRecorder _gfxUsedMemory;
        private ProfilerRecorder _textureMemory;
        private bool _globalGcMeasurementActive;

        public RuntimePerformanceSampler()
        {
            _cpuTotalTime = Start(ProfilerCategory.Render, "CPU Total Frame Time");
            _cpuMainThreadTime = Start(ProfilerCategory.Render, "CPU Main Thread Frame Time");
            _cpuRenderThreadTime = Start(ProfilerCategory.Render, "CPU Render Thread Frame Time");
            _gpuTime = Start(ProfilerCategory.Render, "GPU Frame Time");
            _drawCalls = Start(ProfilerCategory.Render, "Draw Calls Count");
            _batches = Start(ProfilerCategory.Render, "Batches Count");
            _setPassCalls = Start(ProfilerCategory.Render, "SetPass Calls Count");
            _triangles = Start(ProfilerCategory.Render, "Triangles Count");
            _vertices = Start(ProfilerCategory.Render, "Vertices Count");
            _gcAllocatedBytes = Start(ProfilerCategory.Memory, "GC Allocated In Frame");
            _gcAllocationCount = Start(ProfilerCategory.Memory, "GC Allocation In Frame Count");
            _totalUsedMemory = Start(ProfilerCategory.Memory, "Total Used Memory");
            _gfxUsedMemory = Start(ProfilerCategory.Memory, "Gfx Used Memory");
            _textureMemory = Start(ProfilerCategory.Memory, "Texture Memory");
            Stop(ref _gcAllocatedBytes);
            Stop(ref _gcAllocationCount);
        }

        public bool FrameTimingFeatureEnabled => FrameTimingManager.IsFeatureEnabled();

        public void CaptureFrameTiming()
        {
            FrameTimingManager.CaptureFrameTimings();
        }

        public void BeginGlobalGcMeasurement()
        {
            if (_globalGcMeasurementActive)
            {
                throw new InvalidOperationException(
                    "The authoritative global GC recorder is already running.");
            }

            ResetAndStart(ref _gcAllocatedBytes);
            ResetAndStart(ref _gcAllocationCount);
            _globalGcMeasurementActive = true;
        }

        public void EndGlobalGcMeasurement()
        {
            if (!_globalGcMeasurementActive)
            {
                throw new InvalidOperationException(
                    "The authoritative global GC recorder is not running.");
            }

            Stop(ref _gcAllocatedBytes);
            Stop(ref _gcAllocationCount);
            _globalGcMeasurementActive = false;
        }

        public FrameSample ReadLastFrame()
        {
            var sample = new FrameSample();
            uint frameTimingCount = FrameTimingManager.GetLatestTimings(1u, _frameTimings);
            if (frameTimingCount > 0u)
            {
                FrameTiming timing = _frameTimings[0];
                sample.hasFrameTiming = true;
                sample.hasCpuTotalTime = timing.cpuFrameTime > 0d;
                sample.hasCpuMainThreadTime = timing.cpuMainThreadFrameTime > 0d;
                sample.hasCpuMainThreadPresentWaitTime = timing.cpuMainThreadPresentWaitTime > 0d;
                sample.hasCpuRenderThreadTime = timing.cpuRenderThreadFrameTime > 0d;
                sample.hasGpuTime = timing.gpuFrameTime > 0d;
                sample.cpuTotalMilliseconds = timing.cpuFrameTime;
                sample.cpuMainThreadMilliseconds = timing.cpuMainThreadFrameTime;
                sample.cpuMainThreadPresentWaitMilliseconds = timing.cpuMainThreadPresentWaitTime;
                sample.cpuRenderThreadMilliseconds = timing.cpuRenderThreadFrameTime;
                sample.gpuMilliseconds = timing.gpuFrameTime;
            }

            FillTimingFallback(ref sample.hasCpuTotalTime, ref sample.cpuTotalMilliseconds, _cpuTotalTime);
            FillTimingFallback(
                ref sample.hasCpuMainThreadTime,
                ref sample.cpuMainThreadMilliseconds,
                _cpuMainThreadTime);
            FillTimingFallback(
                ref sample.hasCpuRenderThreadTime,
                ref sample.cpuRenderThreadMilliseconds,
                _cpuRenderThreadTime);
            FillTimingFallback(ref sample.hasGpuTime, ref sample.gpuMilliseconds, _gpuTime);

            sample.hasDrawCalls = TryRead(_drawCalls, out sample.drawCalls);
            sample.hasBatches = TryRead(_batches, out sample.batches);
            sample.hasSetPassCalls = TryRead(_setPassCalls, out sample.setPassCalls);
            sample.hasTriangles = TryRead(_triangles, out sample.triangles);
            sample.hasVertices = TryRead(_vertices, out sample.vertices);
            sample.hasGcAllocatedBytes = _globalGcMeasurementActive &&
                                         TryReadRecorded(
                                             _gcAllocatedBytes,
                                             out sample.gcAllocatedBytes);
            sample.hasGcAllocationCount = _globalGcMeasurementActive &&
                                          TryReadRecorded(
                                              _gcAllocationCount,
                                              out sample.gcAllocationCount);
            sample.hasTotalUsedMemory = TryRead(_totalUsedMemory, out sample.totalUsedMemoryBytes);
            sample.hasGfxUsedMemory = TryRead(_gfxUsedMemory, out sample.gfxUsedMemoryBytes);
            sample.hasTextureMemory = TryRead(_textureMemory, out sample.textureMemoryBytes);
            return sample;
        }

        public void Dispose()
        {
            if (_globalGcMeasurementActive)
            {
                Stop(ref _gcAllocatedBytes);
                Stop(ref _gcAllocationCount);
                _globalGcMeasurementActive = false;
            }

            Dispose(ref _cpuTotalTime);
            Dispose(ref _cpuMainThreadTime);
            Dispose(ref _cpuRenderThreadTime);
            Dispose(ref _gpuTime);
            Dispose(ref _drawCalls);
            Dispose(ref _batches);
            Dispose(ref _setPassCalls);
            Dispose(ref _triangles);
            Dispose(ref _vertices);
            Dispose(ref _gcAllocatedBytes);
            Dispose(ref _gcAllocationCount);
            Dispose(ref _totalUsedMemory);
            Dispose(ref _gfxUsedMemory);
            Dispose(ref _textureMemory);
        }

        private static ProfilerRecorder Start(ProfilerCategory category, string counterName)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, counterName, 1);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private static void FillTimingFallback(
            ref bool available,
            ref double milliseconds,
            ProfilerRecorder recorder)
        {
            if (available || !recorder.Valid)
            {
                return;
            }

            long nanoseconds = recorder.LastValue;
            if (nanoseconds > 0L)
            {
                available = true;
                milliseconds = nanoseconds * NanosecondsToMilliseconds;
            }
        }

        private static bool TryRead(ProfilerRecorder recorder, out long value)
        {
            if (recorder.Valid)
            {
                value = recorder.LastValue;
                return true;
            }

            value = 0L;
            return false;
        }

        private static bool TryReadRecorded(ProfilerRecorder recorder, out long value)
        {
            if (recorder.Valid && recorder.Count > 0)
            {
                value = recorder.LastValue;
                return true;
            }

            value = 0L;
            return false;
        }

        private static void ResetAndStart(ref ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
            {
                return;
            }

            recorder.Reset();
            recorder.Start();
        }

        private static void Stop(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid && recorder.IsRunning)
            {
                recorder.Stop();
            }
        }

        private static void Dispose(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }
    }
}
