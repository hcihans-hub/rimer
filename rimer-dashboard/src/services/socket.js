import { store } from '../store'

let socket = null
let messageCallback = null
let closeCallback = null
let reconnectTimer = null
let retryDelay = 3000
let isIntentionallyClosed = false
let lastUpdate = 0

export let isConnected = false

const normalizeMode = (m) => {
  if (!m) return 'NORMAL'
  if (m === 'HIGH_LOAD')  return 'HIGH LOAD'
  if (m === 'PROTECTION') return 'PROTECTION MODE'
  return m
}

export const connect = () => {
  isIntentionallyClosed = false
  if (socket) socket.close()

  socket = new WebSocket('ws://localhost:5000/ws')

  socket.onopen = () => {
    clearTimeout(reconnectTimer)
    retryDelay = 3000
    isConnected = true
    console.log('[WS] connected')
  }

  let pendingData = null

  socket.onmessage = (event) => {
    try {
      const msg = JSON.parse(event.data)
      if (!msg?.type || !msg?.data) return

      switch (msg.type) {

        case 'metrics_update': {
          const { queueDepth, throughput, droppedRequests, systemMode, totalRequests } = msg.data
          if (typeof queueDepth !== 'number' || typeof throughput !== 'number' || typeof droppedRequests !== 'number') return

          pendingData = {
            ...msg.data,
            // Normalize mode inline so store always gets clean values
            systemMode: normalizeMode(systemMode)
          }

          const now = Date.now()
          if (now - lastUpdate < 500) return
          lastUpdate = now

          if (pendingData && messageCallback) {
            messageCallback(pendingData)
            pendingData = null
          }
          break
        }

        case 'alert_trigger': {
          const { id, level, message } = msg.data
          if (typeof id !== 'string' || typeof message !== 'string') return
          store.addAlert({ id, level, message })
          break
        }

        case 'alert_clear': {
          const { id } = msg.data
          if (typeof id === 'string') store.removeAlert(id)
          break
        }

        case 'system_mode_change': {
          const { mode } = msg.data
          if (!mode) return
          // Update store directly — protectionMode computed getter derives from this
          store.updateMetrics({ systemMode: normalizeMode(mode) })
          break
        }
      }

    } catch (err) {
      if (import.meta.env.DEV) console.warn('[WS] parse error:', err)
    }
  }

  socket.onclose = () => {
    isConnected = false
    console.log('[WS] disconnected')
    if (closeCallback) closeCallback()
    if (!isIntentionallyClosed) {
      reconnectTimer = setTimeout(() => connect(), retryDelay)
      retryDelay = Math.min(retryDelay * 2, 30000)
    }
  }

  socket.onerror = () => socket.close()
}

export const onMessage = (cb) => { messageCallback = cb }
export const onClose   = (cb) => { closeCallback   = cb }

export const disconnect = () => {
  isIntentionallyClosed = true
  clearTimeout(reconnectTimer)
  if (socket) {
    socket.onclose = null
    socket.close()
    socket = null
  }
  retryDelay = 3000
  isConnected = false
}

export default {
  connect,
  onMessage,
  onClose,
  disconnect,
  get isConnected() { return isConnected }
}