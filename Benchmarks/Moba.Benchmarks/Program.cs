// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using BenchmarkDotNet.Running;

// Select benchmarks with BenchmarkDotNet's own arguments, for example: --filter "*EventBus*"
BenchmarkSwitcher.FromAssembly(typeof(Moba.Benchmarks.EventBusBenchmarks).Assembly).Run(args);
