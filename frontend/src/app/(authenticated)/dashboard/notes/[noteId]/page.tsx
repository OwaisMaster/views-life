import Link from "next/link";
import { cookies } from "next/headers";
import { notFound } from "next/navigation";
import { getCurrentUserOrRedirect } from "@/lib/server/auth";
import { buildFrontendApiUrl } from "@/lib/api";
import type { NoteResponse } from "@/lib/notes";
import { NoteContent } from "./NoteContent";

async function fetchNote(noteId: string, cookieHeader: string): Promise<NoteResponse> {
  const response = await fetch(buildFrontendApiUrl(`/api/notes/${noteId}`), {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    throw new Error(`Failed to load note: ${response.status}`);
  }

  return response.json();
}

export default async function NoteDetailPage({ params }: { params: Promise<{ noteId: string }> }) {
  await getCurrentUserOrRedirect();

  const { noteId } = await params;
  const cookieHeader = (await cookies()).toString();
  let note: NoteResponse;

  try {
    note = await fetchNote(noteId, cookieHeader);
  } catch {
    notFound();
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Link
          href="/dashboard/notes"
          className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
        >
          ← Back to Notes
        </Link>
      </div>

      <header className="space-y-2">
        <h1 className="text-3xl font-bold">{note.title}</h1>
        <p className="text-sm text-gray-600">Tenant-scoped note details</p>
      </header>

      <NoteContent note={note} />
    </div>
  );
}
