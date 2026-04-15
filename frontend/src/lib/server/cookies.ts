// import { NextResponse } from "next/server";

// /**
//  * Forwards the backend Set-Cookie header directly to the browser response.
//  *
//  * @param response Outgoing Next.js response
//  * @param setCookieHeader Raw Set-Cookie header from backend
//  */
// export function appendBackendSetCookieHeader(
//   response: NextResponse,
//   setCookieHeader: string | null
// ): void {
//   if (!setCookieHeader) {
//     return;
//   }

//   response.headers.append("Set-Cookie", setCookieHeader);
// }

// /**
//  * Clears the auth cookie from the outgoing Next.js response.
//  *
//  * @param response Outgoing Next.js response
//  * @param cookieName Cookie name to clear
//  */
// export function clearCookieFromResponse(
//   response: NextResponse,
//   cookieName: string
// ): void {
//   response.cookies.set({
//     name: cookieName,
//     value: "",
//     httpOnly: true,
//     secure: true,
//     sameSite: "lax",
//     path: "/",
//     expires: new Date(0),
//   });
// }
//--------------------------------------
import { NextResponse } from "next/server";

/**
 * Forwards backend Set-Cookie headers directly to the browser response.
 *
 * Context:
 * - A backend response can contain multiple Set-Cookie headers.
 * - These must be forwarded as separate Set-Cookie headers.
 * - Do not combine them into one comma-joined string.
 *
 * @param response Outgoing Next.js response
 * @param setCookieHeaders Raw Set-Cookie header values from backend
 */
export function appendBackendSetCookieHeaders(
  response: NextResponse,
  setCookieHeaders: string[]
): void {
  for (const headerValue of setCookieHeaders) {
    response.headers.append("Set-Cookie", headerValue);
  }
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