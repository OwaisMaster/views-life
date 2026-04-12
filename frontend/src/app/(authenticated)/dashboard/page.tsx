import Link from "next/link";
import { getCurrentUserOrRedirect } from "@/lib/server/auth";

/**
 * Protected dashboard page.
 *
 * Context:
 * - Uses the shared authenticated server helper.
 * - Assumes redirect behavior is handled centrally by that helper.
 */
export default async function DashboardPage() {
  const currentUser = await getCurrentUserOrRedirect();

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <p className="text-sm text-gray-600">
          Protected application area for authenticated users
        </p>
      </header>

      <section className="rounded-xl border p-4">
        <h2 className="mb-2 text-lg font-semibold">Current User</h2>
        <div className="space-y-2 text-sm">
          <p>
            <strong>User ID:</strong> {currentUser.userId}
          </p>
          <p>
            <strong>Display Name:</strong> {currentUser.displayName}
          </p>
          <p>
            <strong>Is Authenticated:</strong>{" "}
            {currentUser.isAuthenticated ? "true" : "false"}
          </p>
        </div>
      </section>

      <section className="rounded-xl border p-4">
        <h2 className="mb-2 text-lg font-semibold">Next Steps</h2>
        <p className="text-sm text-gray-700">
          This is where the authenticated note experience will begin. The next
          protected slices should be note listing, note detail, and tenant-owned
          CRUD flows.
        </p>
        <div className="mt-4">
          <Link
            href="/dashboard/notes"
            className="inline-flex rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700"
          >
            View notes
          </Link>
        </div>
      </section>
    </div>
  );
}