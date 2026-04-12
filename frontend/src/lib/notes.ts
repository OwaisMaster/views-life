import { buildFrontendApiUrl } from "@/lib/api";

export interface NoteResponse {
  id: string;
  tenantId: string;
  createdByUserId: string;
  createdByDisplayName: string;
  title: string;
  content: string;
  visibility: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface NoteFormPayload {
  title: string;
  content: string;
  visibility?: string;
}

export async function fetchNotes(cookieHeader?: string): Promise<NoteResponse[]> {
  const response = await fetch(buildFrontendApiUrl("/api/notes"), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Failed to load notes: ${response.status}`);
  }

  return (await response.json()) as NoteResponse[];
}

export async function fetchNoteById(
  noteId: string,
  cookieHeader?: string
): Promise<NoteResponse> {
  const response = await fetch(buildFrontendApiUrl(`/api/notes/${noteId}`), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Failed to load note ${noteId}: ${response.status}`);
  }

  return (await response.json()) as NoteResponse;
}
