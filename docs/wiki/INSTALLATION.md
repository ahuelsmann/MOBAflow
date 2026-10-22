# Installation and setup

MOBAflow is currently distributed as source code and version tags. The
repository does not yet publish ready-to-install GitHub release packages.

## Before you start

- Read the [hardware and liability notes](../HARDWARE-DISCLAIMER.md).
- Keep the Z21, PC and Android device on a trusted private LAN.
- Do not forward MOBApi or Z21 ports to the public internet.

## Requirements

### MOBAflow Desktop

- Windows 11 22H2 / build 22621 or newer, matching the existing desktop build minimum.
- The .NET SDK selected by [`global.json`](../../global.json).
- Visual Studio with the workloads from [`.vsconfig`](../../.vsconfig), or an
  equivalent command-line setup.

The Windows SDK targeting pack is `10.0.28000.87`; the desktop minimum remains
`10.0.22621.0`. The newer targeting pack does not require the same Windows build
on the user's PC. See [Windows SDK versions](../BUILD-PERFORMANCE.md#windows-sdk-versions)
for the separate .NET SDK, Windows App SDK and native SDK/tool versions.

### MOBAsmart

- Android 8.0 / API 26 or newer.
- The .NET MAUI Android workload to build the app.
- A connected Android device or emulator for deployment.

### Layout network

- One Roco Z21 command station per project/layout, reachable from the operating
  devices. Multiple projects in one solution run in parallel.
- A LAN that permits device-to-device UDP and TCP traffic.
- Feedback modules and other layout hardware as required by your setup.

## Build from source

Clone the repository and open [`Moba.slnx`](../../Moba.slnx) in Visual Studio.
The solution contains the Windows app, Android app, local API, shared libraries
and tests.

Equivalent command-line builds are:

```powershell
git clone https://github.com/ahuelsmann/MOBAflow.git
cd MOBAflow

dotnet restore MOBAflow/MOBAflow.csproj
dotnet build MOBAflow/MOBAflow.csproj
dotnet run --project MOBAflow/MOBAflow.csproj
```

For Android:

```powershell
dotnet restore MOBAsmart/MOBAsmart.csproj
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android
```

MOBApi normally builds with MOBAflow and can be started by the desktop app. To
run it separately:

```powershell
dotnet run --project MOBApi/MOBApi.csproj
```

MOBApi listens on all network interfaces, using HTTP port `5001` by default.
Discovery announces a reachable LAN address and the configured port so
MOBAsmart can find it. `0.0.0.0` is a listen address, not an address to enter
on the phone. If you change the port, adjust the private-network firewall rule
as well. See the [project reference](../PROJECT-REFERENCE.md#mobapi-endpoints).

## First desktop start

1. Create a solution or open the included sample solution.
2. On **Solution**, use **Z21 Finder** to search the network and drag an unassigned
   command station onto each project, or enter its IP address and UDP port in that
   project's properties (`21105` by default).
3. Each project starts its own runtime and connects to its assigned Z21. A missing
   address or a duplicate assignment leaves the affected project disconnected;
   the other projects keep running. Z21 endpoints are not app-wide settings.
4. Select a project to view its state. This changes the displayed runtime and the
   target of page commands; it does not stop the other projects.
5. Verify feedback and telemetry in **Overview** or **Monitor** before operating
   trains.

Opening or creating another solution, deleting a connected project or closing
MOBAflow asks **Stop all trains?**. Confirmation sends speed 0 to the known
locomotives of the affected runtimes before discarding them; cancellation keeps
them running. Changing a connected project's Z21 assignment also stops its known
locomotives before reconnecting. See the
[desktop guide](MOBAFLOW-USER-GUIDE.md#projects-z21-assignment-and-safe-switching).

If MOBAsmart should synchronize with the desktop, keep the REST API auto-start
setting enabled and confirm that the main-window status bar reports a healthy
API.

## First Android start

1. Connect the phone to the same LAN as the Z21 and desktop PC.
2. Grant notification permission when Android requests it; the foreground
   service uses the notification while a session is active.
3. Enable the **MOBAflow** switch. MOBAsmart discovers MOBApi on the LAN and
   synchronizes the current solution. It remembers the MOBApi addresses it found
   and tries them first next time; tap the MOBAflow status to search again.
4. Select a project with **Project** on the Counter tab, then wait for its Z21
   status to become ON. The phone uses that project's address and port; it does
   not search the network for a Z21.
5. Android asks for camera permission only when you take photos for upload.

See the [MOBAsmart guide](MOBASMART-USER-GUIDE.md) for the connection model and
tab behavior.

## Firewall and LAN notes

Use MOBAflow and MOBApi only on a trusted home LAN, as described in the
[operating model](../../SECURITY.md#operating-model). They provide no sign-in or
transport encryption. Do not forward these ports to the internet.

| Connection | Protocol / default port | Direction and purpose |
| --- | --- | --- |
| MOBAsmart to the PC | TCP `5001` | Inbound on the PC for MOBApi REST and SignalR; responses return on the same connection. Use the configured port if changed. |
| MOBApi discovery | UDP `21106` | Inbound discovery requests on the PC; replies return to the requesting client. Allow local discovery multicast/broadcast through the access point. |
| Operating device to Z21 | UDP `21105` | Outbound to the command station and returning feedback to the operating device. This is separate from the two MOBApi helper rules. |

Windows may prompt for access when an app starts. Choose Private for a trusted
home LAN. This prompt does not restrict rules created separately by
[`FirewallHelper`](../../MOBAflow/Service/FirewallHelper.cs): when permissions
allow, MOBAflow recreates the inbound TCP and discovery UDP rules with
`profile=any`, covering Private, Domain **and Public** Windows profiles. The
current helper does not enforce a private-profile restriction.

Review the rules in Windows Defender Firewall with Advanced Security. Restrict
both `MOBAflow WebApp UDP Discovery` and `MOBAflow WebApp REST-API (Port <configured port>)`
to Private for home use. If MOBAflow later recreates them with sufficient rights,
check the profiles again. Do not use the service on an untrusted shared network.
The current removal helper targets the obsolete TCP rule name for port `5000`;
remove any remaining rule for the configured port manually when uninstalling.

Guest Wi-Fi, client isolation, VPN routing and phone-to-PC firewall rules are
common causes of discovery failures. Check TCP `5001` (or the configured port),
UDP `21106` discovery and the independent UDP `21105` Z21 path.

## Validation

Select the appropriate test target after source changes. Documentation-only
changes need structural checks, not a product build; see [AGENTS.md](../../AGENTS.md):

```powershell
# Portable tests: limit both the restore graph and the test target.
dotnet test Test/Test.csproj -p:TargetFrameworks=net10.0 -f net10.0
# Windows tests (with the required desktop toolchain):
dotnet test Test/Test.csproj -f net10.0-windows10.0.22621.0 -p:IncludeMobaSmartTests=false
```

Windows and Android projects require their platform workloads. Cross-platform
libraries, MOBApi and most tests can be built separately when those workloads
are unavailable.

## Troubleshooting

### Build fails before compilation

- Check `dotnet --info` against [`global.json`](../../global.json).
- Install missing Visual Studio/.NET workloads from [`.vsconfig`](../../.vsconfig).
- Restore the specific app project instead of scanning generated `bin`, `obj` or
  repository-local package directories.

### Z21 cannot be reached

- Verify the project's Z21 IP and port and that the operating devices share the
  same LAN. Check project diagnostics for duplicate assignments.
- Check the private-network firewall profile.
- Confirm that another app has not left stale Z21 clients; restart the app and,
  if necessary, the command station.

### MOBApi cannot start

- Check whether another process already uses TCP port `5001`.
- Inspect the MOBAflow REST API status and Monitor output.
- Run MOBApi separately to see startup errors directly.

## Related guides

- [MOBAflow Desktop](MOBAFLOW-USER-GUIDE.md)
- [MOBAsmart](MOBASMART-USER-GUIDE.md)
- [Piper TTS](PIPER-TTS-SETUP.md)
- [Build performance](../BUILD-PERFORMANCE.md)
