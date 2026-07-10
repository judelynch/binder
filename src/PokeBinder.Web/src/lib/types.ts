export interface AuthUser {
  userId: string
  email: string
  roles: string[]
}

export interface AuthResponse {
  token: string
  userId: string
  email: string
  roles: string[]
}
