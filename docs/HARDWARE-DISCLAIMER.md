# ⚠️ Hardware & Liability Disclaimer

## Legal Clarification

MOBAflow is an **independent open-source project** and is **NOT**
officially affiliated with Roco, Piko, AnyRail, or any other
third-party manufacturers.

---

## 🚂 Z21 Hardware Operation

### General Liability Disclaimer

**MOBAflow communicates with Roco Z21 Digital Command Stations via UDP
connections. Usage is entirely at YOUR OWN RISK.**

By using MOBAflow, you accept the following conditions:

#### ❌ **What MOBAflow does NOT provide:**

- ❌ No warranty for the safety of your model railroad layout
- ❌ No warranty for protection against hardware damage
- ❌ No warranty for the integrity of your Z21 configuration
- ❌ No technical support for Z21 hardware failures
- ❌ No liability for transformer overload, short circuits, or damage

#### ✅ **What is YOUR responsibility:**

- ✅ Proper **Z21 installation** and configuration
- ✅ Compliance with all **local electrical regulations**
- ✅ Regular **inspection** of your model railroad layout
- ✅ Correct **network configuration** (IP addresses, firewalls)
- ✅ Backups of your **configuration files**
- ✅ Understanding of **Z21 documentation** (by Roco)

---

## 📋 Prerequisites for Safe Use

### Hardware Requirements

```text
✓ Roco Z21 Digital Command Station (current firmware)
✓ Reliable power supply (recommended: UPS)
✓ Stable LAN/WLAN (2.4GHz/5GHz, distance <10m)
✓ Windows PC (for WinUI Desktop) or Android (for MOBAsmart)
```

### Network Configuration

```text
✓ Z21 in the same network as MOBAflow
✓ No firewall blocking of UDP port 21105
✓ Static IP for Z21 recommended (avoid DHCP conflicts)
✓ NAT/Port forwarding over the Internet NOT recommended!
```

### Security Guidelines

```text
✓ Never expose UDP port 21105 to the Internet!
✓ Use local networks only (LAN/WLAN)
✓ VPN for remote access (not direct port forwarding)
✓ Regular firmware updates for Z21
✓ No sensitive data transmitted via MOBAflow
```

---

## 🔧 Pre-Operation Checklist

The following points **MUST** be checked by the user before use:

```text
[ ] Z21 hardware is functional and tested
[ ] Z21 firmware is current according to the hardware vendor
[ ] Power supply is stable and tested
[ ] Network connection is stable (ping to Z21 <10ms)
[ ] Windows/Android device is connected to Z21 via LAN/WLAN
[ ] MOBAflow has been tested with Z21 examples
[ ] A backup of Z21 configuration has been created
[ ] README and documentation have been read
```

---

## 🛑 Emergency & Error Handling

### Problems during operation?

#### MOBAflow error → Restart the application

```text
1. Close MOBAflow (Alt+F4)
2. Check Z21 power button (LED green?)
3. Restart MOBAflow
4. Problem persists? → See Support section below
```

#### Z21 not responding → Hardware inspection

```text
1. Check Z21 power cable
2. Test Z21 with Roco App (to exclude MOBAflow)
3. Test network connection with `ping <z21-ip>`
4. Restart Z21 (power switch)
```

#### Model railroad layout is hot/smells strange → TURN OFF IMMEDIATELY!

```text
1. Z21 power switch to OFF
2. Disconnect power supply
3. Wait until transformer cools
4. Check for short circuits in layout
5. DO NOT RESTART UNTIL PROBLEM IS RESOLVED
```

---

## 📞 Support & Help

### MOBAflow-specific issues

- **GitHub Issues:**
  [https://github.com/ahuelsmann/MOBAflow/issues](https://github.com/ahuelsmann/MOBAflow/issues)
- **Discussions:**
  [https://github.com/ahuelsmann/MOBAflow/discussions](https://github.com/ahuelsmann/MOBAflow/discussions)

---

## 📜 Licensing Notice

MOBAflow itself is published under the **MIT License** (see [`LICENSE`](../LICENSE)).

Z21 hardware and software by Roco are subject to **Roco's own license terms**.

See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md) for complete
information on dependencies and third-party software.

---

## 🤝 Contributing & Reporting Issues

If you:

- **Find bugs in MOBAflow** → Create GitHub Issue
- **Discover security issues** → See [`SECURITY.md`](SECURITY.md)
- **Test Z21 compatibility** → Please test and report!
- **Improve documentation** → Pull requests welcome!

---

## Final Clarification

**By using MOBAflow, you accept that:**

1. ✅ MOBAflow is provided "AS IS" (as available)
2. ✅ Usage is entirely your responsibility
3. ✅ No support is provided for hardware failures or damage
4. ✅ MOBAflow developers are not liable for model railroad layout
   damage
5. ✅ You have read and understood the Z21 documentation

**Safety is your responsibility.** Use MOBAflow only if you
understand your model railroad layout and follow all safety
guidelines.

---

> Last Update: February 2026
