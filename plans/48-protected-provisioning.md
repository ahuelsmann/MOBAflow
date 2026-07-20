# RF-02 Protected ESP32 Provisioning Plan

## Document status

- Status: Proposed security design; review required
- Primary issue: [#48](https://github.com/ahuelsmann/MOBAflow/issues/48)
- Parent programme: [#47](https://github.com/ahuelsmann/MOBAflow/issues/47)
- Recommended implementation prerequisite: [RF-01 / #49](https://github.com/ahuelsmann/MOBAflow/issues/49)
- Baseline: `main` at `43953ac43431abc8f0a51604175b1114d24111ff`
- Scope owner: RF-02 only

This plan is the single issue-specific plan for #48. This revision authorizes threat modelling and security design only. It does not authorize production firmware changes.

Production implementation remains blocked until both conditions are true:

1. RF-01 defines and tests the length-safe parser contract that every network decoder can reuse.
2. The activation, credential, storage, recovery, and factory-reset decisions in this plan are reviewed and approved in #48.

## Outcome

Provisioning must be a physically initiated, authenticated, encrypted, time-limited maintenance state. Normal operation must not expose a setup access point or credential-management endpoint. A failed station connection must not silently reopen provisioning.

The design protects Wi-Fi credentials against nearby radio attackers, clients that have not completed the current pairing proof, accidental diagnostic disclosure, stale setup credentials, interrupted rotation, and opportunistic access after reboot.

## Scope boundaries

In scope:

- first-boot and physical activation policy;
- a device-specific, per-window WPA2 credential;
- application-layer authentication and encryption for provisioning messages;
- bounded provisioning lifetime and retry behavior;
- credential replacement, rollback, recovery, and provisioning factory reset;
- secret-free security event logging;
- host-native negative tests and hardware lifecycle acceptance;
- production storage and device-hardening gates for provisioning secrets.

Out of scope:

- display capability negotiation, frame transport, discovery, test patterns, and display adapters from #36;
- MOBApi authentication and authorization from RF-03;
- general Wi-Fi feature expansion or automatic hardware discovery;
- implementation of the RF-01 parser contract;
- production firmware changes in this design-only pull request.

RF-02 may define only the secure readiness assumption consumed by #36: a device is either operational with no provisioning surface, or it is in an explicitly activated provisioning window. It must not define #36 protocol messages or capabilities.

## Verified current exposure

The current `MOBAdisplay/esp32/src/main.cpp` behavior establishes the security baseline:

- `startConfigAccessPoint()` starts `WiFi.softAP(apName)` without a password.
- successful station connection leaves the device in `WIFI_AP_STA` and starts the configuration AP.
- missing credentials and saved-credential connection failure both enter setup mode automatically.
- `GET /api/wifi/status` and `POST /api/wifi/config` are registered without authentication.
- the POST handler stores supplied credentials before proving that the station connection succeeds.
- the configuration server remains active during normal operation.
- credentials are stored through `Preferences`; encrypted NVS and production eFuse policy are not configured by this firmware project.

No current serial statement intentionally prints the Wi-Fi password, but the existing endpoint and lifecycle behavior do not meet #48.

## Assets and security objectives

| Asset | Required property |
| --- | --- |
| Target Wi-Fi SSID and passphrase | Confidential in transit, at rest, responses, displays after setup, and logs |
| Per-window setup credential | Unpredictable, device-specific, short-lived, never logged, invalid after close or reboot |
| Provisioning authorization state | Cannot be entered remotely; expires deterministically |
| Last-known-good network configuration | Atomic replacement and recovery after failed rotation or power loss |
| Device identity | Stable enough to select the intended device, but not treated as a secret |
| Firmware and storage policy | Prevent unauthorized firmware or flash readout from bypassing credential controls in production |
| Audit events | Useful for lifecycle diagnosis without credentials, tokens, proofs, request bodies, or stable client identifiers |

## Trust boundaries and threat actors

Trust boundaries:

1. Physical operator to the BOOT button and local TFT.
2. Provisioning client to the temporary WPA2 SoftAP.
3. Authenticated provisioning session to the credential parser and state machine.
4. Volatile session state to persistent NVS state.
5. Firmware and serial diagnostics to any attached development host.

Threat actors:

- a nearby unauthenticated Wi-Fi client;
- a client that can see the SoftAP but does not possess the current on-device proof;
- a previously authorized client holding an expired setup value;
- a local-network client scanning normal operational endpoints;
- an operator who enters invalid credentials or loses power during rotation;
- a person with temporary physical access to the button, display, UART, or flash;
- malformed or oversized network input reaching a decoder.

Invasive chip decapsulation and laboratory side-channel attacks are not acceptance targets. Casual flash readout, UART download access, unsigned firmware replacement, and exposed debug interfaces are production threats and are addressed by the hardening gate below.

## Threat model

| ID | Threat | Existing weakness | Required control | Verification |
| --- | --- | --- | --- | --- |
| T1 | Unauthorized association | Setup AP is open | Random per-window WPA2-CCMP passphrase shown only on the TFT | Wrong and previous passphrases cannot associate |
| T2 | Remote provisioning activation | AP opens automatically after failures and remains active | Runtime physical hold is the only activation path; first boot waits for that gesture | Boot and station failure leave AP and server off |
| T3 | Unauthorized configuration read or write | Endpoints accept anonymous HTTP | Authenticated encrypted provisioning protocol; no credential read endpoint | Missing, invalid, expired, and replayed proofs are rejected |
| T4 | Credential interception or modification | Plain HTTP carries target credentials | Application-layer authenticated encryption in addition to WPA2 | Packet capture contains no plaintext target credential |
| T5 | Stale credential reuse | Setup access has no lifetime or rotation | Generate a new setup credential for every activation; erase it on close and reboot | Prior-window values fail after success, timeout, cancel, and reboot |
| T6 | Persistent attack surface | HTTP server runs in normal mode | Start server only inside the provisioning state; stop server and SoftAP on every exit | Port scan in operational mode finds no provisioning service |
| T7 | Loss of working configuration | New credentials are committed before validation | Pending and active slots; promote only after successful station connection | Failed rotation and power loss preserve last-known-good settings |
| T8 | Recovery becomes an open backdoor | Failed station connection automatically starts setup | Show offline status and require a fresh physical gesture | Reboot and repeated connection failures never open AP |
| T9 | Secret disclosure through diagnostics | Future handlers may log inputs or proofs | Allowlisted event schema and secret canary tests | Serial and persistent logs contain none of the canary values |
| T10 | Storage extraction or firmware bypass | Plain Preferences storage and no stated production hardening | Encrypted NVS, Flash Encryption Release mode, Secure Boot v2, and restricted debug/download interfaces | Production hardware reports expected security state; flash dump lacks plaintext canaries |
| T11 | Parser memory-safety failure | Network parsing currently mixes fixed buffers and implicit string assumptions | Reuse the reviewed RF-01 pointer-plus-length parser boundary and explicit maximums | Boundary, malformed-input, and fuzz tests pass before hardware rollout |
| T12 | Denial of service or brute force | No attempt or session limits | Single active session, bounded attempts, fixed window, cooldown, and deterministic close | Attempt-limit and timeout tests close the service |

## Security design decisions

### 1. Physical activation and first boot

- The AP is off by default in every boot state.
- An unprovisioned first boot shows `Hold BOOT for setup` on the TFT. It does not start the AP merely because credentials are absent.
- The ESP32-S3-DevKitC-1 BOOT button is read as an application input only after normal boot. Holding it during reset enters the ROM download path and is not an RF-02 gesture.
- Holding BOOT continuously for 5 seconds and releasing before 12 seconds opens one provisioning window. Debounce and duration use wrap-safe monotonic time.
- A saved station connection failure shows an offline/recovery prompt and continues bounded station retries. It never opens the AP.
- Remote packets, HTTP requests, UDP traffic, reboot counts, and connection failures cannot activate provisioning.

The exact GPIO and active level must be isolated in board configuration and verified against the target board revision before implementation. RF-02 must not hardcode a button assumption into portable display behavior.

### 2. Device-specific setup credential

- Each accepted physical activation generates a fresh 128-bit random setup secret with `esp_fill_random()` while a documented hardware entropy source is active.
- The temporary SSID is `MOBAflow-Setup-<device-suffix>`. The suffix comes from the device identifier and is public.
- A WPA2-CCMP passphrase is encoded from the random secret using an unambiguous uppercase alphabet and contains at least 16 random characters. It is unique to the device and provisioning window.
- The TFT displays the SSID, grouped passphrase, remaining time, and device suffix only while the window is active.
- The setup secret exists in RAM only. It is not compiled in, stored in source, persisted to NVS, printed on serial, returned by an endpoint, or reused for a later window.
- Closing the window invalidates the secret, destroys session state, clears the display, and overwrites mutable secret buffers on a best-effort basis.

The MAC-derived suffix is never used as a password or entropy source.

### 3. Provisioning transport and endpoint protection

The preferred implementation is Espressif Protocomm Security 2 over SoftAP: SRP6a authentication with AES-GCM protected provisioning messages. The per-window setup secret supplies the password material used to construct the Security 2 salt and verifier. The implementation review must confirm Arduino/PlatformIO integration and memory use before production code is approved.

Security 0, Basic authentication over plain HTTP, a bearer token sent over unprotected HTTP, and WPA2-only endpoint authorization are rejected designs.

The provisioning surface has these rules:

- no endpoint can return stored SSIDs, passwords, setup secrets, proofs, verifier material, or session keys;
- status responses expose only the provisioning state, remaining window time, attempt count, and coarse result code;
- credential write accepts one bounded message with explicit SSID and passphrase lengths;
- all message decoding uses the RF-01 pointer-plus-length contract, rejects embedded truncation assumptions, and applies fixed maximums before copying;
- one authenticated session is allowed at a time;
- authentication failures are capped at 10 per window, then the service closes and enforces a 60-second local cooldown;
- replayed, duplicate, expired, or out-of-state requests are rejected or handled idempotently according to the reviewed protocol contract;
- normal operational mode does not register or listen on the provisioning port.

If Protocomm Security 2 cannot be integrated within measured flash, RAM, and framework constraints, implementation stops for a revised design review. It must not fall back silently to a custom cryptographic protocol or plaintext HTTP.

### 4. Provisioning window and closure

- A window lasts 10 minutes from physical activation, measured with monotonic time.
- The countdown does not extend when a client connects, authenticates, submits invalid credentials, or causes a station failure.
- The window closes immediately after the device proves a successful station connection and obtains an IP address.
- It also closes on timeout, authentication-attempt exhaustion, explicit local cancel, factory-reset completion, or reboot.
- Exit order is: stop accepting requests, invalidate the authenticated session, stop/deinitialize the provisioning server, disconnect and erase the SoftAP configuration, select STA or OFF mode, clear volatile secrets, update the TFT, then emit the redacted close event.
- A watchdog or error path must converge on the same close routine. There is no alternate exit that leaves AP+STA active.

### 5. Credential storage, rotation, and rollback

Persistent state uses versioned `active` and `pending` records with integrity validation:

1. Receive new credentials only inside an authenticated session.
2. Validate lengths and reject unsupported security modes without logging submitted values.
3. Write a pending record without replacing the active record.
4. Attempt the station connection within the remaining window.
5. After successful association and IP acquisition, atomically promote pending to active and remove the previous active record.
6. On failure, timeout, reboot, or corrupt pending data, erase pending and retain or restore the last-known-good active record.

There is no credential read API. Rotation is a new physically activated provisioning window, not a remote edit during normal operation. Every rotation also rotates the temporary AP passphrase and application-layer session material.

Production storage requires encrypted NVS with a protected NVS key partition. Flash Encryption must use Release mode with a unique per-device key, Secure Boot v2 must verify firmware, and unused debug/UART download access must be restricted according to the approved manufacturing and recovery procedure. These irreversible eFuse choices require a separate hardware rehearsal and sign-off before production enablement.

### 6. Recovery and deterministic reboot behavior

| Starting state | Event | Required next state |
| --- | --- | --- |
| Unprovisioned | Normal boot | AP off; TFT requests physical activation |
| Unprovisioned | 5-second gesture | Fresh 10-minute provisioning window |
| Provisioned | Normal boot and connect succeeds | STA only; provisioning server absent |
| Provisioned | Normal boot and connect fails | STA retry/offline state; AP off; physical recovery prompt |
| Provisioned | 5-second gesture | Fresh window; active credentials retained until replacement succeeds |
| Window active | Invalid target credentials | Pending cleared; active retained; window continues only if time/attempt budget remains |
| Window active | Successful target connection | Promote pending; close server and AP immediately; STA only |
| Window active | Reboot or power loss | Volatile session lost; pending discarded on boot; AP remains off |
| Window active | Timeout or attempt exhaustion | Server and AP stop; pending discarded; active retained |

Recovery never weakens authentication and never automatically reopens provisioning.

### 7. Factory reset

- Holding BOOT for at least 12 seconds during normal application runtime enters a destructive reset confirmation screen.
- The display counts down a further 5 seconds; releasing before completion cancels. Continuing to hold confirms.
- Confirmation erases Wi-Fi active/pending credentials, provisioning/session metadata, and provisioning audit counters, then reboots to the unprovisioned AP-off state.
- It does not erase or redefine display capability, frame transport, or other #36-owned configuration.
- The reset operation emits only start, cancel, success, or failure event codes. It never prints erased data.
- Reset behavior must be idempotent and recover safely if power is lost during erasure.

The gesture timings and screen copy are review decisions and must be verified on the actual enclosure so accidental activation is acceptably unlikely.

### 8. Secret-free logs and diagnostics

Security events use a fixed allowlist rather than interpolating request data:

- `provisioning_activation_accepted`
- `provisioning_activation_rejected`
- `provisioning_session_authenticated`
- `provisioning_authentication_failed`
- `provisioning_credentials_rejected`
- `provisioning_station_connect_failed`
- `provisioning_completed`
- `provisioning_closed`
- `provisioning_factory_reset_started`
- `provisioning_factory_reset_completed`
- `provisioning_factory_reset_failed`

Allowed fields are event code, firmware version, public device suffix, state transition, coarse reason enum, attempt count, and monotonic elapsed milliseconds. Logs must never contain:

- target SSID or Wi-Fi passphrase;
- temporary AP passphrase or raw setup secret;
- authorization headers, request bodies, proof values, salts, verifiers, nonces, session keys, or tokens;
- full client MAC or IP address;
- raw malformed packets or flash contents.

Library logging is configured so Wi-Fi and provisioning components cannot dump credentials. Tests use unique canary values and scan the complete serial capture for absence, including failure and reset paths.

## Parser and input contract gate from RF-01

RF-02 production work may begin only after #49 supplies a reviewed parser seam with these reusable properties:

- every decoder receives `(bytes, length)` or an equivalent bounded span;
- classification checks length before prefix or fixed-field access;
- text construction always supplies an explicit length and defines UTF-8/control-character policy;
- size limits are constants covered at `limit - 1`, `limit`, and `limit + 1`;
- malformed and unknown inputs have deterministic results and do not mutate provisioning state;
- host-native fuzz/property tests can invoke the decoder without Arduino networking or display dependencies.

RF-02 will add credential-message and state-transition tests to that seam. It will not modify the #36 frame/capability protocol.

## Planned implementation slices after approval

No slice below is authorized by this design-only change. After the gates are met, implementation should remain independently reviewable:

1. Extract a host-native provisioning state machine and bounded request model using the RF-01 parser contract.
2. Add physical activation and TFT-only setup credential presentation behind board configuration.
3. Integrate the reviewed protected provisioning transport and single-session policy.
4. Add transactional active/pending encrypted storage, rotation, and rollback.
5. Add deterministic close, recovery, factory reset, and allowlisted audit events.
6. Add PlatformIO build coverage, host-native negative tests, and the hardware acceptance suite.
7. Rehearse Secure Boot, Flash Encryption Release mode, encrypted NVS, recovery, and manufacturing key handling on disposable hardware before production enablement.

Each behavioral slice requires tests before commit. Production firmware must not combine RF-02 with #36 refactoring.

## Validation strategy

### Design-only validation for this revision

- exactly one `plans/48-*.md` exists;
- issue #48 links this file and this file links #48, #47, and #49;
- the diff contains no production firmware, host, API, or #36 changes;
- Markdown links and headings are valid;
- `git diff --check` passes;
- the plan explicitly covers activation, first boot, device-specific WPA2, timeout, endpoint protection, rotation, recovery, factory reset, logs, rollback, and hardware acceptance.

No .NET or PlatformIO behavior changed in this revision, so build and runtime tests provide no additional signal. The later implementation must run the repository-mandated build and test gates.

### Host-native tests required for implementation

- state-machine transitions for every row in the recovery table;
- boundary and fuzz tests for every provisioning message;
- wrong, missing, expired, replayed, and previous-window authentication material;
- one-session enforcement, attempt exhaustion, cooldown, and monotonic timeout;
- atomic promotion, corrupt pending record, failed connection, timeout, and simulated power loss;
- redacted logging with unique SSID, password, AP key, proof, and token canaries;
- factory-reset cancellation, completion, repeated reset, and interrupted reset.

### Hardware acceptance matrix

Run on the documented ESP32-S3-DevKitC-1 module/flash/PSRAM variants before merge of production firmware:

| Scenario | Hardware acceptance |
| --- | --- |
| First boot | AP and provisioning port stay off until the 5-second runtime gesture |
| Device-specific WPA2 | Two devices and two windows produce different passphrases; old/wrong values cannot associate |
| Endpoint protection | Associated client without a valid current Security 2 session cannot read status or submit credentials |
| Confidentiality | Over-the-air capture shows no plaintext target SSID/passphrase or reusable proof |
| Successful pairing | STA obtains an IP; provisioning server and AP disappear immediately |
| Timeout | At 10 minutes the server and AP close with no residual listener |
| Reboot | Reboot during every state leaves AP off and discards volatile/pending state |
| Failed connection | Invalid or unavailable target network preserves the last-known-good credential and does not auto-open AP |
| Rotation | New credentials replace old only after verified success; previous setup credential is invalid |
| Recovery | Physical gesture permits repair while preserving the active credential until promotion |
| Factory reset | Short/medium holds do not erase; confirmed long hold erases only RF-02 state and returns to AP-off first boot |
| Secret-free diagnostics | Full serial capture contains none of the canary secrets across success, failure, timeout, reboot, and reset |
| Storage protection | Plaintext canaries are absent from flash dump and encrypted NVS is active |
| Platform hardening | Secure Boot v2, Flash Encryption Release mode, and approved debug/download restrictions report enabled |
| Resource limits | Measured flash, heap, stack, and provisioning peak stay within an approved margin on every target variant |

Record board revision, module marking, firmware commit, PlatformIO platform version, test client version, timing results, memory measurements, packet capture reference, and pass/fail evidence. Do not attach captures or logs that contain secrets.

## Rollback and recovery for implementation rollout

- Deliver RF-02 separately from #36 so its firmware can be reverted independently before irreversible production hardening.
- Test the protected provisioning behavior on development hardware with recoverable security settings first.
- Do not burn production eFuses until the signed image, encrypted NVS layout, update path, and factory recovery procedure pass on disposable boards.
- Once production eFuses are applied, rollback means installing a previously signed compatible firmware through the approved update/recovery path; it does not mean disabling Secure Boot or Flash Encryption.
- A failed field rotation rolls back credentials transactionally to the last-known-good active record, not firmware.
- If the protected transport or storage cannot meet resource and recovery acceptance, stop rollout and return to design review. Do not restore the open AP/endpoints as a fallback.

## Review decisions required before implementation

Reviewers must explicitly approve or revise:

1. BOOT-button activation and factory-reset timings for the actual enclosure.
2. The 10-minute window, 10-attempt cap, and 60-second cooldown.
3. Protocomm Security 2 integration and client compatibility within the Arduino/PlatformIO project.
4. Transactional active/pending storage and the definition of last-known-good connectivity.
5. Encrypted NVS, Secure Boot v2, Flash Encryption Release mode, UART/JTAG restrictions, and the manufacturing/recovery key procedure.
6. The hardware variants and evidence required by the acceptance matrix.

Approval must be recorded in #48. Until then, the current firmware remains unchanged.

## Authoritative references

- [Espressif Wi-Fi Provisioning](https://docs.espressif.com/projects/esp-idf/en/v5.5.2/esp32/api-reference/provisioning/wifi_provisioning.html): protected provisioning schemes, SoftAP service key, lifecycle, and stop behavior.
- [Espressif Protocol Communication](https://docs.espressif.com/projects/esp-idf/en/v5.1.3/esp32s3/api-reference/provisioning/protocomm.html): Security 2 uses SRP6a and AES-GCM.
- [Espressif ESP32-S3 Random Number Generation](https://docs.espressif.com/projects/esp-idf/en/stable/esp32s3/api-reference/system/random.html): hardware RNG and entropy-source requirements.
- [Espressif ESP32-S3 Flash Encryption](https://docs.espressif.com/projects/esp-idf/en/release-v5.5/esp32s3/security/flash-encryption.html): Release mode, per-device keys, NVS considerations, and irreversible production implications.
- [Espressif ESP32-S3 Secure Boot v2](https://docs.espressif.com/projects/esp-idf/en/stable/esp32s3/security/secure-boot-v2.html): signed bootloader/application verification and key revocation.
- [Espressif ESP32-S3-DevKitC-1 User Guide](https://docs.espressif.com/projects/esp-idf/en/v4.4.3/esp32s3/hw-reference/esp32s3/user-guide-devkitc-1.html): BOOT button and download-mode behavior.
- [NISTIR 8259A IoT Device Cybersecurity Capability Core Baseline](https://www.nist.gov/publications/iot-device-cybersecurity-capability-core-baseline): device configuration, data protection, logical interface access, and cybersecurity state awareness.
- [NIST IoT Logical Access to Interfaces Catalog](https://pages.nist.gov/IoT-Device-Cybersecurity-Requirement-Catalogs/technical/logical/): authentication, out-of-band proof, and disabling unnecessary interfaces.
- [NIST IoT Data Protection Catalog](https://pages.nist.gov/IoT-Device-Cybersecurity-Requirement-Catalogs/technical/protection/): cryptographic protection, key management, rotation, and secure storage.

## Completion criteria

This design task is complete when:

- the plan is linked from #48 and reviewed;
- all six review decisions have an explicit disposition;
- RF-01's parser contract is stable enough to reference from RF-02 implementation;
- reviewers confirm that no #36 scope was absorbed;
- a later implementation task is authorized with the agreed hardware and security gates.

The plan remains active until #48 is complete, then it is deleted according to repository policy; Git history and the closed issue retain the decision record.
