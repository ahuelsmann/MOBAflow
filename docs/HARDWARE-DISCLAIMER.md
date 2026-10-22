# ⚠️ Hardware & Liability Disclaimer

## Legal Clarification

MOBAflow is an **independent open-source project** and is **NOT**
officially affiliated with Roco, Piko, AnyRail, or any other
third-party manufacturers.

---

## 🚂 Z21 Hardware Operation

### Software Limits and Mandatory Rights

MOBAflow communicates with Roco Z21 Digital Command Stations via UDP and sends
commands to physical hardware. It is not a certified safety system and does not
guarantee safe wiring, protection against hardware damage, or the integrity of
a command station's configuration. Hardware installation and repair require the
manufacturer's guidance and appropriate expertise.

The software is supplied under the [MIT License](../LICENSE). This additional
hardware guidance does not exclude or limit liability where applicable law
prohibits doing so. In particular, it does not exclude liability for intentional
acts, gross negligence, culpable injury to life, body or health, or other mandatory
statutory liability. The original license texts remain unchanged.

For the German legal limits, see [section 276(3) BGB](https://www.gesetze-im-internet.de/bgb/__276.html)
and, where the rules for standard contract terms apply, [section 309 no. 7 BGB](https://www.gesetze-im-internet.de/bgb/__309.html).
This document is operating guidance, not a safety certification or a determination
of the legal outcome of an individual case.

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
✓ Do not expose the Z21 or MOBApi through Internet port forwarding
```

### Security Guidelines

```text
✓ Never expose UDP port 21105 to the Internet!
✓ Use only trusted private networks and trusted devices
✓ Restrict firewall access to the trusted private network
✓ Regular firmware updates for Z21
✓ Check photos and user-entered content before sharing them
```

---

MOBApi uses HTTP without sign-in or transport encryption. Devices on the trusted
home network can read layout data and send commands. Uploaded photos and
user-entered content can contain personal or sensitive information; their
sensitivity depends on what the user supplies. See the [security policy](../SECURITY.md)
and the [MOBAsmart privacy notes](wiki/MOBASMART-USER-GUIDE.md#privacy-and-safety).

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
- **Discover security issues** → See [`SECURITY.md`](../SECURITY.md)
- **Test Z21 compatibility** → Please test and report!
- **Improve documentation** → Pull requests welcome!

---

## Operational Summary

Read the Z21 manufacturer's documentation, verify the layout and power supply,
and keep an independent emergency-stop or power-disconnection option within reach.
Software controls do not replace safe electrical installation or hardware
protection. Follow the applicable license terms and the mandatory-rights
clarification above.

---

> Last Update: 2026-10-20
