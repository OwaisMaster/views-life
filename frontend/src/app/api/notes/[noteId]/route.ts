import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";

export async function GET(
  _request: NextRequest,
  context: any
): Promise<NextResponse> {
  const { noteId } = await context.params;
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const backendResponse = await fetch(
    buildBackendApiUrl(`/api/notes/${noteId}`),
    {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
        ...(cookieHeader ? { Cookie: cookieHeader } : {}),
      },
      cache: "no-store",
    }
  );

  const responseText = await backendResponse.text();

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}

export async function PUT(
  request: NextRequest,
  context: any
): Promise<NextResponse> {
  const { noteId } = await context.params;
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const payload = await request.json();

  const backendResponse = await fetch(
    buildBackendApiUrl(`/api/notes/${noteId}`),
    {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        ...(cookieHeader ? { Cookie: cookieHeader } : {}),
      },
      cache: "no-store",
      body: JSON.stringify(payload),
    }
  );

  const responseText = await backendResponse.text();

  return new NextResponse(responseText, {
    status: backendResponse.status,
    headers: {
      "Content-Type": "application/json",
      "Cache-Control": "no-store",
    },
  });
}

export async function DELETE(
  _request: NextRequest,
  context: any
): Promise<NextResponse> {
  const { noteId } = await context.params;
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const backendResponse = await fetch(
    buildBackendApiUrl(`/api/notes/${noteId}`),
    {
      method: "DELETE",
      headers: {
        "Content-Type": "application/json",
        ...(cookieHeader ? { Cookie: cookieHeader } : {}),
      },
      cache: "no-store",
    }
  );

  return new NextResponse(null, {
    status: backendResponse.status,
    headers: {
      "Cache-Control": "no-store",
    },
  });
}
