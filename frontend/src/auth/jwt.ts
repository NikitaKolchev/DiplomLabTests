type JwtPayload = {
  sub?: string;
  unique_name?: string;
  name?: string;
  role?: string;
  exp?: number;
  [key: string]: unknown;
};

export function parseJwt(token: string): JwtPayload | null {
  try {
    const payload = token.split(".")[1];
    if (!payload) return null;
    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized + "=".repeat((4 - (normalized.length % 4)) % 4);
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (char) => char.charCodeAt(0));
    const json = new TextDecoder("utf-8").decode(bytes);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

export function extractRole(token: string): string | null {
  const payload = parseJwt(token);
  if (!payload) return null;
  const role = payload.role ?? payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  return typeof role === "string" ? role : null;
}

export function extractName(token: string): string | null {
  const payload = parseJwt(token);
  if (!payload) return null;
  return typeof payload.name === "string" ? payload.name : null;
}
