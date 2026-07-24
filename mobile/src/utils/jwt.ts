/**
 * Minimal JWT payload decoding.
 *
 * The access token payload is not secret (it is base64url, not encrypted), so
 * decoding it client-side to read the signed-in user's identity is safe. We
 * never trust it for authorization — the server re-validates every request.
 */

export type TokenClaims = {
  userId?: string;
  email?: string;
  exp?: number;
};

function base64UrlDecode(input: string): string {
  const padded = input.replace(/-/g, "+").replace(/_/g, "/");
  const withPadding = padded.padEnd(
    padded.length + ((4 - (padded.length % 4)) % 4),
    "=",
  );

  if (typeof atob === "function") {
    return atob(withPadding);
  }
  // Node / test environments
  return Buffer.from(withPadding, "base64").toString("binary");
}

export function decodeTokenClaims(token: string | null | undefined): TokenClaims | null {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;

  try {
    const json = base64UrlDecode(parts[1]);
    const claims = JSON.parse(json) as Record<string, unknown>;
    return {
      userId:
        (claims.userId as string | undefined) ??
        (claims.nameid as string | undefined) ??
        (claims.sub as string | undefined),
      email:
        (claims.email as string | undefined) ??
        (claims[
          "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
        ] as string | undefined),
      exp: typeof claims.exp === "number" ? (claims.exp as number) : undefined,
    };
  } catch {
    return null;
  }
}
