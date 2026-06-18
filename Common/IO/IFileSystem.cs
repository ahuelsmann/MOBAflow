// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Common.IO;

/// <summary>
/// Small file-system abstraction for code paths that should be unit-testable without real files.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
}

/// <summary>
/// Production file-system implementation.
/// </summary>
public sealed class SystemFileSystem : IFileSystem
{
    public static SystemFileSystem Instance { get; } = new();

    public bool FileExists(string path) => File.Exists(path);
}