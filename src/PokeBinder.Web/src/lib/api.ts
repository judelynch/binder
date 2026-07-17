import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  // Axios defaults to bracket-notation array params (subtypes[]=A&subtypes[]=B), but ASP.NET Core's
  // default model binding for string[] query params only recognizes repeated plain keys
  // (subtypes=A&subtypes=B). Without this, every multi-value filter is silently ignored server-side.
  paramsSerializer: { indexes: null },
})

export function setAuthToken(token: string | null) {
  if (token) {
    api.defaults.headers.common.Authorization = `Bearer ${token}`
  } else {
    delete api.defaults.headers.common.Authorization
  }
}
