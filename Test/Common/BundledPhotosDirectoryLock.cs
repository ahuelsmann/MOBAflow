// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Common;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Serializes tests that create or delete the bundled <c>photos</c> folder next to the test assembly.
/// Stryker starts several test hosts from the same output directory, so <c>NonParallelizable</c> is not enough.
/// </summary>
internal sealed class BundledPhotosDirectoryLock : IDisposable
{
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(30);
    private readonly Mutex _mutex;

    private BundledPhotosDirectoryLock(Mutex mutex) => _mutex = mutex;

    public static BundledPhotosDirectoryLock Acquire(string photosDirectory)
    {
        // Mutex names cannot contain path separators; the hash scopes the lock to this output directory.
        var pathBytes = Encoding.UTF8.GetBytes(Path.GetFullPath(photosDirectory).ToUpperInvariant());
        var mutex = new Mutex(initiallyOwned: false, $"MOBAflow.Test.BundledPhotos.{Convert.ToHexString(SHA256.HashData(pathBytes))}");
        try
        {
            if (!mutex.WaitOne(AcquireTimeout))
            {
                throw new TimeoutException($"Timed out waiting for exclusive access to {photosDirectory}.");
            }
        }
        catch (AbandonedMutexException)
        {
            // A killed test host released the lock; callers still check whether the folder already exists.
        }
        catch
        {
            mutex.Dispose();
            throw;
        }

        return new BundledPhotosDirectoryLock(mutex);
    }

    public void Dispose()
    {
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
