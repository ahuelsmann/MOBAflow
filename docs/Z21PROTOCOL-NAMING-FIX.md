# Z21Protocol Naming Fix Guide

**Issue:** Z21Protocol.cs uses `UPPER_SNAKE_CASE` but backend code still references `PascalCase`.

**Solution:** Global search & replace to align all references.

---

## Files to Update

### 1. Backend/Protocol/Z21Command.cs
**Search:** 
```
Z21Protocol.Header.LanGetSerialNumber
Z21Protocol.Header.LanGetHwinfo
Z21Protocol.Header.LanLogoff
Z21Protocol.Header.LanSystemstateGetdata
Z21Protocol.Header.LanSetBroadcastflags
Z21Protocol.Header.LanXHeader
Z21Protocol.XHeader.XTrackPower
Z21Protocol.XHeader.XSetStop
Z21Protocol.XHeader.XGetStatus
Z21Protocol.XHeader.XSetLocoDrive
Z21Protocol.XHeader.XSetLocoFunction
Z21Protocol.XHeader.XGetLocoInfo
Z21Protocol.TrackPowerDb0.On
Z21Protocol.TrackPowerDb0.Off
```

**Replace With:**
```
Z21Protocol.Header.LAN_GET_SERIAL_NUMBER
Z21Protocol.Header.LAN_GET_HWINFO
Z21Protocol.Header.LAN_LOGOFF
Z21Protocol.Header.LAN_SYSTEMSTATE_GETDATA
Z21Protocol.Header.LAN_SET_BROADCASTFLAGS
Z21Protocol.Header.LAN_X_HEADER
Z21Protocol.XHeader.X_TRACK_POWER
Z21Protocol.XHeader.X_SET_STOP
Z21Protocol.XHeader.X_GET_STATUS
Z21Protocol.XHeader.X_SET_LOCO_DRIVE
Z21Protocol.XHeader.X_SET_LOCO_FUNCTION
Z21Protocol.XHeader.X_GET_LOCO_INFO
Z21Protocol.TrackPowerDb0.ON
Z21Protocol.TrackPowerDb0.OFF
```

### 2. Backend/Protocol/Z21MessageParser.cs
**Search:**
```
Z21Protocol.Header.LanXHeader
Z21Protocol.Header.LanRmbusDatachanged
Z21Protocol.Header.LanSystemstate
Z21Protocol.Header.LanGetSerialNumber
Z21Protocol.Header.LanGetHwinfo
Z21Protocol.XHeader.XStatus
Z21Protocol.XHeader.XStatusChanged
Z21Protocol.XHeader.XLocoInfo
```

**Replace With:**
```
Z21Protocol.Header.LAN_X_HEADER
Z21Protocol.Header.LAN_RMBUS_DATACHANGED
Z21Protocol.Header.LAN_SYSTEMSTATE
Z21Protocol.Header.LAN_GET_SERIAL_NUMBER
Z21Protocol.Header.LAN_GET_HWINFO
Z21Protocol.XHeader.X_STATUS
Z21Protocol.XHeader.X_STATUS_CHANGED
Z21Protocol.XHeader.X_LOCO_INFO
```

### 3. TrackPlan.Renderer/TrackPlanSvgRenderer.cs
**Search:**
```
if (segment is WR wrSegment)
CalculateWRPortPosition(
```

**Issue:** The `WR` type might not be imported or found.  
**Action:** Check using statements and ensure TrackLibrary.PikoA is imported.

---

## Quick Fix Commands (Visual Studio)

### Find & Replace Dialog
1. **Edit → Find and Replace** (Ctrl+H)
2. **Enable Regular Expressions** (Alt+E in Find dialog)
3. **Use patterns below**

### Pattern 1: LanGetSerialNumber
```regex
Find: Z21Protocol\.Header\.Lan([A-Z][a-zA-Z]+)
Replace: Z21Protocol.Header.LAN_${1:upper}
```

### Pattern 2: XHeader values
```regex
Find: Z21Protocol\.XHeader\.X([A-Z][a-zA-Z]+)
Replace: Z21Protocol.XHeader.X_${1:upper}
```

---

## Manual File Updates

If regex doesn't work, use this approach:

```csharp
// BEFORE (in Z21Command.cs)
public static byte[] BuildGetSerialNumber()
    => [0x04, 0x00, Z21Protocol.Header.LanGetSerialNumber, 0x00];

// AFTER
public static byte[] BuildGetSerialNumber()
    => [0x04, 0x00, Z21Protocol.Header.LAN_GET_SERIAL_NUMBER, 0x00];
```

---

## Verification Checklist

After all replacements:
```
[ ] dotnet clean
[ ] dotnet build -c Release
    ✓ No CS0117 errors about "Header contains no definition..."
    ✓ No CS0246 errors about missing types
[ ] Run unit tests
    ✓ All protocol-related tests pass
[ ] Test app startup
    ✓ MainWindow appears
    ✓ Z21 connection works
    ✓ No runtime exceptions
```

---

## Tool: Batch Replacement Script

```powershell
# PowerShell script to do replacements

$replacements = @(
    @{ Old = "LanGetSerialNumber"; New = "LAN_GET_SERIAL_NUMBER" },
    @{ Old = "LanGetHwinfo"; New = "LAN_GET_HWINFO" },
    @{ Old = "LanLogoff"; New = "LAN_LOGOFF" },
    @{ Old = "LanSystemstateGetdata"; New = "LAN_SYSTEMSTATE_GETDATA" },
    @{ Old = "LanSetBroadcastflags"; New = "LAN_SET_BROADCASTFLAGS" },
    @{ Old = "LanXHeader"; New = "LAN_X_HEADER" },
    @{ Old = "LanRmbusDatachanged"; New = "LAN_RMBUS_DATACHANGED" },
    @{ Old = "LanSystemstate"; New = "LAN_SYSTEMSTATE" },
    @{ Old = "XTrackPower"; New = "X_TRACK_POWER" },
    @{ Old = "XSetStop"; New = "X_SET_STOP" },
    @{ Old = "XGetStatus"; New = "X_GET_STATUS" },
    @{ Old = "XSetLocoDrive"; New = "X_SET_LOCO_DRIVE" },
    @{ Old = "XSetLocoFunction"; New = "X_SET_LOCO_FUNCTION" },
    @{ Old = "XGetLocoInfo"; New = "X_GET_LOCO_INFO" },
    @{ Old = "XStatus"; New = "X_STATUS" },
    @{ Old = "XStatusChanged"; New = "X_STATUS_CHANGED" },
    @{ Old = "XLocoInfo"; New = "X_LOCO_INFO" },
)

# Files to update
$files = @(
    "Backend\Protocol\Z21Command.cs",
    "Backend\Protocol\Z21MessageParser.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        
        foreach ($replacement in $replacements) {
            $content = $content -replace $replacement.Old, $replacement.New
        }
        
        Set-Content $file $content -Encoding UTF8
        Write-Host "Updated: $file"
    }
}

Write-Host "Done!"
```

---

## Frequency of Updates Needed

After fixing initial inconsistency:
- **Going forward:** Ensure **all new** Z21Protocol constants use `UPPER_SNAKE_CASE`
- **Code review:** Catch PascalCase references early
- **EditorConfig:** Could enforce this with analyzers
