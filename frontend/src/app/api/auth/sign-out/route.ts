import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { clearCookieFromResponse } from "@/lib/server/cookies";

/**
 * Frontend BFF sign-out route.
 *
 * Behavior:
 * - Forwards the current auth cookie to the backend sign-out endpoint
 * - Clears the auth cookie from the outgoing redirect response
 * - Redirects the browser back to the public homepage
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  await fetch(buildBackendApiUrl("/api/auth/sign-out"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  const nextResponse = NextResponse.redirect(new URL("/", request.url), {
    status: 303,
  });

  clearCookieFromResponse(nextResponse, "viewslife_auth");
  nextResponse.headers.set("Cache-Control", "no-store");

  return nextResponse;
}