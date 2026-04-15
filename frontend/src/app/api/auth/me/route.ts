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
import { buildBackendApiUrl } from "@/lib/api";
import { createHash } from "node:crypto";

/**
 * Computes a SHA-256 hash for a string.
 */
function sha256(value: string): string {
  return createHash("sha256").update(value).digest("hex");
}

export async function GET(request: NextRequest): Promise<NextResponse> {
  const cookieHeader = request.headers.get("cookie") ?? "";
  const backendUrl = buildBackendApiUrl("/api/auth/me");

  console.info(
    JSON.stringify({
      scope: "auth-debug",
      event: "bff_me_request_start",
      timestamp: new Date().toISOString(),
      backendUrl,
      hasCookie: cookieHeader.length > 0,
      cookieHeaderLength: cookieHeader.length,
      cookieHeaderHash: sha256(cookieHeader),
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