export interface JwtTokenDecoded {
    sub: string
    jti: string
    email: string
    uid: string
    ip: string
    roles: string[]
    exp: number
    iss: string
    aud: string
    users?: string
    roleclaims?: string
    [key: string]: any
  }
  