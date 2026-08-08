# Installation and setup

MOBAflow is currently distributed as source code and version tags. The
repository does not yet publish ready-to-install GitHub release packages.

## Before you start

- Read the [hardware and liability notes](../HARDWARE-DISCLAIMER.md).
- Keep the Z21, PC and Android device on a trusted private LAN.
- Do not forward MOBApi or Z21 ports to the public internet.

## Requirements

### MOBAflow Desktop

- Windows 10/11 compatible with the Windows App SDK target used by the project.
- The .NET SDK selected by [`global.json`](../../global.json).
- Visual Studio with the workloads from [`.vsconfig`](../../.vsconfig), or an
  equivalent command-line setup.

### MOBAsmart

- Android 8.0 / API 26 or newer.
- The .NET MAUI Android workload to build the app.
- A connected Android device or emulator for deployment.

### Layout network

- Roco Z21 command station reachable from the operating device.
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
dotnet restore MOBAsmart/MOBAsmart.csproj -f net10.0-android
dotnet build MOBAsmart/MOBAsmart.csproj -f net10.0-android
```

MOBApi normally builds with MOBAflow and can be started by the desktop app. To
run it separately:

```powershell
dotnet run --project MOBApi/MOBApi.csproj
```

The compatibility endpoint listens on `http://0.0.0.0:5001`. MOBApi also
advertises its certificate-pinned HTTPS endpoint through LAN discovery for the
protected host and MOBAsmart pairing flows.

## First desktop start

1. Open **Settings** in MOBAflow.
2. Confirm the Z21 IP address and UDP port (`21105` by default).
3. Create a solution or open the included sample solution.
4. Select a project and connect to the Z21.
5. Verify feedback and telemetry in **Overview** or **Monitor** before operating
   trains.

If MOBAsmart should synchronize with the desktop, keep the REST API auto-start
setting enabled and confirm that the main-window status bar reports a healthy
API.

## First Android start

1. Connect the phone to the same LAN as the Z21 and desktop PC.
2. Grant notification permission when Android requests it; the foreground
   service uses the notification while a session is active.
3. Wait for the Z21 status to become ON.
4. Enable the **MOBAflow** switch to discover MOBApi and synchronize the current
   solution.
5. In MOBAflow, open **Settings / REST API** and select **Create pairing QR
   code**. In MOBAsmart, open **Pairing**, select **Scan MOBAflow QR code**, and
   scan the displayed code.
6. Confirm that the same six-digit code is shown on both devices, then select
   **Approve administrator** in MOBAflow. MOBAsmart connects and synchronizes
   automatically.
7. The same camera permission is also used when taking photos for upload.

See the [MOBAsmart guide](MOBASMART-USER-GUIDE.md) for the connection model and
tab behavior.

## Firewall and LAN notes

Windows may prompt for firewall access when MOBAflow or MOBApi starts. Allow
access only on private networks.

If MOBAsmart cannot discover the PC, verify at minimum:

- TCP `5001` for MOBApi;
- UDP `21105` between the operating device and Z21; and
- local multicast/broadcast traffic used by discovery.

Guest Wi-Fi, client isolation, VPN routing and phone-to-PC firewall rules are
common causes of discovery failures.

## Validation

Run the repository test project after source changes:

```powershell
dotnet test Test/Test.csproj
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

- Verify the configured IP and that both devices share the same LAN.
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
