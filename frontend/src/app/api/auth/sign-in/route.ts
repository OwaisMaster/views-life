import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { appendBackendSetCookieHeader } from "@/lib/server/cookies";

/**
 * Frontend BFF sign-in route.
 *
 * Behavior:
 * - Accepts browser form POST
 * - Forwards JSON to backend sign-in endpoint
 * - On success:
 *   - forwards backend auth cookie
 *   - redirects to /dashboard
 * - On failure:
 *   - redirects back to /sign-in with a friendly error code
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const formData = await request.formData();

  const payload = {
    email: String(formData.get("email") ?? ""),
    password: String(formData.get("password") ?? ""),
  };

  const backendResponse = await fetch(buildBackendApiUrl("/api/auth/sign-in"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    cache: "no-store",
    body: JSON.stringify(payload),
  });

  if (!backendResponse.ok) {
    const responseText = await backendResponse.text();

    let errorCode = "signin_failed";

    // Matches the current backend behavior for bad credentials.
    if (
      backendResponse.status === 401 ||
      responseText.includes("Invalid email or password")
    ) {
      errorCode = "invalid_credentials";
    } else if (backendResponse.status === 400) {
      errorCode = "invalid_input";
    }

    const redirectUrl = new URL("/sign-in", request.url);
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