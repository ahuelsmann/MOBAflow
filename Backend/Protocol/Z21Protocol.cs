// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Backend.Protocol;

/// <summary>
/// Z21 LAN Protocol constants and definitions.
/// Based on official Z21 LAN Protokoll Spezifikation v1.13 (06.11.2023).
/// 
/// Communication:
/// - UDP ports 21105 or 21106
/// - Communication is always asynchronous (broadcasts may occur between request/response)
/// - Each client must communicate at least once per minute or will be removed from active list
/// - Clients should send LAN_LOGOFF when disconnecting
/// 
/// Packet Structure (Z21 Datensatz):
/// - DataLen (2 bytes, little-endian): Total length including DataLen, Header, Data
/// - Header (2 bytes, little-endian): Command or protocol group
/// - Data (n bytes): Command-specific data
/// 
/// Supported Hardware (HwType):
/// - 0x00000200: "schwarze Z21" (Hardware ab 2012)
/// - 0x00000201: "schwarze Z21" (Hardware ab 2013)
/// - 0x00000202: SmartRail (ab 2012)
/// - 0x00000203: "weiße z21" Starterset (ab 2013)
/// - 0x00000204: "z21 start" Starterset (ab 2016)
/// - 0x00000211: Z21 XL Series (ab 2020)
/// </summary>
public static class Z21Protocol
{
    /// <summary>
    /// Default UDP port for Z21 communication.
    /// Alternative port 21106 is also supported.
    /// </summary>
    public const int DefaultPort = 21105;

    /// <summary>
    /// Alternative UDP port for Z21 communication.
    /// </summary>
    public const int AlternativePort = 21106;

    /// <summary>
    /// Maximum number of clients that can connect to Z21.
    /// Without LAN_LOGOFF, inactive clients are removed after 60 seconds.
    /// </summary>
    public const int MaxClients = 20;

    /// <summary>
    /// Z21 LAN packet headers (bytes 2-3 of each packet).
    /// Format: DataLen (2 bytes) + Header (2 bytes) + Data (n bytes)
    /// </summary>
    public static class Header
    {
        // ==================== System, Status, Versions (Section 2) ====================
        
        /// <summary>
        /// LAN_GET_SERIAL_NUMBER: Request Z21 serial number.
        /// Request: 04-00-10-00
        /// Response: 08-00-10-00 + SerialNumber (32-bit little-endian)
        /// </summary>
        public const byte LAN_GET_SERIAL_NUMBER = 0x10;

        /// <summary>
        /// LAN_GET_CODE: Check SW feature scope (especially for z21 start).
        /// Request: 04-00-18-00
        /// Response: 05-00-18-00 + Code (8-bit)
        /// Codes: 0x00=no lock, 0x01=z21 start locked, 0x02=z21 start unlocked
        /// </summary>
        public const byte LAN_GET_CODE = 0x18;

        /// <summary>
        /// LAN_GET_HWINFO: Request hardware type and firmware version.
        /// Request: 04-00-1A-00
        /// Response: 0C-00-1A-00 + HwType (32-bit) + FwVersion (32-bit BCD)
        /// Available since Z21 FW 1.20.
        /// </summary>
        public const byte LAN_GET_HWINFO = 0x1A;

        /// <summary>
        /// LAN_LOGOFF: Unsubscribe client from Z21 broadcasts.
        /// Request: 04-00-30-00 (no response)
        /// Important: Frees client slot immediately instead of waiting 60s timeout.
        /// Use same port number as when connecting.
        /// </summary>
        public const byte LAN_LOGOFF = 0x30;

        /// <summary>
        /// LAN_X_xxx: X-BUS protocol tunneling header.
        /// Used for commands derived from X-BUS protocol (driving, switching, CV programming).
        /// Last byte is XOR checksum over X-BUS command.
        /// </summary>
        public const byte LAN_X_HEADER = 0x40;

        /// <summary>
        /// LAN_SET_BROADCASTFLAGS: Set broadcast subscription flags.
        /// Request: 08-00-50-00 + Flags (32-bit little-endian)
        /// Flags are per client (IP + port) and must be set after each connection.
        /// </summary>
        public const byte LAN_SET_BROADCASTFLAGS = 0x50;

        /// <summary>
        /// LAN_GET_BROADCASTFLAGS: Read current broadcast flags.
        /// Request: 04-00-51-00
        /// Response: 08-00-51-00 + Flags (32-bit little-endian)
        /// </summary>
        public const byte LAN_GET_BROADCASTFLAGS = 0x51;

        // ==================== Settings (Section 3) ====================

        /// <summary>
        /// LAN_GET_LOCOMODE: Read output format for loco address (DCC/MM).
        /// Request: 06-00-60-00 + LocoAddr (16-bit big-endian)
        /// Response: 07-00-60-00 + LocoAddr + Mode (0=DCC, 1=MM)
        /// Note: Addresses >= 256 are always DCC.
        /// </summary>
        public const byte LAN_GET_LOCOMODE = 0x60;

        /// <summary>
        /// LAN_SET_LOCOMODE: Set output format for loco address.
        /// Request: 07-00-61-00 + LocoAddr (16-bit big-endian) + Mode
        /// Stored persistently in Z21.
        /// </summary>
        public const byte LAN_SET_LOCOMODE = 0x61;

        /// <summary>
        /// LAN_GET_TURNOUTMODE: Read output format for accessory decoder address.
        /// Request: 06-00-70-00 + Addr (16-bit big-endian)
        /// Response: 07-00-70-00 + Addr + Mode (0=DCC, 1=MM)
        /// </summary>
        public const byte LAN_GET_TURNOUTMODE = 0x70;

        /// <summary>
        /// LAN_SET_TURNOUTMODE: Set output format for accessory decoder.
        /// Request: 07-00-71-00 + Addr (16-bit big-endian) + Mode
        /// MM accessory decoders supported since Z21 FW 1.20.
        /// </summary>
        public const byte LAN_SET_TURNOUTMODE = 0x71;

        // ==================== Rückmelder R-BUS (Section 7) ====================

        /// <summary>
        /// LAN_RMBUS_DATACHANGED: R-Bus feedback module status changed.
        /// Broadcast: 0F-00-80-00 + GroupIndex (1 byte) + Status (10 bytes)
        /// GroupIndex: 0=modules 1-10, 1=modules 11-20
        /// Status: 1 byte per module, 1 bit per input (8 inputs per module)
        /// 
        /// Compatible with Roco feedback modules: 10787, 10808, 10819
        /// </summary>
        public const byte LAN_RMBUS_DATACHANGED = 0x80;
        public const byte LAN_RBUS_DATACHANGED = LAN_RMBUS_DATACHANGED;

        /// <summary>
        /// LAN_RMBUS_GETDATA: Request current R-Bus feedback status.
        /// Request: 05-00-81-00 + GroupIndex (1 byte)
        /// Response: LAN_RMBUS_DATACHANGED
        /// </summary>
        public const byte LAN_RMBUS_GETDATA = 0x81;

        /// <summary>
        /// LAN_RMBUS_PROGRAMMODULE: Program R-Bus module address.
        /// Request: 05-00-82-00 + Address (1-20, 0=stop programming)
        /// Only one module may be connected during programming.
        /// Command repeats until Address=0 is sent.
        /// </summary>
        public const byte LAN_RMBUS_PROGRAMMODULE = 0x82;

        // ==================== System State (Section 2.18-2.19) ====================

        /// <summary>
        /// LAN_SYSTEMSTATE_DATACHANGED: System state update broadcast.
        /// Broadcast: 14-00-84-00 + SystemState (16 bytes)
        /// Contains: MainCurrent, ProgCurrent, Temperature, Voltage, CentralState, etc.
        /// Sent when subscribed via BroadcastFlags 0x00000100 or on explicit request.
        /// </summary>
        public const byte LAN_SYSTEMSTATE = 0x84;

        /// <summary>
        /// LAN_SYSTEMSTATE_GETDATA: Request current system state.
        /// Request: 04-00-85-00
        /// Response: LAN_SYSTEMSTATE_DATACHANGED
        /// </summary>
        public const byte LAN_SYSTEMSTATE_GETDATA = 0x85;

        // ==================== RailCom (Section 8) ====================

        /// <summary>
        /// LAN_RAILCOM_DATACHANGED: RailCom data changed broadcast.
        /// Broadcast: 11-00-88-00 + RailComDaten (13 bytes)
        /// Contains: LocoAddress, ReceiveCounter, Speed, QoS
        /// Available since Z21 FW 1.29.
        /// </summary>
        public const byte LAN_RAILCOM_DATACHANGED = 0x88;

        /// <summary>
        /// LAN_RAILCOM_GETDATA: Request RailCom data.
        /// Request: 07-00-89-00 + Type (1 byte) + LocoAddr (16-bit little-endian)
        /// Type 0x01 = request for specific loco address
        /// </summary>
        public const byte LAN_RAILCOM_GETDATA = 0x89;

        // ==================== LocoNet (Section 9) ====================

        /// <summary>
        /// LAN_LOCONET_Z21_RX: LocoNet message received by Z21.
        /// Broadcast to clients with LocoNet subscription.
        /// </summary>
        public const byte LAN_LOCONET_Z21_RX = 0xA0;

        /// <summary>
        /// LAN_LOCONET_Z21_TX: LocoNet message sent by Z21.
        /// Broadcast to clients with LocoNet subscription.
        /// </summary>
        public const byte LAN_LOCONET_Z21_TX = 0xA1;

        /// <summary>
        /// LAN_LOCONET_FROM_LAN: Send LocoNet message via LAN.
        /// Request: 04+n-00-A2-00 + LocoNet message (n bytes incl. checksum)
        /// Z21 acts as Ethernet/LocoNet gateway and LocoNet master.
        /// Available since Z21 FW 1.20.
        /// </summary>
        public const byte LAN_LOCONET_FROM_LAN = 0xA2;

        /// <summary>
        /// LAN_LOCONET_DISPATCH_ADDR: Prepare loco for LocoNet dispatch.
        /// Request: 06-00-A3-00 + LocoAddr (16-bit little-endian)
        /// Response (FW >= 1.22): 07-00-A3-00 + LocoAddr + Result
        /// </summary>
        public const byte LAN_LOCONET_DISPATCH_ADDR = 0xA3;

        /// <summary>
        /// LAN_LOCONET_DETECTOR: LocoNet occupancy detector status.
        /// Request: 07-00-A4-00 + Type + ReportAddr (16-bit little-endian)
        /// Types: 0x80=Digitrax SIC, 0x81=Uhlenbrock, 0x82=LISSY
        /// Available since Z21 FW 1.22.
        /// </summary>
        public const byte LAN_LOCONET_DETECTOR = 0xA4;

        // ==================== CAN (Section 10) ====================

        /// <summary>
        /// LAN_CAN_DETECTOR: CAN occupancy detector status (10808, 10819).
        /// Request: 07-00-C4-00 + Type (0x00) + CAN-NetworkID (16-bit)
        /// NetworkID 0xD000 = query all CAN detectors.
        /// Available since Z21 FW 1.30.
        /// </summary>
        public const byte LAN_CAN_DETECTOR = 0xC4;

        /// <summary>
        /// LAN_CAN_DEVICE_GET_DESCRIPTION: Read CAN device name.
        /// Request: 06-00-C8-00 + NetworkID
        /// Response: 16-00-C8-00 + NetworkID + Name[16]
        /// </summary>
        public const byte LAN_CAN_DEVICE_GET_DESCRIPTION = 0xC8;

        /// <summary>
        /// LAN_CAN_DEVICE_SET_DESCRIPTION: Write CAN device name.
        /// Request: 16-00-C9-00 + NetworkID + Name[16]
        /// </summary>
        public const byte LAN_CAN_DEVICE_SET_DESCRIPTION = 0xC9;

        /// <summary>
        /// LAN_CAN_BOOSTER_SYSTEMSTATE_CHGD: CAN booster state broadcast.
        /// Sent once per second per booster output.
        /// Available since Z21 FW 1.41.
        /// </summary>
        public const byte LAN_CAN_BOOSTER_SYSTEMSTATE = 0xCA;

        /// <summary>
        /// LAN_CAN_BOOSTER_SET_TRACKPOWER: Control CAN booster outputs.
        /// Request: 07-00-CB-00 + NetworkID + Power
        /// Power: 0x00=off, 0xFF=on, 0x10/0x11=first output, 0x20/0x21=second output
        /// </summary>
        public const byte LAN_CAN_BOOSTER_SET_TRACKPOWER = 0xCB;

        // ==================== Fast Clock / Modellzeit (Section 12) ====================

        /// <summary>
        /// LAN_FAST_CLOCK_CONTROL: Control model time (read/set/start/stop).
        /// Available since Z21 FW 1.43.
        /// </summary>
        public const byte LAN_FAST_CLOCK_CONTROL = 0xCC;

        /// <summary>
        /// LAN_FAST_CLOCK_DATA: Model time broadcast.
        /// Sent once per model minute when subscribed (Flag 0x00000010).
        /// </summary>
        public const byte LAN_FAST_CLOCK_DATA = 0xCD;

        /// <summary>
        /// LAN_FAST_CLOCK_SETTINGS_GET: Read persistent fast clock settings.
        /// </summary>
        public const byte LAN_FAST_CLOCK_SETTINGS_GET = 0xCE;

        /// <summary>
        /// LAN_FAST_CLOCK_SETTINGS_SET: Write persistent fast clock settings.
        /// </summary>
        public const byte LAN_FAST_CLOCK_SETTINGS_SET = 0xCF;
    }

    /// <summary>
    /// X-BUS protocol X-Header values (first byte after LAN_X_HEADER).
    /// Used with Header 0x40 for X-BUS tunneled commands.
    /// </summary>
    public static class XHeader
    {
        // ==================== System Commands ====================

        /// <summary>
        /// LAN_X_GET_VERSION: Request X-Bus version.
        /// Command: 0x21 0x21 0x00
        /// Response: 0x63 0x21 XBUS_VER CMDST_ID
        /// CMDST_ID 0x12 = Z21 device family
        /// </summary>
        public const byte X_GET_VERSION = 0x21;

        /// <summary>
        /// LAN_X_GET_STATUS: Request central status.
        /// Command: 0x21 0x24 0x05
        /// Response: LAN_X_STATUS_CHANGED
        /// </summary>
        public const byte X_GET_STATUS = 0x21;

        /// <summary>
        /// LAN_X_SET_TRACK_POWER: Track power on/off command group.
        /// With DB0=0x80: LAN_X_SET_TRACK_POWER_OFF (Command: 0x21 0x80 0xA1)
        /// With DB0=0x81: LAN_X_SET_TRACK_POWER_ON  (Command: 0x21 0x81 0xA0)
        /// </summary>
        public const byte X_TRACK_POWER = 0x21;

        /// <summary>
        /// LAN_X_SET_STOP: Activate emergency stop (locos stop, track power stays on).
        /// Command: 0x80 0x80
        /// Response: LAN_X_BC_STOPPED (0x81 0x00 0x81)
        /// </summary>
        public const byte X_SET_STOP = 0x80;

        /// <summary>
        /// LAN_X_BC_STOPPED: Emergency stop broadcast response.
        /// Packet: 0x81 0x00 0x81
        /// </summary>
        public const byte X_BC_STOPPED = 0x81;

        // ==================== Status Responses ====================

        /// <summary>
        /// LAN_X_BC_xxx: Broadcast status group.
        /// 0x00: LAN_X_BC_TRACK_POWER_OFF
        /// 0x01: LAN_X_BC_TRACK_POWER_ON
        /// 0x02: LAN_X_BC_PROGRAMMING_MODE
        /// 0x08: LAN_X_BC_TRACK_SHORT_CIRCUIT
        /// 0x12: LAN_X_CV_NACK_SC (short circuit during CV programming)
        /// 0x13: LAN_X_CV_NACK (no ACK from decoder)
        /// 0x82: LAN_X_UNKNOWN_COMMAND
        /// </summary>
        public const byte X_BC_STATUS = 0x61;

        /// <summary>
        /// LAN_X_STATUS_CHANGED: Central state response.
        /// Packet: 0x62 0x22 Status XOR
        /// Status bits: see CentralState bitmasks
        /// </summary>
        public const byte X_STATUS_CHANGED = 0x62;
        public const byte X_STATUS = X_BC_STATUS;

        /// <summary>
        /// LAN_X_GET_FIRMWARE_VERSION: Request firmware version.
        /// Command: 0xF1 0x0A 0xFB
        /// Response: 0xF3 0x0A V_MSB V_LSB XOR (BCD format)
        /// </summary>
        public const byte X_GET_FIRMWARE_VERSION = 0xF1;

        /// <summary>
        /// LAN_X_CV_RESULT: CV read/write result.
        /// Packet: 0x64 0x14 CVAdr_MSB CVAdr_LSB Value XOR
        /// </summary>
        public const byte X_CV_RESULT = 0x64;

        // ==================== Driving Commands (Section 4) ====================

        /// <summary>
        /// LAN_X_LOCO_INFO: Loco status information.
        /// Response packet: 0xEF + Loco-Information
        /// Contains: Address, speed steps, direction, functions F0-F31
        /// </summary>
        public const byte X_LOCO_INFO = 0xEF;

        /// <summary>
        /// LAN_X_GET_LOCO_INFO: Request loco status (also subscribes to updates).
        /// Command: 0xE3 0xF0 Adr_MSB Adr_LSB XOR
        /// For addresses >= 128: Adr_MSB = (0xC0 | high_byte)
        /// Max 16 loco addresses can be subscribed per client (FIFO).
        /// </summary>
        public const byte X_GET_LOCO_INFO = 0xE3;

        /// <summary>
        /// LAN_X_SET_LOCO_DRIVE: Set loco speed and direction.
        /// Command: 0xE4 0x1S Adr_MSB Adr_LSB RVVVVVVV XOR
        /// S: Speed steps (0=14, 2=28, 3=128)
        /// R: Direction (1=forward)
        /// V: Speed (encoding depends on speed steps)
        /// </summary>
        public const byte X_SET_LOCO_DRIVE = 0xE4;

        /// <summary>
        /// LAN_X_SET_LOCO_FUNCTION: Set single loco function.
        /// Command: 0xE4 0xF8 Adr_MSB Adr_LSB TTNNNNNN XOR
        /// TT: 00=off, 01=on, 10=toggle
        /// NNNNNN: Function index (0=F0/light, 1=F1, etc.)
        /// </summary>
        public const byte X_SET_LOCO_FUNCTION = 0xE4;

        /// <summary>
        /// LAN_X_SET_LOCO_E_STOP: Emergency stop single loco.
        /// Command: 0x92 Adr_MSB Adr_LSB XOR
        /// Available since Z21 FW 1.43.
        /// </summary>
        public const byte X_SET_LOCO_E_STOP = 0x92;

        // ==================== Switching Commands (Section 5) ====================

        /// <summary>
        /// LAN_X_GET_TURNOUT_INFO: Request turnout status.
        /// Command: 0x43 FAdr_MSB FAdr_LSB XOR
        /// </summary>
        public const byte X_GET_TURNOUT_INFO = 0x43;

        /// <summary>
        /// LAN_X_TURNOUT_INFO: Turnout status response.
        /// Packet: 0x43 FAdr_MSB FAdr_LSB 000000ZZ XOR
        /// ZZ: 00=not switched, 01=P=0, 10=P=1
        /// </summary>
        public const byte X_TURNOUT_INFO = 0x43;

        /// <summary>
        /// LAN_X_GET_EXT_ACCESSORY_INFO: Request extended accessory decoder status.
        /// Command: 0x44 Adr_MSB Adr_LSB 0x00 XOR
        /// Available since Z21 FW 1.40.
        /// </summary>
        public const byte X_GET_EXT_ACCESSORY_INFO = 0x44;

        /// <summary>
        /// LAN_X_SET_TURNOUT: Set turnout position.
        /// Command: 0x53 FAdr_MSB FAdr_LSB 10Q0A00P XOR
        /// A: 0=deactivate, 1=activate
        /// P: 0=output 1, 1=output 2
        /// Q: 0=immediate, 1=queue (since FW 1.24)
        /// </summary>
        public const byte X_SET_TURNOUT = 0x53;

        /// <summary>
        /// LAN_X_SET_EXT_ACCESSORY: Extended accessory decoder command (DCCext).
        /// Command: 0x54 Adr_MSB Adr_LSB DDDDDDDD 0x00 XOR
        /// RawAddress 4 = first extended accessory decoder (shown as "Address 1" in UI)
        /// Available since Z21 FW 1.40.
        /// </summary>
        public const byte X_SET_EXT_ACCESSORY = 0x54;

        // ==================== CV Programming (Section 6) ====================

        /// <summary>
        /// LAN_X_CV_READ: Read CV in direct mode.
        /// Command: 0x23 0x11 CVAdr_MSB CVAdr_LSB XOR
        /// CV address: 0=CV1, 1=CV2, 255=CV256
        /// </summary>
        public const byte X_CV_READ = 0x23;

        /// <summary>
        /// LAN_X_CV_WRITE: Write CV in direct mode.
        /// Command: 0x24 0x12 CVAdr_MSB CVAdr_LSB Value XOR
        /// </summary>
        public const byte X_CV_WRITE = 0x24;

        /// <summary>
        /// LAN_X_CV_POM_xxx: POM (Programming on Main) commands.
        /// For loco decoders: 0xE6 0x30 + POM parameters
        /// For accessory decoders: 0xE6 0x31 + POM parameters
        /// </summary>
        public const byte X_CV_POM = 0xE6;
    }

    /// <summary>
    /// Track power control DB0 values (used with X_TRACK_POWER).
    /// </summary>
    public static class TrackPowerDb0
    {
        /// <summary>LAN_X_SET_TRACK_POWER_OFF: Command 0x21 0x80 0xA1</summary>
        public const byte OFF = 0x80;

        /// <summary>LAN_X_SET_TRACK_POWER_ON: Command 0x21 0x81 0xA0</summary>
        public const byte ON = 0x81;
    }

    /// <summary>
    /// Broadcast flags for LAN_SET_BROADCASTFLAGS (0x50).
    /// Flags are OR-combined and set per client (IP + port).
    /// Must be set after each connection.
    /// 
    /// Warning: Consider network load impact, especially for flags
    /// 0x00010000, 0x00040000, 0x02000000, 0x04000000.
    /// UDP packets may be dropped by router under load.
    /// </summary>
    public static class BroadcastFlags
    {
        /// <summary>
        /// All flags set - subscribes to EVERYTHING.
        /// NOT RECOMMENDED due to high network traffic!
        /// </summary>
        public const uint All = 0xFFFF_FFFF;

        /// <summary>
        /// Basic flags for feedback-focused applications.
        /// Combines: R-Bus feedback + System state updates
        /// </summary>
        public const uint Basic = Rbus | SystemState;

        // ==================== Standard Flags ====================

        /// <summary>
        /// 0x00000001: Auto-generated broadcasts for driving and switching.
        /// Subscribes to:
        /// - LAN_X_BC_TRACK_POWER_OFF/ON
        /// - LAN_X_BC_PROGRAMMING_MODE
        /// - LAN_X_BC_TRACK_SHORT_CIRCUIT
        /// - LAN_X_BC_STOPPED
        /// - LAN_X_LOCO_INFO (loco address must also be subscribed)
        /// - LAN_X_TURNOUT_INFO
        /// </summary>
        public const uint Driving = 0x0000_0001;

        /// <summary>
        /// 0x00000002: R-Bus feedback module changes.
        /// Broadcasts: LAN_RMBUS_DATACHANGED
        /// Compatible with: Roco 10787, 10808, 10819
        /// </summary>
        public const uint Rbus = 0x0000_0002;

        /// <summary>
        /// 0x00000004: RailCom data for subscribed locos.
        /// Broadcasts: LAN_RAILCOM_DATACHANGED
        /// Loco address must be subscribed first.
        /// </summary>
        public const uint RailCom = 0x0000_0004;

        /// <summary>
        /// 0x00000100: Z21 system state changes.
        /// Broadcasts: LAN_SYSTEMSTATE_DATACHANGED
        /// Contains: current, voltage, temperature, central state
        /// </summary>
        public const uint SystemState = 0x0000_0100;

        /// <summary>
        /// 0x00000010: Fast clock model time messages.
        /// Broadcasts: LAN_FAST_CLOCK_DATA
        /// Available since Z21 FW 1.43.
        /// </summary>
        public const uint FastClock = 0x0000_0010;

        // ==================== Extended Flags (since FW 1.20) ====================

        /// <summary>
        /// 0x00010000: LAN_X_LOCO_INFO for ALL locos (no subscription needed).
        /// HIGH TRAFFIC - only for full PC control software, not mobile controllers!
        /// Since FW 1.24: Only changed locos are reported.
        /// </summary>
        public const uint AllLocoInfo = 0x0001_0000;

        /// <summary>
        /// 0x00020000: CAN bus booster status messages.
        /// Broadcasts: LAN_CAN_BOOSTER_SYSTEMSTATE_CHGD
        /// Available since Z21 FW 1.41.
        /// </summary>
        public const uint CanBooster = 0x0002_0000;

        /// <summary>
        /// 0x00040000: RailCom data for ALL locos (no subscription needed).
        /// HIGH TRAFFIC - only for PC control software!
        /// Available since Z21 FW 1.29.
        /// </summary>
        public const uint AllRailCom = 0x0004_0000;

        /// <summary>
        /// 0x00080000: CAN bus occupancy detector messages.
        /// Broadcasts: LAN_CAN_DETECTOR
        /// Available since Z21 FW 1.30.
        /// </summary>
        public const uint CanDetector = 0x0008_0000;

        // ==================== LocoNet Flags (since FW 1.20) ====================

        /// <summary>
        /// 0x01000000: LocoNet messages (excluding loco/turnout specific).
        /// </summary>
        public const uint LocoNet = 0x0100_0000;

        /// <summary>
        /// 0x02000000: Loco-specific LocoNet messages.
        /// HIGH TRAFFIC!
        /// Includes: OPC_LOCO_SPD, OPC_LOCO_DIRF, OPC_LOCO_SND, OPC_LOCO_F912, OPC_EXP_CMD
        /// </summary>
        public const uint LocoNetLoco = 0x0200_0000;

        /// <summary>
        /// 0x04000000: Turnout-specific LocoNet messages.
        /// HIGH TRAFFIC!
        /// Includes: OPC_SW_REQ, OPC_SW_REP, OPC_SW_ACK, OPC_SW_STATE
        /// </summary>
        public const uint LocoNetTurnout = 0x0400_0000;

        /// <summary>
        /// 0x08000000: LocoNet occupancy detector status messages.
        /// Broadcasts: LAN_LOCONET_DETECTOR
        /// Available since Z21 FW 1.22.
        /// </summary>
        public const uint LocoNetDetector = 0x0800_0000;
    }

    /// <summary>
    /// Central state bitmasks for SystemState.CentralState (byte 12).
    /// Also returned in LAN_X_STATUS_CHANGED response.
    /// </summary>
    public static class CentralState
    {
        /// <summary>0x01: Emergency stop is active</summary>
        public const byte EmergencyStop = 0x01;

        /// <summary>0x02: Track voltage is off</summary>
        public const byte TrackVoltageOff = 0x02;

        /// <summary>0x04: Short circuit detected</summary>
        public const byte ShortCircuit = 0x04;

        /// <summary>0x20: Programming mode is active</summary>
        public const byte ProgrammingModeActive = 0x20;
    }

    /// <summary>
    /// Extended central state bitmasks for SystemState.CentralStateEx (byte 13).
    /// </summary>
    public static class CentralStateEx
    {
        /// <summary>0x01: Temperature too high</summary>
        public const byte HighTemperature = 0x01;

        /// <summary>0x02: Input voltage too low</summary>
        public const byte PowerLost = 0x02;

        /// <summary>0x04: Short circuit on external booster output</summary>
        public const byte ShortCircuitExternal = 0x04;

        /// <summary>0x08: Short circuit on main or programming track</summary>
        public const byte ShortCircuitInternal = 0x08;

        /// <summary>0x20: Turnout addressing according to RCN-213 (since FW 1.42)</summary>
        public const byte Rcn213 = 0x20;
    }

    /// <summary>
    /// Capabilities bitmasks for SystemState.Capabilities (byte 15).
    /// Available since Z21 FW 1.42.
    /// If Capabilities == 0, assume older firmware version.
    /// </summary>
    public static class Capabilities
    {
        /// <summary>0x01: Supports DCC</summary>
        public const byte Dcc = 0x01;

        /// <summary>0x02: Supports Motorola (MM)</summary>
        public const byte Mm = 0x02;

        /// <summary>0x08: RailCom is enabled</summary>
        public const byte RailCom = 0x08;

        /// <summary>0x10: Accepts LAN commands for loco decoders</summary>
        public const byte LocoCmds = 0x10;

        /// <summary>0x20: Accepts LAN commands for accessory decoders</summary>
        public const byte AccessoryCmds = 0x20;

        /// <summary>0x40: Accepts LAN commands for occupancy detectors</summary>
        public const byte DetectorCmds = 0x40;

        /// <summary>0x80: Requires unlock code (z21 start)</summary>
        public const byte NeedsUnlockCode = 0x80;
    }

    /// <summary>
    /// Hardware type codes returned by LAN_GET_HWINFO.
    /// </summary>
    public static class HardwareType
    {
        /// <summary>0x00000200: "schwarze Z21" (Hardware variant from 2012)</summary>
        public const uint Z21Old = 0x0000_0200;

        /// <summary>0x00000201: "schwarze Z21" (Hardware variant from 2013)</summary>
        public const uint Z21New = 0x0000_0201;

        /// <summary>0x00000202: SmartRail (from 2012)</summary>
        public const uint Smartrail = 0x0000_0202;

        /// <summary>0x00000203: "weiße z21" Starterset variant (from 2013)</summary>
        public const uint Z21Small = 0x0000_0203;

        /// <summary>0x00000204: "z21 start" Starterset variant (from 2016)</summary>
        public const uint Z21Start = 0x0000_0204;

        /// <summary>0x00000205: 10806 "Z21 Single Booster" (zLink)</summary>
        public const uint SingleBooster = 0x0000_0205;

        /// <summary>0x00000206: 10807 "Z21 Dual Booster" (zLink)</summary>
        public const uint DualBooster = 0x0000_0206;

        /// <summary>0x00000211: 10870 "Z21 XL Series" (from 2020)</summary>
        public const uint Z21Xl = 0x0000_0211;

        /// <summary>0x00000212: 10869 "Z21 XL Booster" (from 2021, zLink)</summary>
        public const uint XlBooster = 0x0000_0212;

        /// <summary>0x00000301: 10836 "Z21 SwitchDecoder" (zLink)</summary>
        public const uint Z21SwitchDecoder = 0x0000_0301;

        /// <summary>0x00000302: 10837 "Z21 SignalDecoder" (zLink)</summary>
        public const uint Z21SignalDecoder = 0x0000_0302;
    }

    /// <summary>
    /// Speed step modes for LAN_X_SET_LOCO_DRIVE.
    /// Value S in command byte 0x1S.
    /// </summary>
    public static class SpeedSteps
    {
        /// <summary>DCC 14 speed steps, or MMI with 14 steps and F0</summary>
        public const byte Steps14 = 0x00;

        /// <summary>DCC 28 speed steps, or MMII with 14 real steps and F0-F4</summary>
        public const byte Steps28 = 0x02;

        /// <summary>DCC 128 speed steps (126 usable + stops), or MMII with 28 real steps and F0-F4</summary>
        public const byte Steps128 = 0x03;
    }

    /// <summary>
    /// Loco mode values for LAN_GET/SET_LOCOMODE.
    /// </summary>
    public static class LocoMode
    {
        /// <summary>DCC format</summary>
        public const byte Dcc = 0x00;

        /// <summary>Motorola format</summary>
        public const byte Mm = 0x01;
    }

    /// <summary>
    /// Converts byte array to hex string for debugging.
    /// </summary>
    public static string ToHex(byte[] data) => BitConverter.ToString(data);

    /// <summary>
    /// Converts byte array to hex string with spaces for readability.
    /// </summary>
    public static string ToHexSpaced(byte[] data) => BitConverter.ToString(data).Replace("-", " ");
}
