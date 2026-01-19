import { makeAutoObservable, runInAction } from 'mobx'
import { api } from '../api/apiClient'

// ============================================================================
// INTERFACES - Matching WebAppViewModel from SharedUI
// ============================================================================

export interface Train {
  id: number
  name: string
  address: number
  speed: number
  direction: 'forward' | 'backward'
  functions: boolean[]
}

export interface Journey {
  id: number
  name: string
  trainId: number
  stations: string[]
  isRunning: boolean
}

/**
 * Z21 System State - matches WebAppViewModel properties
 */
export interface SystemState {
  isTrackPowerOn: boolean
  isEmergencyStop: boolean
  mainCurrent: number      // mA
  temperature: number      // °C
  supplyVoltage: number    // mV
  vccVoltage: number       // mV
}

/**
 * Z21 Version Info
 */
export interface VersionInfo {
  serialNumber: string
  firmwareVersion: string
  hardwareType: string
}

/**
 * Track Statistics - matches InPortStatistic from SharedUI
 */
export interface TrackStatistic {
  inPort: number
  name: string
  count: number
  targetLapCount: number
  lastLapTime: string | null      // "mm:ss" format
  lastFeedbackTime: string | null // "HH:mm:ss" format
  hasReceivedFirstLap: boolean
  progress: number                 // 0.0 to 1.0
  lapCountFormatted: string        // "X/Y laps"
}

/**
 * Lap Counter Settings
 */
export interface LapCounterSettings {
  countOfFeedbackPoints: number
  globalTargetLapCount: number
  useTimerFilter: boolean
  timerIntervalSeconds: number
}

// ============================================================================
// APPSTORE - MobX State Management (mirrors WebAppViewModel)
// ============================================================================

/**
 * Main application store using MobX for state management.
 * Mirrors WebAppViewModel from SharedUI for direct Blazor/React comparison.
 */
export class AppStore {
  // ─────────────────────────────────────────────────────────────────────────
  // Z21 Connection State
  // ─────────────────────────────────────────────────────────────────────────
  isConnected = false
  ipAddress = '192.168.0.111'
  availableIpAddresses: string[] = ['192.168.0.111']
  connectionError: string | null = null
  statusText = 'Disconnected'

  // ─────────────────────────────────────────────────────────────────────────
  // Z21 System State (matches WebAppViewModel)
  // ─────────────────────────────────────────────────────────────────────────
  systemState: SystemState = {
    isTrackPowerOn: false,
    isEmergencyStop: false,
    mainCurrent: 0,
    temperature: 0,
    supplyVoltage: 0,
    vccVoltage: 0,
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Z21 Version Info
  // ─────────────────────────────────────────────────────────────────────────
  versionInfo: VersionInfo = {
    serialNumber: '-',
    firmwareVersion: '-',
    hardwareType: '-',
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Track Statistics (matches Statistics collection in WebAppViewModel)
  // ─────────────────────────────────────────────────────────────────────────
  statistics: TrackStatistic[] = []

  // ─────────────────────────────────────────────────────────────────────────
  // Lap Counter Settings
  // ─────────────────────────────────────────────────────────────────────────
  lapCounterSettings: LapCounterSettings = {
    countOfFeedbackPoints: 3,
    globalTargetLapCount: 10,
    useTimerFilter: false,
    timerIntervalSeconds: 2.0,
  }

  // ─────────────────────────────────────────────────────────────────────────
  // Legacy Data (Trains/Journeys)
  // ─────────────────────────────────────────────────────────────────────────
  trains: Train[] = []
  journeys: Journey[] = []
  selectedTrainId: number | null = null
  selectedJourneyId: number | null = null

  // ─────────────────────────────────────────────────────────────────────────
  // Loading States
  // ─────────────────────────────────────────────────────────────────────────
  isLoading = false
  pollingInterval: ReturnType<typeof setInterval> | null = null

  constructor() {
    makeAutoObservable(this)
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // COMPUTED PROPERTIES
  // ═══════════════════════════════════════════════════════════════════════════

  get selectedTrain(): Train | undefined {
    return this.trains.find((t) => t.id === this.selectedTrainId)
  }

  get selectedJourney(): Journey | undefined {
    return this.journeys.find((j) => j.id === this.selectedJourneyId)
  }

  get trainCount(): number {
    return this.trains.length
  }

  get journeyCount(): number {
    return this.journeys.length
  }

  /** Formatted supply voltage in V */
  get supplyVoltageFormatted(): string {
    return (this.systemState.supplyVoltage / 1000.0).toFixed(1)
  }

  /** Formatted VCC voltage in V */
  get vccVoltageFormatted(): string {
    return (this.systemState.vccVoltage / 1000.0).toFixed(1)
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Z21 CONNECTION ACTIONS
  // ═══════════════════════════════════════════════════════════════════════════

  async connect(): Promise<void> {
    this.isLoading = true
    this.connectionError = null
    this.statusText = 'Connecting...'

    try {
      const response = await api.post('/api/z21/connect', { ipAddress: this.ipAddress })
      runInAction(() => {
        this.isConnected = response.data.success
        this.statusText = response.data.success ? 'Connected' : 'Connection failed'
        if (!response.data.success) {
          this.connectionError = response.data.error
        } else {
          this.startPolling()
        }
      })
    } catch (error) {
      runInAction(() => {
        this.isConnected = false
        this.statusText = 'Connection failed'
        this.connectionError = error instanceof Error ? error.message : 'Verbindungsfehler'
      })
    } finally {
      runInAction(() => {
        this.isLoading = false
      })
    }
  }

  async disconnect(): Promise<void> {
    this.stopPolling()
    try {
      await api.post('/api/z21/disconnect')
      runInAction(() => {
        this.isConnected = false
        this.statusText = 'Disconnected'
      })
    } catch (error) {
      console.error('Disconnect error:', error)
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // SYSTEM STATE ACTIONS
  // ═══════════════════════════════════════════════════════════════════════════

  async fetchSystemState(): Promise<void> {
    try {
      const response = await api.get<{
        isConnected: boolean
        isTrackPowerOn: boolean
        mainCurrent: number
        temperature: number
        supplyVoltage: number
        vccVoltage: number
        serialNumber: string
        firmwareVersion: string
        hardwareType: string
      }>('/api/z21/status')

      runInAction(() => {
        this.isConnected = response.data.isConnected
        this.systemState = {
          isTrackPowerOn: response.data.isTrackPowerOn,
          isEmergencyStop: false,
          mainCurrent: response.data.mainCurrent,
          temperature: response.data.temperature,
          supplyVoltage: response.data.supplyVoltage,
          vccVoltage: response.data.vccVoltage,
        }
        this.versionInfo = {
          serialNumber: response.data.serialNumber,
          firmwareVersion: response.data.firmwareVersion,
          hardwareType: response.data.hardwareType,
        }
      })
    } catch (error) {
      console.error('Failed to fetch system state:', error)
    }
  }

  async toggleTrackPower(): Promise<void> {
    const newState = !this.systemState.isTrackPowerOn
    try {
      await api.post('/api/z21/track-power', { on: newState })
      runInAction(() => {
        this.systemState.isTrackPowerOn = newState
      })
    } catch (error) {
      console.error('Track power error:', error)
    }
  }

  async emergencyStop(): Promise<void> {
    try {
      await api.post('/api/z21/emergency-stop')
      runInAction(() => {
        this.systemState.isEmergencyStop = true
        this.trains.forEach((train) => (train.speed = 0))
      })
    } catch (error) {
      console.error('Emergency stop error:', error)
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // TRACK STATISTICS ACTIONS
  // ═══════════════════════════════════════════════════════════════════════════

  async fetchStatistics(): Promise<void> {
    try {
      const response = await api.get<TrackStatistic[]>('/api/statistics')
      runInAction(() => {
        this.statistics = response.data
      })
    } catch (error) {
      console.error('Failed to fetch statistics:', error)
    }
  }

  async fetchLapCounterSettings(): Promise<void> {
    try {
      const response = await api.get<LapCounterSettings>('/api/statistics/settings')
      runInAction(() => {
        this.lapCounterSettings = response.data
      })
    } catch (error) {
      console.error('Failed to fetch lap counter settings:', error)
    }
  }

  async resetCounters(): Promise<void> {
    try {
      await api.post('/api/statistics/reset')
      runInAction(() => {
        this.statistics = this.statistics.map((stat) => ({
          ...stat,
          count: 0,
          lastLapTime: null,
          lastFeedbackTime: null,
          hasReceivedFirstLap: false,
          progress: 0,
          lapCountFormatted: `0/${stat.targetLapCount} laps`,
        }))
      })
    } catch (error) {
      console.error('Reset counters error:', error)
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // POLLING (simulates real-time updates like Blazor SignalR)
  // ═══════════════════════════════════════════════════════════════════════════

  startPolling(): void {
    if (this.pollingInterval) return

    // Poll every 2 seconds (similar to Z21 polling interval)
    this.pollingInterval = setInterval(() => {
      if (this.isConnected) {
        this.fetchSystemState()
        this.fetchStatistics()
      }
    }, 2000)

    // Initial fetch
    this.fetchSystemState()
    this.fetchStatistics()
    this.fetchLapCounterSettings()
  }

  stopPolling(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval)
      this.pollingInterval = null
    }
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // LEGACY ACTIONS (Trains/Journeys)
  // ═══════════════════════════════════════════════════════════════════════════

  async fetchTrains(): Promise<void> {
    this.isLoading = true
    try {
      const response = await api.get<Train[]>('/api/trains')
      runInAction(() => {
        this.trains = response.data
      })
    } catch (error) {
      console.error('Failed to fetch trains:', error)
    } finally {
      runInAction(() => {
        this.isLoading = false
      })
    }
  }

  async fetchJourneys(): Promise<void> {
    this.isLoading = true
    try {
      const response = await api.get<Journey[]>('/api/journeys')
      runInAction(() => {
        this.journeys = response.data
      })
    } catch (error) {
      console.error('Failed to fetch journeys:', error)
    } finally {
      runInAction(() => {
        this.isLoading = false
      })
    }
  }

  setTrainSpeed(trainId: number, speed: number): void {
    const train = this.trains.find((t) => t.id === trainId)
    if (train) {
      train.speed = Math.max(0, Math.min(126, speed))
      api.post(`/api/trains/${trainId}/speed`, { speed: train.speed }).catch(console.error)
    }
  }

  selectTrain(trainId: number | null): void {
    this.selectedTrainId = trainId
  }

  selectJourney(journeyId: number | null): void {
    this.selectedJourneyId = journeyId
  }

  setIpAddress(ip: string): void {
    this.ipAddress = ip
  }
}
