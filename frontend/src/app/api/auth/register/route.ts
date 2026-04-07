import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { appendBackendSetCookieHeader } from "@/lib/server/cookies";

/**
 * Frontend BFF registration route.
 *
 * Behavior:
 * - Accepts browser form POST
 * - Forwards JSON to backend registration endpoint
 * - On success:
 *   - forwards backend auth cookie
 *   - redirects to /dashboard
 * - On failure:
 *   - redirects back to /register with a friendly error code
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const formData = await request.formData();

  const payload = {
    displayName: String(formData.get("displayName") ?? ""),
    tenantName: String(formData.get("tenantName") ?? ""),
    email: String(formData.get("email") ?? ""),
    password: String(formData.get("password") ?? ""),
  };

  const backendResponse = await fetch(buildBackendApiUrl("/api/auth/register"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    cache: "no-store",
    body: JSON.stringify(payload),
  });

  if (!backendResponse.ok) {
    const responseText = await backendResponse.text();

    let errorCode = "registration_failed";

    // Matches the current backend behavior where duplicate email returns 400
    // with a message containing this text.
    if (responseText.includes("already exists")) {
      errorCode = "email_exists";
    } else if (backendResponse.status === 400) {
      errorCode = "invalid_input";
    }

    const redirectUrl = new URL("/register", request.url);
    redirectUrl.searchParams.set("error", errorCode);

    return NextResponse.redirect(redirectUrl, { status: 303 });
  }

  const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
    status: 303,
  });

  appendBackendSetCookieHeader(
    nextResponse,
    backendResponse.headers.get("set-cookie")
  );

  return nextResponse;
}