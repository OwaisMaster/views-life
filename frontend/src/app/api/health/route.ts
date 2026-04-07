import { NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";

/**
 * Frontend BFF health route.
 * Forwards health requests to the ASP.NET backend.
 */
export async function GET(): Promise<NextResponse> {
  const backendResponse = await fetch(buildBackendApiUrl("/api/health"), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
    cache: "no-store",
  });

  const responseText = await backendResponse.text();

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
    },
  });
}