# Viessmann multiplex signal mapping

MOBAflow maps signal aspects to Z21 turnout commands through
`Common/Multiplex/MultiplexerHelper.cs`. The currently registered multiplexers
are Viessmann `5229` and `52292`.

## 5229 with main signal 4046

For base address `B`, the current mapping is:

| Aspect | DCC address | Output | Activate |
| --- | ---: | ---: | --- |
| Hp0 | `B` | 0 | true |
| Ks1 | `B` | 1 | true |
| Ra12 | `B+1` | 0 | true |
| Zs1 | `B+1` | 1 | true |
| Ks2 | `B+2` | 0 | true |
| Ks1 blinking | `B+2` | 1 | true |
| Marker light | `B+3` | 0 | true |
| Dark | `B+3` | 1 | true |

The 5229 definition also contains reduced aspect sets for signal articles 4040,
4042, 4043 and 4045. The `52292` double multiplexer reuses the same per-signal
mapping.

## Configure a signal

1. Add or select a signal in **Signal Box**.
2. Set the multiplexer article, main-signal article and base DCC address in the
   signal properties.
3. Connect the Z21 and select an aspect.
4. Compare the physical result with the current Viessmann manual for the exact
   decoder/signal combination.

If a decoder is wired with reversed polarity, the four address offsets can be
inverted independently through the `SignalBox` settings in `appsettings.json`:

```json
"SignalBox": {
  "InvertPolarityOffset0": true,
  "InvertPolarityOffset1": false,
  "InvertPolarityOffset2": false,
  "InvertPolarityOffset3": false
}
```

The JSON property names and casing must match the current settings schema.

## Developer notes

`MultiplexerCommandResolver` combines the signal's base address, selected
article, aspect and per-offset polarity. It then calls the Z21 turnout command
path. Keep mapping changes covered by `MultiplexerHelperTests` and
`MultiplexerCommandResolverTests`.

Vendor manuals remain authoritative for physical decoder programming and
wiring. MOBAflow is independent from Viessmann.
