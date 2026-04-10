"use client";

import { useState, useRef, useEffect } from "react";
import { useRouter } from "next/navigation";
import DOMPurify from "dompurify";
import type { NoteResponse } from "@/lib/notes";

interface NoteContentProps {
  note: NoteResponse;
}

export function NoteContent({ note }: NoteContentProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    title: note.title,
    content: note.content,
    visibility: note.visibility,
  });
  const editorRef = useRef<HTMLDivElement>(null);
  const router = useRouter();

  const handleEdit = () => {
    setIsEditing(true);
  };

  const handleCancel = () => {
    setFormData({
      title: note.title,
      content: note.content,
      visibility: note.visibility,
    });
    setIsEditing(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      const response = await fetch(`/api/notes/${note.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(formData),
      });

      if (response.ok) {
        setIsEditing(false);
        router.refresh(); // Refresh the page to show updated data
      } else {
        console.error("Failed to update note");
      }
    } catch (error) {
      console.error("Error updating note:", error);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Toolbar command functions
  const execCommand = (command: string, value: string = "") => {
    document.execCommand(command, false, value);
    editorRef.current?.focus();
  };

  const handleImageUpload = () => {
    const url = prompt("Enter image URL:");
    if (url) {
      execCommand("insertImage", url);
    }
  };

  const handleLinkInsert = () => {
    const url = prompt("Enter URL:");
    if (url) {
      execCommand("createLink", url);
    }
  };

  // Handle content changes in the editor
  const handleContentChange = () => {
    if (editorRef.current) {
      setFormData(prev => ({
        ...prev,
        content: editorRef.current!.innerHTML
      }));
    }
  };

  // Set initial content when entering edit mode
  useEffect(() => {
    if (isEditing && editorRef.current) {
      editorRef.current.innerHTML = formData.content;
    }
  }, [isEditing, formData.content]);

  if (isEditing) {
    return (
      <section className="rounded-xl border p-6">
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-900">Edit Note</h2>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleCancel}
              className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
              disabled={isSubmitting}
            >
              Cancel
            </button>
            <button
              type="submit"
              form="edit-note-form"
              className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Saving..." : "Save Changes"}
            </button>
          </div>
        </div>

        <form id="edit-note-form" onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="title" className="block text-sm font-medium text-gray-700">
              Title
            </label>
            <input
              type="text"
              id="title"
              value={formData.title}
              onChange={(e) => setFormData({ ...formData, title: e.target.value })}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              required
              maxLength={200}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Content
            </label>
            {/* Rich Text Toolbar */}
            <div className="mb-2 flex flex-wrap gap-1 rounded-t-md border border-gray-300 bg-gray-50 p-2">
              <button
                type="button"
                onClick={() => execCommand("bold")}
                className="rounded px-2 py-1 text-sm font-medium hover:bg-gray-200"
                title="Bold"
              >
                <strong>B</strong>
              </button>
              <button
                type="button"
                onClick={() => execCommand("italic")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Italic"
              >
                <em>I</em>
              </button>
              <button
                type="button"
                onClick={() => execCommand("underline")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Underline"
              >
                <u>U</u>
              </button>
              <button
                type="button"
                onClick={() => execCommand("strikeThrough")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Strikethrough"
              >
                <s>S</s>
              </button>
              <div className="mx-1 h-6 w-px bg-gray-300"></div>
              <button
                type="button"
                onClick={() => execCommand("formatBlock", "h1")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 1"
              >
                H1
              </button>
              <button
                type="button"
                onClick={() => execCommand("formatBlock", "h2")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 2"
              >
                H2
              </button>
              <button
                type="button"
                onClick={() => execCommand("formatBlock", "h3")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 3"
              >
                H3
              </button>
              <div className="mx-1 h-6 w-px bg-gray-300"></div>
              <button
                type="button"
                onClick={() => execCommand("insertUnorderedList")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Bullet List"
              >
                • List
              </button>
              <button
                type="button"
                onClick={() => execCommand("insertOrderedList")}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Numbered List"
              >
                1. List
              </button>
              <div className="mx-1 h-6 w-px bg-gray-300"></div>
              <button
                type="button"
                onClick={handleLinkInsert}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Insert Link"
              >
                🔗 Link
              </button>
              <button
                type="button"
                onClick={handleImageUpload}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Insert Image"
              >
                🖼️ Image
              </button>
            </div>
            {/* Rich Text Editor */}
            <div
              ref={editorRef}
              contentEditable
              onInput={handleContentChange}
              className="min-h-[200px] w-full rounded-b-md border border-t-0 border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              style={{ whiteSpace: "pre-wrap" }}
            />
          </div>

          <div>
            <label htmlFor="visibility" className="block text-sm font-medium text-gray-700">
              Visibility
            </label>
            <select
              id="visibility"
              value={formData.visibility}
              onChange={(e) => setFormData({ ...formData, visibility: e.target.value })}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            >
              <option value="Private">Private</option>
              <option value="Shared">Shared</option>
            </select>
          </div>
        </form>
      </section>
    );
  }

  return (
    <section className="rounded-xl border p-6">
      <div className="mb-6 flex items-center justify-between">
        <div className="text-xs text-gray-500">
          <span className={`inline-flex items-center gap-1 rounded-full px-2 py-1 text-xs font-medium ${
            note.visibility === 'Private'
              ? 'bg-gray-100 text-gray-600'
              : 'bg-blue-100 text-blue-600'
          }`}>
            {note.visibility}
          </span>
        </div>
        <div className="flex gap-2">
          <button
            onClick={handleEdit}
            className="inline-flex items-center gap-2 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
          >
            Edit
          </button>
        </div>
      </div>

      <div className="space-y-6">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Content</h2>
          <div className="mt-2 rounded-lg bg-gray-50 p-4">
            <div
              className="prose prose-sm max-w-none text-sm text-gray-800 leading-relaxed"
              dangerouslySetInnerHTML={{
                __html: DOMPurify.sanitize(note.content, {
                  ALLOWED_TAGS: ['p', 'br', 'strong', 'em', 'u', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'ul', 'ol', 'li', 'blockquote', 'a', 'img'],
                  ALLOWED_ATTR: ['href', 'src', 'alt', 'title']
                })
              }}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Created by</h3>
            <p className="mt-1 text-sm text-gray-600">{note.createdByDisplayName}</p>
          </div>
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Created</h3>
            <p className="mt-1 text-sm text-gray-600">{new Date(note.createdAtUtc).toLocaleString()}</p>
          </div>
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Last updated</h3>
            <p className="mt-1 text-sm text-gray-600">{new Date(note.updatedAtUtc).toLocaleString()}</p>
          </div>
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Visibility</h3>
            <p className="mt-1 text-sm text-gray-600">{note.visibility}</p>
          </div>
        </div>
      </div>
    </section>
  );
}