import axios from 'axios'
import { getToken } from '../auth'

const http = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 10000,
})

http.interceptors.request.use(cfg => {
  const token = getToken()
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

export const notificationApi = {
  getMyNotifications: async () => {
    const res = await http.get('/Notifications')
    return res.data
  },
  getUnreadCount: async () => {
    const res = await http.get('/Notifications/unread-count')
    return res.data
  },
  markRead: async (id) => {
    const res = await http.post(`/Notifications/${id}/read`)
    return res.data
  },
  markAllRead: async () => {
    const res = await http.post('/Notifications/read-all')
    return res.data
  }
}
