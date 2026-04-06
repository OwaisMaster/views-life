import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { appendBackendSetCookieHeader } from "@/lib/server/cookies";

/**
 * Frontend BFF development sign-in route.
 *
 * Behavior:
 * - Accepts a browser form POST
 * - Forwards the sign-in request to the ASP.NET backend
 * - Forwards the backend Set-Cookie header directly to the browser
 * - Redirects the browser to /dashboard
 *
 * This preserves the backend auth cookie exactly and avoids cookie-shape drift.
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const formData = await request.formData();

  const payload = {
    displayName: String(formData.get("displayName") ?? ""),
    email: String(formData.get("email") ?? ""),
    authProvider: String(formData.get("authProvider") ?? ""),
    providerSubjectId: String(formData.get("providerSubjectId") ?? ""),
  };

  const backendResponse = await fetch(
    buildBackendApiUrl("/api/auth/dev-sign-in"),
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      cache: "no-store",
      body: JSON.stringify(payload),
    }
  );

  if (!backendResponse.ok) {
    const errorText = await backendResponse.text();

    return new NextResponse(errorText || "Dev sign-in failed.", {
      status: backendResponse.status,
      headers: {
        "Content-Type": "text/plain",
        "Cache-Control": "no-store",
      },
    });
  }

  const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
    status: 303,
  });

  const setCookieHeader = backendResponse.headers.get("set-cookie");

  appendBackendSetCookieHeader(nextResponse, setCookieHeader);

  nextResponse.headers.set("Cache-Control", "no-store");

  return nextResponse;
}