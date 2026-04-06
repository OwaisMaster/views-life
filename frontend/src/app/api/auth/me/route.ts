import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";

/**
 * Frontend BFF current-user route.
 * Forwards the incoming browser cookies to the backend so cookie authentication
 * can be validated there.
 */
export async function GET(): Promise<NextResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const backendResponse = await fetch(buildBackendApiUrl("/api/auth/me"), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  const responseText = await backendResponse.text();

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}