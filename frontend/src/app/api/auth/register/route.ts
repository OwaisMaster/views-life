import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { appendBackendSetCookieHeaders } from "@/lib/server/cookies";

/**
 * Frontend BFF register route.
 *
 * Context:
 * - Forwards the browser form submission to the backend register endpoint.
 * - On success, forwards all backend Set-Cookie headers to the browser.
 * - Redirects the user into the authenticated area.
 *
 * Dependency:
 * - Requires a runtime that supports fetch Response headers.
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const formData = await request.formData();

  const payload = {
    email: String(formData.get("email") ?? ""),
    password: String(formData.get("password") ?? ""),
    displayName: String(formData.get("displayName") ?? ""),
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
    const redirectUrl = new URL("/register", request.url);
    redirectUrl.searchParams.set("error", "registration_failed");

    return NextResponse.redirect(redirectUrl, { status: 303 });
  }

  // Extract all Set-Cookie headers safely from the backend response.
  // Some runtimes expose getSetCookie(); fallback keeps compatibility.
  const setCookieHeaders =
    typeof backendResponse.headers.getSetCookie === "function"
      ? backendResponse.headers.getSetCookie()
      : (() => {
          const singleHeader = backendResponse.headers.get("set-cookie");

          return singleHeader ? [singleHeader] : [];
        })();

  const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
    status: 303,
  });

  appendBackendSetCookieHeaders(nextResponse, setCookieHeaders);

  return nextResponse;
}