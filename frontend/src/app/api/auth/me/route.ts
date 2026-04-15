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
import { NextRequest, NextResponse } from "next/server";
import { createHash } from "node:crypto";
import { buildBackendApiUrl } from "@/lib/api";

/**
 * Computes a SHA-256 hash for a string.
 *
 * @param value Input string
 * @returns Lowercase SHA-256 hex string
 */
function sha256(value: string): string {
  return createHash("sha256").update(value).digest("hex");
}

/**
 * Extracts a named cookie value from a raw Cookie header.
 *
 * @param cookieHeader Raw Cookie header
 * @param cookieName Cookie name to extract
 * @returns Cookie value if found; otherwise null
 */
function extractCookieValue(
  cookieHeader: string,
  cookieName: string
): string | null {
  if (!cookieHeader) {
    return null;
  }

  const parts = cookieHeader
    .split(";")
    .map((part) => part.trim())
    .filter(Boolean);

  for (const part of parts) {
    const separatorIndex = part.indexOf("=");

    if (separatorIndex <= 0) {
      continue;
    }

    const name = part.slice(0, separatorIndex);
    const value = part.slice(separatorIndex + 1);

    if (name === cookieName) {
      return value;
    }
  }

  return null;
}

/**
 * Frontend BFF current-user route.
 *
 * Context:
 * - Reads the raw incoming cookie header from the browser request
 * - Logs only safe metadata and a hash of the auth cookie value
 * - Forwards the exact raw Cookie header to the backend
 */
export async function GET(request: NextRequest): Promise<NextResponse> {
  const cookieHeader = request.headers.get("cookie") ?? "";
  const authCookieValue = extractCookieValue(cookieHeader, "viewslife_auth");
  const backendUrl = buildBackendApiUrl("/api/auth/me");

  console.info(
    JSON.stringify({
      scope: "auth-debug",
      event: "bff_me_request_start",
      timestamp: new Date().toISOString(),
      backendUrl,
      hasCookie: cookieHeader.length > 0,
      cookieHeaderLength: cookieHeader.length,
      authCookieFound: Boolean(authCookieValue),
      authCookieValueLength: authCookieValue?.length ?? 0,
      authCookieValueHash: authCookieValue ? sha256(authCookieValue) : "empty",
    })
  );

  const backendResponse = await fetch(backendUrl, {
    method: "GET",
    headers: {
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  const responseText = await backendResponse.text();

  console.info(
    JSON.stringify({
      scope: "auth-debug",
      event: "bff_me_response",
      timestamp: new Date().toISOString(),
      backendUrl,
      status: backendResponse.status,
      ok: backendResponse.ok,
      responsePreview: responseText.slice(0, 300),
    })
  );

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}