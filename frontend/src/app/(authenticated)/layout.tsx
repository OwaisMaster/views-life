import type { ReactNode } from "react";
import AppShell from "@/components/layout/AppShell";
import { getCurrentUserOrRedirect } from "@/lib/server/auth";

/**
 * Protected authenticated layout.
 *
 * Behavior:
 * - Loads the current authenticated user through the shared server helper
 * - Redirects automatically if the user is not authenticated
 * - Wraps protected pages in the authenticated app shell
 */
export default async function AuthenticatedLayout({
  children,
}: {
  children: ReactNode;
}) {
  const currentUser = await getCurrentUserOrRedirect();

  return (
    <AppShell currentUserDisplayName={currentUser.displayName}>
      {children}
    </AppShell>
  );
}