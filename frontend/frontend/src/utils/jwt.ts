export type JwtPayload = { sub?: string; email?: string; role?: string; exp?: number; iat?: number }

export function decodeJwt(token: string): JwtPayload | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64).split('').map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')
    )
    return JSON.parse(json) as JwtPayload
  } catch { return null }
}

export function isTokenExpired(token: string): boolean {
  const p = decodeJwt(token)
  if (!p?.exp) return false
  const now = Math.floor(Date.now() / 1000)
  return now >= p.exp
}
