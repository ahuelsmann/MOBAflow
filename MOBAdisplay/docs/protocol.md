# MOBAdisplay ESP32 Display Protocol

## Status and scope

This document defines the versioned UDP contract between a MOBAflow host and an
ESP32 display endpoint. Version 1.0 is the first normative binary protocol. The
keywords MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY describe interoperability
requirements.

The protocol is independent of the display controller, graphics library, board
pins, DMA configuration, Wi-Fi provisioning, and credential storage. Those
details remain owned by each firmware project.

The current line-oriented `HOST_VER`, `FRAME_START`, indexed-row, and
`FRAME_DONE` transport is legacy protocol v0. The old documentation stated that
the host sent `DISPLAY_META`, but `UdpLineFrameSender` does not send that
datagram. Implementations MUST NOT infer v1 capabilities from that obsolete
description. There is no silent fallback from a negotiated v1 session to v0.

Firmware integration of this specification remains gated by RF-01 (length-safe
UDP parsing) and RF-02 (stable provisioning boundary). The host envelope and
golden vectors can be implemented and tested independently of those gates.

## Transport limits

- UDP is used on the explicitly configured endpoint; the default device port is
  `4210`.
- Every datagram contains exactly one envelope and its declared payload. A
  receiver MUST reject truncated datagrams and datagrams with trailing bytes.
- The default maximum UDP payload is 1232 bytes: 1280-byte IPv6 minimum MTU
  minus the 40-byte IPv6 header and 8-byte UDP header. The 32-byte v1 envelope
  therefore leaves a default application payload of 1200 bytes.
- A device MAY advertise a lower maximum. A host MUST use the lower of its own
  limit and the negotiated device limit.
- A v1 payload length is an unsigned 16-bit value, but implementations SHOULD
  stay at or below the negotiated non-fragmenting limit. They MUST NOT rely on
  IP fragmentation for normal operation.
- Receivers MUST tolerate UDP loss, duplication, and reordering according to
  the transaction rules below. Packet identifiers do not provide transport
  authentication.

These limits follow the UDP usage guidance in RFC 8085 and the IPv6 minimum MTU
defined by RFC 8200. UDP itself is defined by RFC 768.

## Scalar encoding

- All unsigned 16-bit and 32-bit integers use network byte order (big-endian).
- Boolean values are one byte: `0` is false and `1` is true. Other values are
  invalid.
- Reserved bytes and reserved flag bits MUST be zero when sent and MUST cause
  rejection when received unless a later negotiated minor version defines them.
- Strings are UTF-8 with a one-byte byte length followed by exactly that many
  bytes. They are not null-terminated. Identity and version strings MUST NOT
  contain credentials or provisioning secrets.
- RGB565 pixels are transmitted in row-major order, high byte first, with red in
  bits 15-11, green in bits 10-5, and blue in bits 4-0.

## Fixed envelope

Every v1 datagram starts with this 32-byte header:

| Offset | Size | Field | Requirement |
| ---: | ---: | --- | --- |
| 0 | 4 | Magic | ASCII `MOBA`, hexadecimal `4D 4F 42 41` |
| 4 | 1 | Protocol major | `1` for this specification; zero is invalid |
| 5 | 1 | Protocol minor | `0` for this specification |
| 6 | 1 | Message type | One value from the message catalog |
| 7 | 1 | Flags | Bit mask defined below |
| 8 | 2 | Header length | `32`; other values are rejected by v1.0 |
| 10 | 2 | Payload length | Exact bytes following the header |
| 12 | 4 | Request ID | Non-zero caller-generated correlation ID |
| 16 | 4 | Frame ID | Non-zero for frame transactions; otherwise zero |
| 20 | 4 | Session ID | Zero before negotiation; assigned by the device afterward |
| 24 | 2 | Packet index | Zero-based index in a logical packet sequence |
| 26 | 2 | Packet count | Total packets; at least one and greater than packet index |
| 28 | 4 | Payload CRC32 | IEEE CRC32 of payload bytes; empty payload uses `00000000` |

CRC32 uses reflected polynomial `EDB88320`, initial value `FFFFFFFF`, and final
XOR `FFFFFFFF`. It detects accidental corruption but is not a security or
authentication mechanism.

### Flags

| Bit | Name | Meaning |
| ---: | --- | --- |
| 0 | Response | Datagram is a response to the matching request ID |
| 1 | Acknowledgement required | Sender requires a structured `Result` |
| 2 | Retry | Sender is retransmitting an identical logical operation |
| 3 | Final packet | Last packet in a logical packet sequence |
| 4-7 | Reserved | MUST be zero in v1.0 |

The `Retry` flag does not permit different payload bytes under the same request
ID. A receiver MUST return `Invalid` when a reused request ID changes the
operation or payload.

## Message catalog

| Value | Name | Direction | Response |
| ---: | --- | --- | --- |
| `01` | HelloRequest | Host to device | `CapabilitiesResponse` |
| `02` | CapabilitiesResponse | Device to host | None |
| `03` | HealthRequest | Host to device | `HealthResponse` |
| `04` | HealthResponse | Device to host | None |
| `10` | BeginFrame | Host to device | `Result` |
| `11` | FrameRegion | Host to device | `Result` only when requested or rejected |
| `12` | CompleteFrame | Host to device | `Result` |
| `13` | AbortFrame | Host to device | `Result` |
| `20` | Clear | Host to device | `Result` |
| `21` | SetBrightness | Host to device | `Result` |
| `22` | RenderTestPattern | Host to device | `Result` |
| `7F` | Result | Either direction | None |

Unknown message types are rejected. Responses copy the request ID of the
request and set the `Response` flag. Frame-related responses also copy the frame
ID. A device MUST NOT create a session or execute a command for an incompatible
major version.

## Payload definitions

### HelloRequest (`01`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Minimum supported major |
| 1 | 1 | Minimum supported minor for that major |
| 2 | 1 | Maximum supported major |
| 3 | 1 | Maximum supported minor for that major |
| 4 | 2 | Host maximum UDP datagram length |
| 6 | 2 | Reserved, zero |

The request uses session ID zero. The device selects the highest mutually
supported minor version within a common major. Version 1.0 supports one major
range only; a range spanning multiple major versions is invalid. If no major is
shared, the device returns `UnsupportedVersion` in a v1 envelope when it can
safely parse one, otherwise it drops the datagram.

### CapabilitiesResponse (`02`)

The fixed portion is followed by three length-prefixed UTF-8 strings.

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Selected protocol major |
| 1 | 1 | Selected protocol minor |
| 2 | 2 | Display width in pixels |
| 4 | 2 | Display height in pixels |
| 6 | 2 | Maximum UDP datagram length |
| 8 | 2 | Maximum region payload bytes |
| 10 | 2 | Pixel-format mask |
| 12 | 1 | Rotation mask |
| 13 | 1 | Optional-command mask |
| 14 | 1 | Frame capability flags |
| 15 | 1 | Acknowledgement mode |
| 16 | 4 | Newly assigned non-zero session ID |
| 20 | 1 + n | Device identity UTF-8 string |
| 21 + n | 1 + m | Firmware version UTF-8 string |
| 22 + n + m | 1 + p | Adapter identity UTF-8 string |

The device identity is stable enough for diagnostics but is not an
authentication credential. The adapter identity describes the logical display
backend, for example `tft-espi-st7789`; it MUST NOT expose pin or credential
configuration.

Pixel-format mask bit 0 is RGB565 big-endian. Rotation bits 0-3 represent 0,
90, 180, and 270 degrees. Optional-command bits 0-2 represent clear,
brightness, and built-in test pattern. Frame capability bit 0 means full-frame
staging, bit 1 means region transfer, and bit 2 guarantees atomic presentation.
Acknowledgement mode `0` means control and completion only; v1.0 defines no
other mode.

The maximum region payload counts only pixel bytes, not the 16-byte
`FrameRegion` metadata. It MUST be greater than zero and small enough that the
complete region datagram does not exceed the advertised datagram length.

### HealthRequest (`03`)

The payload is empty.

### HealthResponse (`04`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Health state |
| 1 | 1 | Last result code |
| 2 | 2 | Reserved, zero |
| 4 | 4 | Uptime seconds |
| 8 | 4 | Free heap bytes |
| 12 | 4 | Accepted-frame count |
| 16 | 4 | Rejected-frame count |
| 20 | 4 | Last completed frame ID, or zero |

Health states are `0` ready, `1` busy, and `2` degraded. Health data is
diagnostic and MUST NOT include SSIDs, passwords, tokens, packet payloads, or
other provisioning data.

### BeginFrame (`10`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 2 | Frame width |
| 2 | 2 | Frame height |
| 4 | 1 | Pixel format; `1` is RGB565 big-endian |
| 5 | 1 | Rotation; `0`, `1`, `2`, `3` mean 0, 90, 180, 270 degrees |
| 6 | 2 | Reserved, zero |
| 8 | 4 | Expected pixel-byte count |
| 12 | 4 | CRC32 of the complete pixel-byte stream |

The envelope frame ID and session ID MUST be non-zero. The expected byte count
MUST equal `width * height * 2` for RGB565 and MUST fit the negotiated device
limits. A successful result means staging is ready; it does not mean the frame
is visible.

### FrameRegion (`11`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 2 | X origin |
| 2 | 2 | Y origin |
| 4 | 2 | Region width |
| 6 | 2 | Region height |
| 8 | 4 | Offset in the complete frame pixel-byte stream |
| 12 | 4 | Region pixel-byte length |
| 16 | n | RGB565 pixel bytes |

The region length MUST equal `width * height * 2`, match the remaining payload,
stay within the declared frame, and not exceed the negotiated region limit.
Regions MAY arrive out of order. A byte range already accepted for the same
frame MAY be repeated only with identical bytes; it MUST NOT increase completion
coverage. Overlapping non-identical data invalidates the frame transaction.

`PacketIndex` and `PacketCount` identify the complete set of regions for the
frame. They do not replace geometric and byte-range validation. The last region
sets `FinalPacket`, but only `CompleteFrame` may request presentation.

### CompleteFrame (`12`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | Expected complete-frame CRC32 |

The device validates dimensions, coverage, byte count, and CRC before calling
the display adapter's present operation. It then returns `Ok` with the
`Presented` result flag. Missing ranges return `Incomplete`; checksum mismatch
returns `ChecksumMismatch`. Neither case changes the visible display.

Repeating an identical successful completion for the same session and frame ID
returns the cached success result and MUST NOT present the frame again.

### AbortFrame (`13`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Abort reason |
| 1 | 3 | Reserved, zero |

Abort reasons are `0` host cancellation, `1` replacement, and `2` shutdown.
Abort is idempotent. It discards only staging state; the visible frame remains.

### Clear (`20`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 2 | RGB565 clear color |

### SetBrightness (`21`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Brightness from 0 through 100 percent |

The device returns `Unsupported` if brightness control is not advertised. It
MUST NOT report success without applying the requested setting.

### RenderTestPattern (`22`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Pattern ID |
| 1 | 3 | Reserved, zero |

Pattern ID `1` is the v1 conformance pattern. The top half of the display is
divided into three vertical bands (red `F800`, green `07E0`, blue `001F`) and
the bottom half is divided into white `FFFF` and black `0000` vertical bands.
For divisions that leave a remainder, earlier bands receive one extra pixel.
The adapter generates the pattern locally at its native reported dimensions.

### Result (`7F`)

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 1 | Result code |
| 1 | 1 | Result flags |
| 2 | 2 | Detail code |
| 4 | 4 | Retry-after milliseconds; zero when not applicable |
| 8 | 4 | First missing byte offset; zero when not applicable |
| 12 | 4 | Missing byte count; zero when not applicable |

Result codes are:

| Value | Name | Meaning |
| ---: | --- | --- |
| `00` | Ok | Operation completed |
| `01` | Invalid | Malformed or inconsistent input |
| `02` | Unsupported | Known operation or capability is unavailable |
| `03` | UnsupportedVersion | No compatible protocol version |
| `04` | Busy | Device cannot accept the operation now |
| `05` | Incomplete | Required frame bytes are missing |
| `06` | ChecksumMismatch | Declared integrity check failed |
| `07` | Timeout | Device discarded expired staging state |
| `08` | HardwareFailure | Display adapter failed |
| `09` | WrongSession | Session is unknown or stale after reboot |
| `0A` | Conflict | Identifier was reused with different content |

Result flag bit 0 is `Presented`, bit 1 is `Duplicate`, and bit 2 is `Retryable`.
Other bits are reserved. Detail codes are message-specific and zero when no
additional standardized detail exists.

## Negotiation and session rules

1. The host sends `HelloRequest` with session ID zero and a new request ID.
2. The device returns `CapabilitiesResponse` with the same request ID, selected
   version, and a newly generated non-zero session ID.
3. The host validates the response envelope, selected version, lengths, and
   capability invariants before storing the capabilities as live.
4. Every later request carries that session ID. A stale or unknown value returns
   `WrongSession` and causes the host to discard cached live capabilities.
5. A reboot creates a new session ID. Persisted capabilities are diagnostic
   cache only and MUST be treated as stale until a new hello succeeds.
6. Minor-version negotiation enables only features defined by the selected
   minor. Unknown fields or flags are not accepted merely because a peer claims
   a newer minor version.

## Frame transaction and reliability rules

- Request IDs identify logical operations. Frame IDs identify frame staging and
  presentation. Both are monotonically advanced or randomly generated so that
  active identifiers are not reused.
- The host requests acknowledgement for `BeginFrame`, `CompleteFrame`, abort,
  and control commands. Individual regions are not positively acknowledged
  unless the host requests one or the device rejects them.
- A retry uses the same request ID, frame ID, message type, and payload plus the
  `Retry` flag. The receiver returns the cached result for a duplicate completed
  operation. It does not execute it again.
- The host uses bounded timeouts and a bounded retry count. Cancellation stops
  waiting and further retries, then sends a best-effort `AbortFrame` when a
  transaction was started.
- A device maintains one active staging transaction per display unless its
  capabilities explicitly say otherwise. A different `BeginFrame` returns
  `Busy` or atomically replaces the old transaction only when replacement was
  explicitly requested and the old transaction is discarded.
- Staging expires after the device's documented inactivity timeout. Expiry
  discards the staging bytes and records `Timeout`; it never presents them.
- Incomplete, invalid, conflicting, timed-out, aborted, pre-reboot, or
  wrong-session data never changes the visible display.
- A frame ID is presented at most once within one session. Duplicate completion
  returns the prior result with the `Duplicate` flag.
- When `Incomplete` identifies one contiguous missing range, the result includes
  its offset and length. The host MAY resend intersecting regions and retry
  completion. Multiple gaps may require a new complete frame transaction in
  v1.0.

## Security boundary

Version 1.0 is intended for a trusted, explicitly configured local network. The
magic value, CRC, request IDs, frame IDs, and session IDs are validation and
correlation mechanisms, not authentication, authorization, encryption, or
replay protection against an attacker. Deployments that require an untrusted
network boundary MUST add protection outside this protocol before enabling
remote access.

Protocol diagnostics MUST use an allow-list and MUST NOT include Wi-Fi
credentials, HTTP provisioning secrets, tokens, or raw frame payloads.

## Golden conformance vector

The canonical v1.0 hello request uses:

- request ID `01020304`;
- acknowledgement-required flag;
- packet index zero and packet count one;
- payload `0100010004D00000` (v1.0 through v1.0, maximum datagram 1232,
  reserved zero);
- payload CRC32 `603FAE35`.

The complete datagram is:

```text
4D4F4241010001020020000801020304000000000000000000000001603FAE35
0100010004D00000
```

A conforming encoder MUST produce these bytes exactly. A conforming decoder
MUST accept them and MUST reject variants with invalid magic, header length,
declared payload length, packet sequence, reserved flags, or payload CRC.

Further message-specific golden vectors are added with their payload codecs so
that specification, .NET implementation, and firmware harness share the same
fixtures.

## References

- [RFC 768 - User Datagram Protocol](https://www.rfc-editor.org/rfc/rfc768)
- [RFC 8085 - UDP Usage Guidelines](https://www.rfc-editor.org/rfc/rfc8085)
- [RFC 8200 - Internet Protocol, Version 6](https://www.rfc-editor.org/rfc/rfc8200)
