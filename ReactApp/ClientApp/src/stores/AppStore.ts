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
  // Backing field may be set to non-array by API; getter always returns an array.
  // ─────────────────────────────────────────────────────────────────────────
  private _statistics: unknown = []

  get statistics(): TrackStatistic[] {
    return Array.isArray(this._statistics) ? (this._statistics as TrackStatistic[]) : []
  }

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
    const v = this.systemState?.supplyVoltage
    const n = typeof v === 'number' && !Number.isNaN(v) ? v : 0
    return (n / 1000.0).toFixed(1)
  }

  /** Formatted VCC voltage in V */
  get vccVoltageFormatted(): string {
    const v = this.systemState?.vccVoltage
    const n = typeof v === 'number' && !Number.isNaN(v) ? v : 0
    return (n / 1000.0).toFixed(1)
  }

  // ═══════════════════════════════════════════════════════════════════════════
  // Z21 CONNECTION ACTIONS
  // ═══════════════════════════════════════════════════════════════════════════

  async connect(): Promise<void> {
    this.isLoading = true
    this.connectionError = null
    this.statusText = 'Connecting...'

    try {
      const response = await api.post<{ success: boolean; error?: string }>('/api/z21/connect', { ipAddress: this.ipAddress })
      const data = response?.data
      const success = Boolean(data?.success)
      runInAction(() => {
        try {
          this.isConnected = success
          this.statusText = success ? 'Connected' : 'Connection failed'
          if (!success) {
            this.connectionError = typeof data?.error === 'string' ? data.error : 'Connection failed'
          } else {
            this.startPolling()
          }
        } catch (e) {
          console.error('[AppStore] connect runInAction error:', e)
        }
      })
    } catch (error) {
      runInAction(() => {
        this.isConnected = false
        this.statusText = 'Connection failed'
        const msg = error instanceof Error ? error.message : 'Verbindungsfehler'
        const isAxios = typeof (error as { response?: { status?: number } })?.response?.status === 'number'
        const status = (error as { response?: { status?: number } })?.response?.status
        if (isAxios && status === 404) {
          this.connectionError = 'API nicht gefunden (404). Läuft das Backend unter ' + window.location.origin + '?'
        } else {
          this.connectionError = msg
        }
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

      const d = response.data
      if (!d || typeof d !== 'object') return

      runInAction(() => {
        try {
          this.isConnected = Boolean(d.isConnected)
          this.systemState = {
            isTrackPowerOn: Boolean(d.isTrackPowerOn),
            isEmergencyStop: false,
            mainCurrent: Number(d.mainCurrent) || 0,
            temperature: Number(d.temperature) || 0,
            supplyVoltage: Number(d.supplyVoltage) || 0,
            vccVoltage: Number(d.vccVoltage) || 0,
          }
          this.versionInfo = {
            serialNumber: String(d.serialNumber ?? '-'),
            firmwareVersion: String(d.firmwareVersion ?? '-'),
            hardwareType: String(d.hardwareType ?? '-'),
          }
        } catch (e) {
          console.error('[AppStore] fetchSystemState runInAction error:', e)
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
      const response = await api.get<TrackStatistic[] | unknown>('/api/statistics')
      const data = response.data
      if (!Array.isArray(data)) return
      runInAction(() => {
        try {
          this._statistics = data
        } catch (e) {
          console.error('[AppStore] fetchStatistics runInAction error:', e)
        }
      })
    } catch (error) {
      console.error('Failed to fetch statistics:', error)
    }
  }

  async fetchLapCounterSettings(): Promise<void> {
    try {
      const response = await api.get<LapCounterSettings>('/api/statistics/settings')
      const d = response.data
      if (!d || typeof d !== 'object') return
      runInAction(() => {
        try {
          this.lapCounterSettings = {
            countOfFeedbackPoints:
              typeof d.countOfFeedbackPoints === 'number' ? d.countOfFeedbackPoints : this.lapCounterSettings.countOfFeedbackPoints,
            globalTargetLapCount:
              typeof d.globalTargetLapCount === 'number' ? d.globalTargetLapCount : this.lapCounterSettings.globalTargetLapCount,
            useTimerFilter: Boolean(d.useTimerFilter),
            timerIntervalSeconds:
              typeof d.timerIntervalSeconds === 'number' ? d.timerIntervalSeconds : this.lapCounterSettings.timerIntervalSeconds,
          }
        } catch (e) {
          console.error('[AppStore] fetchLapCounterSettings runInAction error:', e)
        }
      })
    } catch (error) {
      console.error('Failed to fetch lap counter settings:', error)
    }
  }

  async resetCounters(): Promise<void> {
    try {
      await api.post('/api/statistics/reset')
      const current = this.statistics
      if (!Array.isArray(current)) return
      runInAction(() => {
        try {
          this._statistics = current.map((stat) => ({
            ...stat,
            count: 0,
            lastLapTime: null,
            lastFeedbackTime: null,
            hasReceivedFirstLap: false,
            progress: 0,
            lapCountFormatted: `0/${stat.targetLapCount} laps`,
          }))
        } catch (e) {
          console.error('[AppStore] resetCounters runInAction error:', e)
        }
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
