import axios from 'axios'
import { getToken } from '../auth'

const api = axios.create({
  baseURL: 'http://localhost:5000/api/Announcements'
})

api.interceptors.request.use(config => {
  const token = getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export const announcementApi = {
  // Kullanıcılar için aktif duyurular
  getActive() {
    return api.get('/')
  },
  
  // Admin için tüm duyurular
  getAllAdmin() {
    return api.get('/admin')
  },
  
  create(data) {
    return api.post('/', data)
  },
  
  update(id, data) {
    return api.put(`/${id}`, data)
  },
  
  toggleStatus(id) {
    return api.put(`/${id}/status`)
  },
  
  delete(id) {
    return api.delete(`/${id}`)
  }
}
