import axios from 'axios'

/**
 * Axios instance configured for the MOBAflow REST API.
 * In development, Vite proxy handles /api requests.
 * In production, requests go to the same origin.
 */
export const api = axios.create({
  baseURL: '',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor for logging
api.interceptors.request.use(
  (config) => {
    console.debug(`[API] ${config.method?.toUpperCase()} ${config.url}`)
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response interceptor for error handling
api.interceptors.response.use(
  (response) => {
    return response
  },
  (error) => {
    if (error.response) {
      console.error(`[API Error] ${error.response.status}: ${error.response.data?.message || error.message}`)
    } else if (error.request) {
      console.error('[API Error] No response received:', error.message)
    } else {
      console.error('[API Error]', error.message)
    }
    return Promise.reject(error)
  }
)
