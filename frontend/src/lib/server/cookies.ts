import { NextResponse } from "next/server";

/**
 * Forwards the backend Set-Cookie header directly to the browser response.
 *
 * @param response Outgoing Next.js response
 * @param setCookieHeader Raw Set-Cookie header from backend
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