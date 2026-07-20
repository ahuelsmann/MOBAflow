# RF-02 Protected ESP32 Provisioning Plan

## Document status

- Status: Proposed security design; PR #53 review findings incorporated; approval required
- Primary issue: [#48](https://github.com/ahuelsmann/MOBAflow/issues/48)
- Parent programme: [#47](https://github.com/ahuelsmann/MOBAflow/issues/47)
- Recommended implementation prerequisite: [RF-01 / #49](https://github.com/ahuelsmann/MOBAflow/issues/49)
- Follow-up baseline: `main` at `fa55bff7e7f90decf6b6c97e567158f74b5382d5`
- Scope owner: RF-02 only

This plan is the single issue-specific plan for #48. This revision authorizes threat modelling and security design only. It does not authorize production firmware changes.

Production implementation remains blocked until both conditions are true:

1. RF-01 defines and tests the length-safe parser contract that every network decoder can reuse.
2. The activation, credential, storage, recovery, and factory-reset decisions in this plan are reviewed and approved in #48.

## Outcome

Provisioning must be a physically initiated, authenticated, encrypted, time-limited maintenance state. After first enrollment, physical activation alone must not authorize configuration or reset. Normal operation must not expose a setup access point or credential-management endpoint. A failed station connection must not silently reopen provisioning.

The design protects Wi-Fi credentials against nearby radio attackers, clients that have not completed the current pairing proof, accidental diagnostic disclosure, stale setup credentials, interrupted rotation, and opportunistic access after reboot.

## Scope boundaries

In scope:

- first-boot and physical activation policy;
- a device-specific, per-window WPA2 credential;
- first-enrollment owner binding and owner-authorized maintenance;
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
| Target Wi-Fi configuration | Passphrase confidential in transit, at rest, responses, displays, and logs; SSID omitted from application logs but not treated as secret because 802.11 management frames expose it |
| Per-window setup credential | Unpredictable, device-specific, short-lived, never logged, invalid after close or reboot |
| Owner authorization key | Private key remains only with the owner's provisioning client; device stores only the public key and requires signed approval for post-enrollment configuration or reset |
| Provisioning authorization state | Cannot be entered remotely; expires deterministically |
| Last-known-good network configuration | Atomic replacement and recovery after failed rotation or power loss |
| Device identity | Stable enough to select the intended device, but not treated as a secret |
| Firmware and storage policy | Prevent unauthorized firmware or flash readout from bypassing credential controls in production |
| Audit events | Useful for lifecycle diagnosis without credentials, tokens, proofs, request bodies, or stable client identifiers |

## Trust boundaries and threat actors

Trust boundaries:

1. Physical operator to the BOOT button and local TFT.
2. Provisioning client to the temporary WPA2 SoftAP.
3. Authenticated provisioning session to the owner-authorization check.
4. Owner-authorized session to the credential parser and state machine.
5. Volatile session state to persistent NVS state.
6. Firmware and serial diagnostics to any attached development host.

Threat actors:

- a nearby unauthenticated Wi-Fi client;
- a client that can see the SoftAP but does not possess the current on-device proof;
- a previously authorized client holding an expired setup value;
- a local-network client scanning normal operational endpoints;
- an operator who enters invalid credentials or loses power during rotation;
- a person with temporary physical access to the button and display but not the owner's private key;
- a person attempting UART, debug, or casual flash access after deployment;
- malformed or oversized network input reaching a decoder.

First enrollment is an explicit trust-on-first-use boundary: before an owner key exists, the person who controls the device, button, and display can claim it. Deployment procedures must therefore preserve physical custody through first enrollment. After enrollment, button and display access alone must not authorize credential rotation, owner-key replacement, or factory reset.

Invasive chip decapsulation and laboratory side-channel attacks are not acceptance targets. Casual flash readout, UART download access, signed downgrade to a vulnerable image, unsigned firmware replacement, and exposed debug interfaces are production threats and are addressed by the hardening gate below.

## Threat model

| ID | Threat | Existing weakness | Required control | Verification |
| --- | --- | --- | --- | --- |
| T1 | Unauthorized association | Setup AP is open | Random per-window WPA2-CCMP passphrase shown only on the TFT | Wrong and previous passphrases cannot associate |
| T2 | Remote provisioning activation | AP opens automatically after failures and remains active | Runtime physical hold is the only activation path; first boot waits for that gesture | Boot and station failure leave AP and server off |
| T3 | Unauthorized configuration read or write | Endpoints accept anonymous HTTP | Authenticated encrypted provisioning protocol, enrolled owner signature for sensitive operations, and no credential read endpoint | Missing, invalid, expired, and replayed session or owner proofs are rejected |
| T4 | Credential interception or modification | Plain HTTP carries target credentials | Application-layer authenticated encryption in addition to WPA2 | Provisioning payload capture contains no plaintext target passphrase or reusable proof; ordinary SSID management frames are explicitly excluded |
| T5 | Stale credential reuse | Setup access has no lifetime or rotation | Generate a new setup credential for every activation; erase it on close and reboot | Prior-window values fail after success, timeout, cancel, and reboot |
| T6 | Persistent attack surface | HTTP server runs in normal mode | Start server only inside the provisioning state; stop server and SoftAP on every exit | Port scan in operational mode finds no provisioning service |
| T7 | Loss of working configuration | New credentials are committed before validation | Pending and active slots; promote only after authenticated LAN handover; reconnect the active network after failure | Failed rotation and power loss restore verified last-known-good connectivity |
| T8 | Recovery becomes an open backdoor | Failed station connection automatically starts setup | Show offline status and require a fresh physical gesture | Reboot and repeated connection failures never open AP |
| T9 | Secret disclosure through diagnostics | Future handlers may log inputs or proofs | Allowlisted event schema and secret canary tests | Serial and persistent logs contain none of the canary values |
| T10 | Storage extraction or firmware bypass | Plain Preferences storage and no stated production hardening | Encrypted NVS, Flash Encryption Release mode, Secure Boot v2, and restricted debug/download interfaces | Production hardware reports expected security state; flash dump lacks plaintext canaries |
| T11 | Parser memory-safety failure | Network parsing currently mixes fixed buffers and implicit string assumptions | Reuse the reviewed RF-01 pointer-plus-length parser boundary and explicit maximums | Boundary, malformed-input, and fuzz tests pass before hardware rollout |
| T12 | Denial of service or brute force | No attempt or session limits | Single active session, bounded attempts, fixed window, cooldown, and deterministic close | Attempt-limit and timeout tests close the service |
| T13 | Temporary physical takeover | Button and displayed proof alone authorize all maintenance | First enrollment binds an owner public key; later writes, key rotation, and reset require a current owner signature plus the physical gesture | A person with button/display access but no owner private key cannot change or erase configuration |
| T14 | Signed firmware downgrade | Secure Boot accepts older images signed by a still-trusted key | Hardware anti-rollback floor tied to monotonically increasing application `secure_version` | A signed image below the burned security version is rejected |

## Security design decisions

### 1. Physical activation and first boot

- The AP is off by default in every boot state.
- An unprovisioned first boot shows `Hold BOOT for setup` on the TFT. It does not start the AP merely because credentials are absent.
- The ESP32-S3-DevKitC-1 BOOT button is read as an application input only after normal boot. Holding it during reset enters the ROM download path and is not an RF-02 gesture.
- Holding BOOT continuously for 5 seconds and releasing before 12 seconds opens one provisioning window. Debounce and duration use wrap-safe monotonic time.
- On unowned first boot, the authenticated first provisioning session must enroll exactly one owner public key before Wi-Fi credentials can become active. This trust-on-first-use event is displayed prominently and logged without key material.
- After owner enrollment, the gesture enables only the temporary radio and Security 2 session. Credential writes, owner-key rotation, and factory reset additionally require a signature from the enrolled owner key bound to the current device, session, nonce, requested action, and request digest.
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

### 3. Owner binding and recovery

- The provisioning client generates a P-256 owner key pair during first enrollment. The private key remains in the client's protected credential store; the device persists only the public key, key identifier, algorithm/version, and enrollment generation.
- First enrollment is allowed only while the device has no valid owner record and the first-boot physical gesture plus current Security 2 session are present.
- Every later sensitive request carries a fresh device nonce and an owner signature over a canonical, length-bounded transcript containing protocol version, device identifier, session identifier, nonce, action, and request digest.
- Nonces are single-use and expire with the provisioning window. A signature for one device, session, or action cannot authorize another.
- Owner-key rotation requires the current owner signature and the physical gesture. The replacement public key is committed transactionally and acknowledged before the old key is removed.
- Factory reset requires both the long physical gesture and a current owner-signed reset authorization. Button/display access alone is insufficient after enrollment.
- Loss of the owner private key is not recovered through the network or TFT. Recovery requires physical service access and an approved signed recovery image or manufacturing procedure that erases RF-02 credentials and owner binding. Secure Boot, anti-rollback, and disabled debug/download policy remain enforced; recovery never reveals old secrets.

Temporary physical access can still cause bounded availability disruption by opening a window or power cycling the device. It cannot claim an already owned device, change credentials, rotate ownership, or confirm factory reset.

### 4. Provisioning transport and endpoint protection

The preferred implementation is Espressif Protocomm Security 2 over SoftAP: SRP6a authentication with AES-GCM protected provisioning messages. The per-window setup secret supplies the password material used to construct the Security 2 salt and verifier. The SRP username is the fixed public 24-byte ASCII value `mobaflow-provisioning-v1`, without a trailing NUL. The client obtains this constant from protocol version 1 rather than a secret channel. The implementation review must confirm Arduino/PlatformIO integration and memory use before production code is approved.

Interoperability tests must pin the Security 2/SRP group, exact username bytes and length, password encoding, salt length, verifier encoding, and proof/AES-GCM transcript. Public deterministic test credentials produce committed firmware/client golden vectors; generated production credentials never appear in fixtures or logs.

Security 0, Basic authentication over plain HTTP, a bearer token sent over unprotected HTTP, and WPA2-only endpoint authorization are rejected designs.

The provisioning surface has these rules:

- no endpoint can return stored SSIDs, passwords, setup secrets, proofs, verifier material, or session keys;
- status responses expose only the provisioning state, remaining window time, attempt count, and coarse result code;
- credential write accepts one bounded message with explicit SSID and passphrase lengths;
- owner enrollment, owner-key rotation, credential write, and factory reset each have distinct signed action identifiers and cannot reuse one another's authorization;
- all message decoding uses the RF-01 pointer-plus-length contract, rejects embedded truncation assumptions, and applies fixed maximums before copying;
- one authenticated session is allowed at a time;
- authentication failures are capped at 10 per window, then the service closes and enforces a 60-second local cooldown;
- replayed, duplicate, expired, or out-of-state requests are rejected or handled idempotently according to the reviewed protocol contract;
- normal operational mode does not register or listen on the provisioning port.

If Protocomm Security 2 cannot be integrated within measured flash, RAM, and framework constraints, implementation stops for a revised design review. It must not fall back silently to a custom cryptographic protocol or plaintext HTTP.

### 5. Provisioning window, handover, and closure

- A window lasts 10 minutes from physical activation, measured with monotonic time.
- The countdown does not extend when a client connects, authenticates, submits invalid credentials, or causes a station failure.
- Association and IP acquisition create only a pending candidate. The credential is not active until the authenticated LAN handover defined below succeeds.
- After a successful handover and atomic promotion, the device stops accepting new operations, sends the authenticated terminal success response, and waits until the response is flushed or for a maximum 2-second drain interval. It then deinitializes the provisioning server and SoftAP; total post-promotion exposure must remain below 5 seconds.
- It also closes on timeout, authentication-attempt exhaustion, explicit local cancel, factory-reset completion, or reboot.
- Exit order after terminal-response delivery is: invalidate the authenticated session, stop/deinitialize the provisioning server, disconnect and erase the SoftAP configuration, select STA or OFF mode, clear volatile secrets, update the TFT, then emit the redacted close event.
- A watchdog or error path must converge on the same close routine. There is no alternate exit that leaves AP+STA active.

### 6. Credential storage, LAN handover, rotation, and rollback

Persistent state uses versioned `active` and `pending` records with integrity validation:

1. Receive new credentials only inside an authenticated session.
2. Validate lengths and reject unsupported security modes without logging submitted values.
3. Write a pending record without replacing the active record.
4. Attempt the pending station connection within the remaining window while retaining the active record.
5. Treat the pending network as usable only after all checks pass: association to the submitted network, DHCP address and route acquisition, default-gateway reachability, and an authenticated one-time handover confirmation received through a temporary provisioning-only endpoint on the STA address.
6. Bind the handover confirmation to the Security 2 session using a fresh 128-bit nonce and a session-derived authenticator. The client switches to the target LAN and confirms reachability within 60 seconds. Internet access is neither required nor tested.
7. After the handover succeeds, atomically promote pending to active, retain the previous record until the terminal success response is drained, and then remove the previous record.
8. On failed association, DHCP, gateway, handover, timeout, reboot, or corrupt pending data, erase pending, reconnect using the active record, and verify association, DHCP, and gateway reachability before closing the window. Failure to restore connectivity is an explicit offline error; it never promotes pending or opens an unauthenticated path.

There is no credential read API. Rotation is a new physically activated provisioning window, not a remote edit during normal operation. Every rotation also rotates the temporary AP passphrase and application-layer session material.

The temporary STA handover endpoint exists only inside the current provisioning window, accepts only the one session-bound confirmation, exposes no general status or #36 capability surface, and closes through the common shutdown path.

Production storage requires encrypted NVS with a protected NVS key partition. Flash Encryption must use Release mode with a unique per-device key, Secure Boot v2 must verify firmware, hardware anti-rollback must reject application images whose `secure_version` is below the eFuse floor, and unused debug/UART download access must be restricted according to the approved manufacturing and recovery procedure. These irreversible eFuse choices require a separate hardware rehearsal and sign-off before production enablement.

### 7. Recovery and deterministic reboot behavior

| Starting state | Event | Required next state |
| --- | --- | --- |
| Unprovisioned | Normal boot | AP off; TFT requests physical activation |
| Unprovisioned | 5-second gesture | Fresh 10-minute provisioning window; first successful session must bind an owner key |
| Provisioned | Normal boot and connect succeeds | STA only; provisioning server absent |
| Provisioned | Normal boot and connect fails | STA retry/offline state; AP off; physical recovery prompt |
| Provisioned | 5-second gesture | Fresh window; active credentials retained; sensitive operations still require the owner signature |
| Window active | Invalid target credentials or failed handover | Pending cleared; reconnect and verify active network; window continues only if time/attempt budget remains |
| Window active | Successful target handover | Promote pending; send terminal response; drain for at most 2 seconds; close server and AP; STA only |
| Window active | Reboot or power loss | Volatile session lost; pending discarded on boot; AP remains off |
| Window active | Timeout or attempt exhaustion | Pending discarded; active network reconnected and verified; server and AP stop |

Recovery never weakens authentication and never automatically reopens provisioning.

### 8. Factory reset

- Holding BOOT for at least 12 seconds during normal application runtime enters a destructive reset confirmation screen.
- The display counts down a further 5 seconds; releasing before completion cancels. Continuing to hold confirms.
- On an owned device, the countdown can complete only when the current provisioning session already holds a valid owner-signed reset authorization. Without it, the display reports `Owner approval required`, records a redacted rejection, and leaves all state intact.
- Confirmation erases Wi-Fi active/pending credentials, the owner public-key binding, provisioning/session metadata, and provisioning audit counters, then reboots to the unprovisioned AP-off state.
- It does not erase or redefine display capability, frame transport, or other #36-owned configuration.
- The reset operation emits only start, cancel, success, or failure event codes. It never prints erased data.
- Reset behavior must be idempotent and recover safely if power is lost during erasure.

The gesture timings and screen copy are review decisions and must be verified on the actual enclosure so accidental activation is acceptably unlikely.

### 9. Secret-free logs and diagnostics

Security events use a fixed allowlist rather than interpolating request data:

- `provisioning_activation_accepted`
- `provisioning_activation_rejected`
- `provisioning_session_authenticated`
- `provisioning_authentication_failed`
- `provisioning_owner_enrolled`
- `provisioning_owner_authorized`
- `provisioning_owner_authorization_failed`
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
3. Integrate the reviewed protected provisioning transport, SRP golden vectors, owner enrollment/signatures, and single-session policy.
4. Add transactional active/pending encrypted storage, authenticated STA handover, active-network restoration, rotation, and rollback.
5. Add deterministic close, recovery, factory reset, and allowlisted audit events.
6. Add PlatformIO build coverage, host-native negative tests, and the hardware acceptance suite.
7. Rehearse Secure Boot, anti-rollback, Flash Encryption Release mode, encrypted NVS, recovery, and manufacturing key handling on disposable hardware before production enablement.

Each behavioral slice requires tests before commit. Production firmware must not combine RF-02 with #36 refactoring.

## Validation strategy

### Design-only validation for this revision

- exactly one `plans/48-*.md` exists;
- issue #48 links this file and this file links #48, #47, and #49;
- the diff contains no production firmware, host, API, or #36 changes;
- Markdown links and headings are valid;
- `git diff --check` passes;
- the plan explicitly covers activation, first boot, device-specific WPA2, owner authorization, Security 2 interoperability, timeout, endpoint protection, handover, rotation, recovery, factory reset, logs, anti-rollback, and hardware acceptance.

No .NET or PlatformIO behavior changed in this revision, so build and runtime tests provide no additional signal. The later implementation must run the repository-mandated build and test gates.

### Host-native tests required for implementation

- state-machine transitions for every row in the recovery table;
- boundary and fuzz tests for every provisioning message;
- wrong, missing, expired, replayed, and previous-window authentication material;
- one-session enforcement, attempt exhaustion, cooldown, and monotonic timeout;
- owner enrollment, wrong/missing/replayed/cross-device/cross-action signatures, owner-key rotation, and lost-key service recovery;
- atomic promotion, handover nonce/authenticator, terminal-response drain, corrupt pending record, failed connection, active-network reconnection, timeout, and simulated power loss;
- redacted logging with unique SSID, password, AP key, proof, and token canaries;
- factory-reset cancellation, completion, repeated reset, and interrupted reset.

### Hardware acceptance matrix

Run on the documented ESP32-S3-DevKitC-1 module/flash/PSRAM variants before merge of production firmware:

| Scenario | Hardware acceptance |
| --- | --- |
| First boot | AP and provisioning port stay off until the 5-second runtime gesture |
| Device-specific WPA2 | Two devices and two windows produce different passphrases; old/wrong values cannot associate |
| Endpoint protection | Associated client without a valid current Security 2 session cannot read status or submit credentials |
| Owner authorization | After first enrollment, button/display access without the owner private key cannot write credentials, rotate ownership, or complete factory reset |
| Security 2 interoperability | Firmware and reference client reproduce the committed public SRP username/salt/verifier/proof vectors byte-for-byte |
| Confidentiality | Provisioning application payloads reveal no target passphrase, owner secret, session key, or reusable proof; test evidence notes that target SSIDs remain visible in ordinary 802.11 management frames |
| Successful pairing | Association, DHCP, gateway, and authenticated STA handover pass; terminal success reaches the client before server/AP teardown within the 5-second bound |
| Timeout | At 10 minutes the server and AP close with no residual listener |
| Reboot | Reboot during every state leaves AP off and discards volatile/pending state |
| Failed connection | Invalid/unavailable target or failed handover clears pending, reconnects and verifies the last-known-good network, and does not auto-open AP |
| Rotation | New credentials replace old only after verified handover; old network is restored on failure; previous setup credential is invalid |
| Recovery | Physical gesture permits repair while preserving the active credential until promotion |
| Lost owner key | Network/TFT recovery is rejected; the approved signed service-recovery procedure erases RF-02 state without disclosing old secrets or lowering the anti-rollback floor |
| Factory reset | Short/medium holds and an unsigned long hold do not erase; owner-authorized confirmed long hold erases only RF-02 state and returns to AP-off first boot |
| Secret-free diagnostics | Full serial capture contains none of the canary secrets across success, failure, timeout, reboot, and reset |
| Storage protection | Plaintext canaries are absent from flash dump and encrypted NVS is active |
| Platform hardening | Secure Boot v2, Flash Encryption Release mode, and approved debug/download restrictions report enabled |
| Anti-rollback | A correctly signed image below the burned application `secure_version` floor is rejected; an allowed recovery image meets or exceeds the floor |
| Resource limits | Measured flash, heap, stack, and provisioning peak stay within an approved margin on every target variant |

Record board revision, module marking, firmware commit, PlatformIO platform version, test client version, timing results, memory measurements, packet capture reference, and pass/fail evidence. Do not attach captures or logs that contain secrets.

## Rollback and recovery for implementation rollout

- Deliver RF-02 separately from #36 so its firmware can be reverted independently before irreversible production hardening.
- Test the protected provisioning behavior on development hardware with recoverable security settings first.
- Do not burn production eFuses until the signed image, encrypted NVS layout, update path, and factory recovery procedure pass on disposable boards.
- Enable ESP-IDF application anti-rollback for production and assign a monotonically increasing `secure_version` to security releases. Advance the eFuse floor only after the running image passes its health checks; account for the finite eFuse field in the release policy.
- Once production eFuses are applied, firmware rollback means installing a previously signed, compatible image whose `secure_version` is not below the eFuse floor. A merely signed vulnerable image is not an allowed rollback target, and rollback never disables Secure Boot, Flash Encryption, or anti-rollback.
- A failed field rotation rolls back credentials transactionally to the last-known-good active record, not firmware.
- If the protected transport or storage cannot meet resource and recovery acceptance, stop rollout and return to design review. Do not restore the open AP/endpoints as a fallback.

## Review decisions required before implementation

Reviewers must explicitly approve or revise:

1. BOOT-button timings, first-boot trust-on-first-use custody, owner-key enrollment/rotation, and lost-key service recovery for the actual enclosure and client.
2. The 10-minute window, 10-attempt cap, and 60-second cooldown.
3. Protocomm Security 2 integration, fixed SRP username, golden vectors, and client compatibility within the Arduino/PlatformIO project.
4. Transactional active/pending storage, authenticated STA handover, terminal-response drain, and active-network restoration.
5. Encrypted NVS, Secure Boot v2, Flash Encryption Release mode, hardware anti-rollback floor, UART/JTAG restrictions, and the manufacturing/recovery key procedure.
6. The hardware variants, packet-capture scope, and evidence required by the acceptance matrix.

Approval must be recorded in #48. Until then, the current firmware remains unchanged.

## Authoritative references

- [Espressif Wi-Fi Provisioning](https://docs.espressif.com/projects/esp-idf/en/v5.5.2/esp32/api-reference/provisioning/wifi_provisioning.html): protected provisioning schemes, SoftAP service key, lifecycle, and stop behavior.
- [Espressif Protocol Communication](https://docs.espressif.com/projects/esp-idf/en/v5.1.3/esp32s3/api-reference/provisioning/protocomm.html): Security 2 uses SRP6a and AES-GCM.
- [Espressif ESP32-S3 Random Number Generation](https://docs.espressif.com/projects/esp-idf/en/stable/esp32s3/api-reference/system/random.html): hardware RNG and entropy-source requirements.
- [Espressif ESP32-S3 Flash Encryption](https://docs.espressif.com/projects/esp-idf/en/release-v5.5/esp32s3/security/flash-encryption.html): Release mode, per-device keys, NVS considerations, and irreversible production implications.
- [Espressif ESP32-S3 Secure Boot v2](https://docs.espressif.com/projects/esp-idf/en/stable/esp32s3/security/secure-boot-v2.html): signed bootloader/application verification and key revocation.
- [Espressif ESP32-S3 OTA Updates](https://docs.espressif.com/projects/esp-idf/en/v5.5/esp32s3/api-reference/system/ota.html): application `secure_version`, validation, rollback, and anti-rollback behavior.
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
