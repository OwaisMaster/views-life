import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";

export async function GET(): Promise<NextResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const backendResponse = await fetch(buildBackendApiUrl("/api/notes"), {
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

export async function POST(request: NextRequest): Promise<NextResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const formData = await request.formData();

  const payload = {
    title: String(formData.get("title") ?? ""),
    content: String(formData.get("content") ?? ""),
    visibility: String(formData.get("visibility") ?? "Private"),
  };

  const backendResponse = await fetch(buildBackendApiUrl("/api/notes"), {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
    body: JSON.stringify(payload),
  });

  if (!backendResponse.ok) {
    const responseText = await backendResponse.text();
    const redirectUrl = new URL("/dashboard/notes", request.url);
    redirectUrl.searchParams.set("error", "invalid_input");
    redirectUrl.searchParams.set("message", responseText);

    return NextResponse.redirect(redirectUrl, { status: 303 });
  }

  return NextResponse.redirect(new URL("/dashboard/notes", request.url), {
    status: 303,
  });
}

