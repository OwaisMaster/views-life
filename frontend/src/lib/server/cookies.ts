import { NextResponse } from "next/server";

/**
 * Appends the backend Set-Cookie header directly to the outgoing Next.js response.
 *
 * Context:
 * - This preserves the cookie exactly as the ASP.NET backend emitted it.
 * - That is safer than reconstructing the cookie manually for auth flows.
 *
 * @param response Outgoing Next.js response
 * @param setCookieHeader Raw Set-Cookie header from the backend response
 */
export function appendBackendSetCookieHeader(
  response: NextResponse,
  setCookieHeader: string | null
): void {
  if (!setCookieHeader) {
    return;
  }

  response.headers.append("Set-Cookie", setCookieHeader);
}

/**
 * Clears the auth cookie from the outgoing Next.js response.
 *
 * @param response Outgoing Next.js response
 * @param cookieName Cookie name to clear
 */
export function clearCookieFromResponse(
  response: NextResponse,
  cookieName: string
): void {
  response.cookies.set({
    name: cookieName,
    value: "",
    httpOnly: true,
    secure: true,
    sameSite: "lax",
    path: "/",
    expires: new Date(0),
  });
}