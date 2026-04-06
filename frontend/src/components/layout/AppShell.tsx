import type { ReactNode } from "react";
import Link from "next/link";
import SignOutButton from "@/components/auth/SignOutButton";

/**
 * Props for the authenticated app shell.
 */
interface AppShellProps {
  currentUserDisplayName: string;
  children: ReactNode;
}

/**
 * Basic authenticated application shell.
 *
 * Context:
 * - Wraps protected pages only.
 * - Establishes the shared authenticated layout and navigation chrome.
 */
export default function AppShell({
  currentUserDisplayName,
  children,
}: AppShellProps) {
  return (
    <div className="min-h-screen bg-white text-black">
      <header className="border-b">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <div className="space-y-1">
            <p className="text-lg font-semibold">ViewsLife</p>
            <p className="text-xs text-gray-600">Authenticated app area</p>
          </div>

          <nav className="flex items-center gap-4 text-sm">
            <Link href="/dashboard" className="hover:underline">
              Dashboard
            </Link>
            <span className="text-gray-600">{currentUserDisplayName}</span>
            <SignOutButton />
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-6xl px-6 py-8">{children}</main>
    </div>
  );
}