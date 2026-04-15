// import { cookies } from "next/headers";
// import { NextResponse } from "next/server";
// import { buildBackendApiUrl } from "@/lib/api";

// /**
//  * Frontend BFF current-user route.
//  * Forwards the incoming browser cookies to the backend so cookie authentication
//  * can be validated there.
//  */
// export async function GET(): Promise<NextResponse> {
//   const cookieStore = await cookies();
//   const cookieHeader = cookieStore.toString();

//   const backendResponse = await fetch(buildBackendApiUrl("/api/auth/me"), {
//     method: "GET",
//     headers: {
//       "Content-Type": "application/json",
//       ...(cookieHeader ? { Cookie: cookieHeader } : {}),
//     },
//     cache: "no-store",
//   });

//   const responseText = await backendResponse.text();

//   return new NextResponse(responseText, {
//     status: backendResponse.status,
//     headers: {
//       "Content-Type": "application/json",
//       "Cache-Control": "no-store",
//     },
//   });
// }
//-------------------------------------
import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import {
  logAuthDebug,
  summarizeCookieHeader,
} from "@/lib/server/auth-debug";

/**
 * Frontend BFF current-user route.
 *
 * Behavior:
 * - Reads the incoming browser cookies on the Vercel side
 * - Forwards them to the backend /api/auth/me endpoint
 * - Returns the backend JSON response and status as-is
 *
 * Diagnostic behavior:
 * - Logs cookie presence from the incoming request
 * - Logs the exact backend URL being called
 * - Logs backend status and a safe response preview
 */
export async function GET(): Promise<NextResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();
  const cookieSummary = summarizeCookieHeader(cookieHeader);
  const backendUrl = buildBackendApiUrl("/api/auth/me");

  logAuthDebug("bff_me_request_start", {
    backendUrl,
    ...cookieSummary,
  });

  const backendResponse = await fetch(backendUrl, {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  const responseText = await backendResponse.text();

  logAuthDebug("bff_me_response", {
    backendUrl,
    ...cookieSummary,
    backendStatus: backendResponse.status,
    backendOk: backendResponse.ok,
    responsePreview: responseText.slice(0, 500),
  });

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}