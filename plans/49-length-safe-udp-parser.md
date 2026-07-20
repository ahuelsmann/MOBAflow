# Length-Safe ESP32 UDP Parser Implementation Plan

## Document status

- Status: Ready
- Primary issue: https://github.com/ahuelsmann/MOBAflow/issues/49
- Parent programme: https://github.com/ahuelsmann/MOBAflow/issues/47
- Status and acceptance criteria source: GitHub issue #49

## Purpose

This plan defines the one-to-one implementation boundary for RF-01: make the MOBAdisplay ESP32 UDP parser explicitly length-bounded while preserving the existing frame-start, frame-data, host-version metadata, and completion behavior. GitHub issue #49 owns scope and acceptance criteria; this document owns parser seams, deterministic outcomes, test coverage, memory constraints, risks, and validation commands.

## Scope boundaries

In scope:

- extract Arduino-independent packet classification and host-version metadata parsing;
- pass both copied-buffer length and original UDP datagram length into the parser;
- reject datagrams that exceed the fixed receive buffer and drain their unread remainder;
- replace implicit null-terminated host-version construction with an explicit bounded copy;
- preserve exact `FRAME_START`, `FRAME_DONE`, 480-byte legacy-row, 482-byte indexed-row, and `HOST_VER:` behavior;
- add host-native boundary and deterministic fuzz tests;
- build and test through PlatformIO.

Out of scope:

- RF-02 provisioning authentication or Wi-Fi credential lifecycle from issue #48;
- `DISPLAY_META`, shared display capabilities, or any feature work from issue #36;
- board/display-driver changes, sender protocol extensions, or unrelated firmware refactoring.

## Current risk

`main.cpp` currently classifies packets inline after copying at most 768 bytes. The host-version path constructs an Arduino `String` from `buffer + prefixLength` without supplying the received length, so it assumes a null terminator outside the UDP payload. A datagram larger than the fixed buffer can also leave unread bytes in `WiFiUDP`, preventing the next packet from being parsed until the remainder is consumed.

## Parser seam

Add a private, portable PlatformIO component under `MOBAdisplay/esp32/lib/MobaUdpPacketParser/` with no Arduino, Wi-Fi, heap, exception, or display dependencies.

The parser accepts:

- `const uint8_t* buffer`;
- `size_t copiedLength`, the bytes actually available in `buffer`;
- `size_t datagramLength`, the original size returned by `WiFiUDP::parsePacket()`.

It returns a small value object containing:

- a packet-kind enum;
- an indexed-row value when applicable;
- a non-owning host-version payload pointer and explicit payload length when applicable.

The parser never allocates, never scans beyond `copiedLength`, and never dereferences the buffer for empty, null, incomplete, or oversized inputs. Protocol constants used by both firmware integration and tests live with the parser so fixed-width assumptions have one source of truth.

## Deterministic classification

| Input | Result | Firmware action |
| --- | --- | --- |
| Zero-length datagram | `Empty` | Ignore |
| Non-zero length with null buffer | `Malformed` | Ignore |
| `copiedLength < datagramLength` within the supported maximum | `Truncated` | Ignore |
| Datagram larger than the 768-byte receive capacity | `Oversized` | Drain unread bytes and ignore |
| Proper prefix fragment shorter than a supported control prefix | `Truncated` | Ignore |
| Exact `FRAME_START` | `FrameStart` | Reset capture |
| Exact `FRAME_DONE` | `FrameDone` | Present and reset capture |
| Exact `HOST_VER:` without payload | `Malformed` | Ignore |
| `HOST_VER:` plus printable ASCII payload | `HostVersion` | Copy at most 28 payload bytes, terminate locally, update host version |
| `HOST_VER:` payload containing control/non-ASCII bytes | `Malformed` | Ignore |
| Exactly 480 bytes | `LegacyLine` | Preserve sequential-row behavior |
| Exactly 482 bytes and row 0 through 279 | `IndexedLine` | Preserve indexed-row behavior |
| Exactly 482 bytes with row outside 0 through 279 | `Malformed` | Ignore |
| Any other complete packet, including `DISPLAY_META` | `Unknown` | Ignore |

Control and metadata classification remains ahead of row-length classification, matching current behavior for a host-version datagram whose total size happens to equal a row packet size.

## Test harness

Add a PlatformIO `native` environment and a Unity test suite that includes only the portable parser component. PlatformIO's private-library layout keeps the production parser shared without compiling Arduino `setup()` or `loop()` into the native test executable.

Boundary tests cover:

- lengths immediately below, equal to, and immediately above every supported control prefix;
- empty and partial `HOST_VER:` prefixes, one-byte payloads, the 28-byte display limit, longer compatible payloads, and malformed payload bytes;
- lengths 479, 480, 481, 482, and 483;
- indexed rows 0, 279, 280, and 65535;
- copied/datagram length mismatches;
- datagram lengths 767, 768, and 769;
- null buffers and unknown packets;
- classification precedence where metadata length equals a row-packet length.

A deterministic seeded fuzz/property test generates arbitrary byte sequences and length combinations across empty, supported, truncated, and oversized ranges. It asserts result invariants such as payload bounds and valid indexed rows. The native test runs with AddressSanitizer and UndefinedBehaviorSanitizer where supported so any out-of-bounds or invalid memory access fails validation.

## Memory constraints

- Keep the existing 768-byte global UDP receive buffer; do not grow it.
- Use no heap allocation in classification or metadata parsing.
- Return metadata as a non-owning view into the receive buffer.
- Use one 29-byte stack buffer in firmware integration for the displayed 28-character host version plus terminator.
- Do not duplicate frame payloads or row buffers.

## Affected files

- `MOBAdisplay/esp32/lib/MobaUdpPacketParser/src/UdpPacketParser.h` (new)
- `MOBAdisplay/esp32/lib/MobaUdpPacketParser/src/UdpPacketParser.cpp` (new)
- `MOBAdisplay/esp32/src/main.cpp`
- `MOBAdisplay/esp32/platformio.ini`
- `MOBAdisplay/esp32/test/test_udp_packet_parser/test_main.cpp` (new)
- `MOBAdisplay/docs/protocol.md`
- `plans/49-length-safe-udp-parser.md` (deleted after completion, before the final delivery commit)

No .NET application, RF-02, or issue #36 files are changed.

## Delivery sequence

1. Commit and push this Draft plan on the issue-specific branch.
2. Link the pushed plan from issue #49, then set the plan and issue/project status to In progress.
3. Add the portable parser and boundary/fuzz tests.
4. Integrate parser results into the UDP loop with explicit lengths and bounded metadata copying.
5. Validate native tests, sanitizer execution, ESP32 firmware compilation, repository tests, and the final diff.
6. Record validation evidence in issue #49, delete this completed standalone plan, push the final branch, and open an unmerged PR linked to #49.

## Validation commands

Run from the repository root:

```powershell
python -m platformio test -d MOBAdisplay/esp32 -e native
python -m platformio run -d MOBAdisplay/esp32 -e esp32s3
dotnet test Test/Test.csproj
git diff --check
```

If the Windows host has no native GCC toolchain, install or expose a supported GCC toolchain before executing the native environment; do not downgrade the test gate to an uninstrumented ad-hoc parser test.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Parser reads a prefix or row index before proving enough bytes exist | Centralize ordered length checks and exercise every boundary under sanitizers |
| Oversized UDP packet leaves unread bytes and stalls later reception | Drain `WiFiUDP::available()` after the bounded read and classify using original datagram length |
| Refactoring changes packet precedence or frame behavior | Encode current precedence and all supported packet kinds in compatibility tests |
| Host version loses compatibility | Accept printable payloads longer than 28 bytes but preserve the existing 28-character display truncation |
| Native tests accidentally depend on Arduino | Keep the component limited to standard fixed-width integer and size types |
| RF-01 absorbs display capability work | Keep `DISPLAY_META` deterministically `Unknown`; make no sender or #36 changes |

## Rollback strategy

The change is isolated to one portable parser component, its tests, and the firmware dispatch call site. Reverting the RF-01 commits restores the previous inline classifier without changing persisted state, protocol payloads, Wi-Fi configuration, or display drivers.

## Documentation and completion

- Keep acceptance evidence and PR linkage in issue #49.
- Update the current protocol document with deterministic parser outcomes and validation commands.
- Delete this plan once implementation and validation are complete; the linked issue, PR, and Git history retain the implementation record.
