// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

namespace Moba.Common.Security;

/// <summary>
/// Confirms that a bootstrap pipe client is the launched MOBApi process before secrets are exchanged.
/// </summary>
public static partial class HostBootstrapPipeClientVerifier
{
    /// <summary>
    /// Accepts the launched process itself or its direct child, which is the MOBApi process
    /// when MOBAflow starts it through <c>dotnet run</c>.
    /// </summary>
    public static bool IsExpectedClient(int clientProcessId, int? clientParentProcessId, int expectedProcessId)
        => expectedProcessId > 0
           && (clientProcessId == expectedProcessId || clientParentProcessId == expectedProcessId);

    /// <summary>
    /// Throws when the connected client is neither the expected process nor its direct child.
    /// Named pipes on Unix have no client process query; their current-user check still applies there.
    /// </summary>
    internal static void EnsureExpectedClient(NamedPipeServerStream pipe, int expectedProcessId)
    {
        ArgumentNullException.ThrowIfNull(pipe);
        if (!OperatingSystem.IsWindows())
            return;

        if (!NativeMethods.GetNamedPipeClientProcessId(pipe.SafePipeHandle, out var clientProcessId))
            throw new InvalidOperationException("The host bootstrap client process could not be identified.");

        var clientId = (int)clientProcessId;
        if (!IsExpectedClient(clientId, TryGetParentProcessId(clientId), expectedProcessId))
            throw new InvalidOperationException("An unexpected process connected to the host bootstrap channel.");
    }

    [SupportedOSPlatform("windows")]
    private static int? TryGetParentProcessId(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var status = NativeMethods.NtQueryInformationProcess(
                process.SafeHandle,
                NativeMethods.ProcessBasicInformationClass,
                out var information,
                Marshal.SizeOf<NativeMethods.ProcessBasicInformation>(),
                out _);
            return status == 0 ? (int)information.InheritedFromUniqueProcessId : null;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
                                              or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static partial class NativeMethods
    {
        internal const int ProcessBasicInformationClass = 0;

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetNamedPipeClientProcessId(SafePipeHandle pipe, out uint clientProcessId);

        [LibraryImport("ntdll.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial int NtQueryInformationProcess(
            SafeProcessHandle processHandle,
            int processInformationClass,
            out ProcessBasicInformation processInformation,
            int processInformationLength,
            out int returnLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessBasicInformation
        {
            public nint ExitStatus;
            public nint PebBaseAddress;
            public nint AffinityMask;
            public nint BasePriority;
            public nint UniqueProcessId;
            public nint InheritedFromUniqueProcessId;
        }
    }
}