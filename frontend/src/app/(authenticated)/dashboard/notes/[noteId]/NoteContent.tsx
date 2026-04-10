"use client";

import { useState, useEffect, ReactNode } from "react";
import { useRouter } from "next/navigation";
import { Editor, EditorState, RichUtils, convertToRaw, convertFromRaw, ContentState } from "draft-js";
import "draft-js/dist/Draft.css";
import type { NoteResponse } from "@/lib/notes";

// Safe rich text editor that stores content as JSON, not HTML
interface NoteContentProps {
  note: NoteResponse;
}

// Helper function to parse content into EditorState
const parseContentToEditorState = (content: string): EditorState => {
  try {
    const parsed = JSON.parse(content);
    if (parsed.blocks && parsed.entityMap) {
      const contentState = convertFromRaw(parsed);
      return EditorState.createWithContent(contentState);
    }
  } catch {
    // If parsing fails, treat as plain text
  }
  // Fallback to plain text or empty
  const contentState = content.trim()
    ? ContentState.createFromText(content)
    : ContentState.createFromText('');
  return EditorState.createWithContent(contentState);
};

export function NoteContent({ note }: NoteContentProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [formData, setFormData] = useState({
    title: note.title,
    content: note.content,
    visibility: note.visibility,
  });
  const [editorState, setEditorState] = useState(() => parseContentToEditorState(note.content));
  const router = useRouter();

  // Update editor state when note content changes (after save/refresh)
  useEffect(() => {
    setEditorState(parseContentToEditorState(note.content));
  }, [note.content]);

  const handleEdit = () => {
    // Initialize editor with current saved content
    setEditorState(parseContentToEditorState(note.content));
    setIsEditing(true);
  };

  const handleCancel = () => {
    setFormData({
      title: note.title,
      content: note.content,
      visibility: note.visibility,
    });
    // Reset editor state to current saved content
    setEditorState(parseContentToEditorState(note.content));
    setIsEditing(false);
  };

  const handleDelete = async () => {
    if (!confirm('Are you sure you want to delete this note? This action cannot be undone.')) {
      return;
    }

    try {
      const response = await fetch(`/api/notes/${note.id}`, {
        method: "DELETE",
      });

      if (response.ok) {
        router.push('/dashboard/notes'); // Redirect to notes list
      } else {
        console.error("Failed to delete note");
      }
    } catch (error) {
      console.error("Error deleting note:", error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);

    try {
      // Convert editor content to JSON for secure storage
      const contentState = editorState.getCurrentContent();
      const rawContent = convertToRaw(contentState);
      const serializedContent = JSON.stringify(rawContent);

      const response = await fetch(`/api/notes/${note.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          ...formData,
          content: serializedContent,
        }),
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

  // Rich text formatting functions
  const toggleInlineStyle = (style: string) => {
    setEditorState(RichUtils.toggleInlineStyle(editorState, style));
  };

  const toggleBlockType = (blockType: string) => {
    setEditorState(RichUtils.toggleBlockType(editorState, blockType));
  };

  // Style map for custom inline styles (colors, etc.)
  const styleMap = {
    'RED': { color: '#dc2626' },
    'BLUE': { color: '#2563eb' },
    'GREEN': { color: '#16a34a' },
    'YELLOW': { color: '#ca8a04' },
    'PURPLE': { color: '#9333ea' },
    'ORANGE': { color: '#ea580c' },
    'BOLD': { fontWeight: 'bold' },
    'ITALIC': { fontStyle: 'italic' },
    'UNDERLINE': { textDecoration: 'underline' },
    'STRIKETHROUGH': { textDecoration: 'line-through' },
  };

  // Block style function for headings
  const getBlockStyle = (block: { getType: () => string }) => {
    switch (block.getType()) {
      case 'blockquote':
        return 'border-l-4 border-gray-300 pl-4 italic text-gray-600';
      case 'header-one':
        return 'text-2xl font-bold mb-2';
      case 'header-two':
        return 'text-xl font-bold mb-2';
      case 'header-three':
        return 'text-lg font-bold mb-2';
      case 'unordered-list-item':
        return 'ml-4';
      case 'ordered-list-item':
        return 'ml-4';
      default:
        return '';
    }
  };

  // Render content safely from stored JSON
  const renderContent = (): ReactNode => {
    try {
      const content = JSON.parse(note.content);
      if (content.blocks && content.entityMap) {
        const contentState = convertFromRaw(content);
        const readOnlyEditorState = EditorState.createWithContent(contentState);
        return (
          <div className="prose prose-sm max-w-none text-sm text-gray-800 leading-relaxed">
            <div className="draft-editor-wrapper">
              <Editor
                editorState={readOnlyEditorState}
                onChange={() => {}} // Read-only
                customStyleMap={styleMap}
                blockStyleFn={getBlockStyle}
                readOnly={true}
              />
            </div>
          </div>
        );
      }
    } catch (error) {
      console.log('Error parsing content:', error);
    }

    // Render as plain text if no rich content or parsing failed
    return (
      <p className="whitespace-pre-wrap text-sm text-gray-800 leading-relaxed">
        {note.content || 'No content'}
      </p>
    );
  };

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
              {/* Text Formatting */}
              <button
                type="button"
                onClick={() => toggleInlineStyle('BOLD')}
                className="rounded px-2 py-1 text-sm font-medium hover:bg-gray-200"
                title="Bold"
              >
                <strong>B</strong>
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('ITALIC')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Italic"
              >
                <em>I</em>
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('UNDERLINE')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Underline"
              >
                <u>U</u>
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('STRIKETHROUGH')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Strikethrough"
              >
                <s>S</s>
              </button>

              <div className="mx-1 h-6 w-px bg-gray-300"></div>

              {/* Colors */}
              <button
                type="button"
                onClick={() => toggleInlineStyle('RED')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Red Text"
              >
                🔴 Red
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('BLUE')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Blue Text"
              >
                🔵 Blue
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('GREEN')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Green Text"
              >
                🟢 Green
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('YELLOW')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Yellow Text"
              >
                🟡 Yellow
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('PURPLE')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Purple Text"
              >
                🟣 Purple
              </button>
              <button
                type="button"
                onClick={() => toggleInlineStyle('ORANGE')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Orange Text"
              >
                🟠 Orange
              </button>

              <div className="mx-1 h-6 w-px bg-gray-300"></div>

              {/* Headings */}
              <button
                type="button"
                onClick={() => toggleBlockType('header-one')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 1"
              >
                H1
              </button>
              <button
                type="button"
                onClick={() => toggleBlockType('header-two')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 2"
              >
                H2
              </button>
              <button
                type="button"
                onClick={() => toggleBlockType('header-three')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Heading 3"
              >
                H3
              </button>

              <div className="mx-1 h-6 w-px bg-gray-300"></div>

              {/* Lists */}
              <button
                type="button"
                onClick={() => toggleBlockType('unordered-list-item')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Bullet List"
              >
                • List
              </button>
              <button
                type="button"
                onClick={() => toggleBlockType('ordered-list-item')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Numbered List"
              >
                1. List
              </button>

              <div className="mx-1 h-6 w-px bg-gray-300"></div>

              {/* Blockquote */}
              <button
                type="button"
                onClick={() => toggleBlockType('blockquote')}
                className="rounded px-2 py-1 text-sm hover:bg-gray-200"
                title="Blockquote"
              >
                &quot;Quote&quot;
              </button>
            </div>

            {/* Rich Text Editor */}
            <div className="min-h-[300px] w-full rounded-b-md border border-t-0 border-gray-300 px-3 py-2 shadow-sm focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500">
              <Editor
                editorState={editorState}
                onChange={setEditorState}
                customStyleMap={styleMap}
                blockStyleFn={getBlockStyle}
                placeholder="Start writing your note..."
              />
            </div>
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
          <button
            onClick={handleDelete}
            className="inline-flex items-center gap-2 rounded-lg border border-red-300 bg-white px-3 py-2 text-sm font-medium text-red-700 shadow-sm hover:bg-red-50 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2"
          >
            Delete
          </button>
        </div>
      </div>

      <div className="space-y-6">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Content</h2>
          <div className="mt-2 rounded-lg bg-gray-50 p-4">
            {renderContent()}
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