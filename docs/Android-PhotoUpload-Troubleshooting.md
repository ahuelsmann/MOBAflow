# Android Photo Upload Troubleshooting

## Connection Failure Issues

If you're experiencing "Connection failure" errors when uploading photos from the MAUI Android app, here are the common causes and solutions:

### 1. **Android Emulator Network Configuration**

Android emulators have special networking that requires specific IP addresses:

- **Physical Device**: Use the actual LAN IP address of your PC (e.g., `192.168.0.78`)
- **Android Emulator**: Use `10.0.2.2` to access services running on your PC's `localhost`
  - Example: If your WebApp runs on `http://localhost:5001`, configure the MAUI app to use `10.0.2.2:5001`

### 2. **Firewall Configuration**

Ensure Windows Firewall allows incoming connections on port 5001:

```powershell
# Check if rule exists
netsh advfirewall firewall show rule name="MOBAflow WebApp HTTP"

# If not exists, create it
netsh advfirewall firewall add rule name="MOBAflow WebApp HTTP" dir=in action=allow protocol=TCP localport=5001
```

The WinUI app automatically creates this rule, but verify it exists.

### 3. **Network Connectivity**

**For Physical Android Device:**
- Ensure device and PC are on the same WiFi network
- Ping the PC from Android to verify connectivity
- Use PC's LAN IP address (find with `ipconfig` on Windows)

**For Android Emulator:**
- Always use `10.0.2.2` as the server IP
- Do NOT use `localhost`, `127.0.0.1`, or your PC's LAN IP

### 4. **WebApp Server Configuration**

Verify the WebApp is listening on all network interfaces (`0.0.0.0`), not just `localhost`:

In `WebApp/Program.cs`:
```csharp
builder.WebHost.UseUrls("http://0.0.0.0:5001");
```

### 5. **MAUI App Configuration**

Update REST API settings in the MAUI app:

**For Physical Device:**
```
REST API IP: 192.168.0.78  (your PC's actual IP)
REST API Port: 5001
```

**For Android Emulator:**
```
REST API IP: 10.0.2.2
REST API Port: 5001
```

### 6. **Diagnostic Steps**

1. **Test Server Accessibility**
   - Open browser on Android device/emulator
   - Navigate to `http://{server-ip}:5001/api/photos/health`
   - Should return: `{"status":"healthy"}`

2. **Check Server Logs**
   - Look at WebApp console output
   - Verify it shows `Binding: http://0.0.0.0:5001`

3. **Check MAUI Logs**
   - Look for connection test result: `✅ Server {ip}:{port} is reachable`
   - If shows timeout or error, networking is the issue

### 7. **Common Error Messages**

| Error | Cause | Solution |
|-------|-------|----------|
| `Connection failure` | Server unreachable | Check IP, firewall, network |
| `Connection timed out` | Wrong IP or server not running | Verify server is running, use correct IP |
| `Failed to connect to /{ip}:{port}` | Android emulator using wrong IP | Use `10.0.2.2` for emulator |

### 8. **Quick Fix Checklist**

- [ ] WebApp is running and showing `http://0.0.0.0:5001` binding
- [ ] Firewall rule exists for port 5001
- [ ] Using correct IP address:
  - [ ] `10.0.2.2` for Android Emulator
  - [ ] Actual LAN IP for physical device
- [ ] Device/PC on same network (physical device only)
- [ ] Health endpoint accessible from browser on Android

## Additional Notes

- The latest code includes automatic connectivity testing before upload
- Error messages now include troubleshooting tips
- HttpClient configured with 5-minute timeout for large photo uploads
- Android-specific message handler used for better compatibility
