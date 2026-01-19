import React, { createContext, useContext, ReactNode } from 'react'
import { AppStore } from './AppStore'

interface StoreContextType {
  appStore: AppStore
}

const StoreContext = createContext<StoreContextType | null>(null)

// Create singleton instance
const appStore = new AppStore()

interface StoreProviderProps {
  children: ReactNode
}

export const StoreProvider: React.FC<StoreProviderProps> = ({ children }) => {
  return (
    <StoreContext.Provider value={{ appStore }}>
      {children}
    </StoreContext.Provider>
  )
}

export const useStore = (): StoreContextType => {
  const context = useContext(StoreContext)
  if (!context) {
    throw new Error('useStore must be used within a StoreProvider')
  }
  return context
}
