import Link from "next/link";
import { cookies } from "next/headers";
import { getCurrentUserOrRedirect } from "@/lib/server/auth";
import { buildFrontendApiUrl } from "@/lib/api";
import type { NoteResponse } from "@/lib/notes";

// Helper function to extract plain text from Draft.js JSON content
function getPlainTextFromDraftContent(content: string): string {
  try {
    const parsed = JSON.parse(content);
    if (parsed.blocks && Array.isArray(parsed.blocks)) {
      return parsed.blocks
        .map((block: { text: string }) => block.text || '')
        .join(' ')
        .trim();
    }
  } catch {
    // If parsing fails, return the content as-is (might be plain text)
  }
  return content;
}

async function fetchNotes(cookieHeader: string): Promise<NoteResponse[]> {
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

  return response.json();
}

export default async function NotesPage() {
  const currentUser = await getCurrentUserOrRedirect();
  const cookieHeader = (await cookies()).toString();
  const notes = await fetchNotes(cookieHeader);

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h1 className="text-3xl font-bold">Notes</h1>
            <p className="text-sm text-gray-600">Tenant-scoped notes for {currentUser.tenantName}</p>
          </div>
          <Link href="/dashboard/notes" className="text-sm text-blue-600 hover:underline">
            Refresh
          </Link>
        </div>
      </header>

      <section className="grid gap-6 lg:grid-cols-[1.5fr_1fr]">
        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-lg font-semibold">Your notes</h2>
          {notes.length === 0 ? (
            <p className="text-sm text-gray-600">No notes yet. Create one with the form on the right.</p>
          ) : (
            <div className="space-y-4">
              {notes.map((note) => (
                <article key={note.id} className="rounded-xl border p-4 hover:shadow-md transition-shadow">
                  <h3 className="text-lg font-semibold">{note.title}</h3>
                  <p className="mt-2 text-sm text-gray-700 line-clamp-3">{getPlainTextFromDraftContent(note.content)}</p>
                  <div className="mt-3 flex items-center justify-between text-xs text-gray-500">
                    <div className="flex flex-col gap-1">
                      <span>By {note.createdByDisplayName}</span>
                      <span>{new Date(note.createdAtUtc).toLocaleDateString()}</span>
                    </div>
                    <div className="flex flex-col items-end gap-1">
                      <span className={`px-2 py-1 rounded-full text-xs ${
                        note.visibility === 'Private' 
                          ? 'bg-gray-100 text-gray-600' 
                          : 'bg-blue-100 text-blue-600'
                      }`}>
                        {note.visibility}
                      </span>
                      <Link href={`/dashboard/notes/${note.id}`} className="text-blue-600 hover:underline">
                        View details →
                      </Link>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </div>

        <div className="rounded-xl border p-6">
          <h2 className="mb-4 text-lg font-semibold">Create a note</h2>
          <form action="/api/notes" method="post" className="space-y-4">
            <div>
              <label htmlFor="title" className="mb-2 block text-sm font-medium text-gray-700">
                Title
              </label>
              <input
                id="title"
                name="title"
                type="text"
                maxLength={200}
                required
                className="w-full rounded-md border px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label htmlFor="content" className="mb-2 block text-sm font-medium text-gray-700">
                Content
              </label>
              <textarea
                id="content"
                name="content"
                rows={6}
                maxLength={5000}
                required
                className="w-full rounded-md border px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label htmlFor="visibility" className="mb-2 block text-sm font-medium text-gray-700">
                Visibility
              </label>
              <select
                id="visibility"
                name="visibility"
                defaultValue="Private"
                className="w-full rounded-md border px-3 py-2 text-sm"
              >
                <option value="Private">Private</option>
                <option value="Shared">Shared</option>
              </select>
            </div>
            <button
              type="submit"
              className="inline-flex items-center justify-center rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
            >
              Create note
            </button>
          </form>
        </div>
      </section>
    </div>
  );
}
