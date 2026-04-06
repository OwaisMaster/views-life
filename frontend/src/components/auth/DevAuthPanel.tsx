/**
 * Temporary development auth panel.
 *
 * Context:
 * - Uses plain browser form POSTs instead of client-side fetch.
 * - This makes auth-cookie issuance more reliable during local development,
 *   because the browser receives a normal redirect response with Set-Cookie.
 */
export default function DevAuthPanel() {
  return (
    <section className="rounded-xl border p-4">
      <h2 className="mb-2 text-lg font-semibold">Development Auth</h2>

      <div className="flex flex-col gap-3">
        <form action="/api/auth/dev-sign-in" method="post">
          <input type="hidden" name="displayName" value="Views Dev User" />
          <input type="hidden" name="email" value="views.dev@example.com" />
          <input type="hidden" name="authProvider" value="Local" />
          <input
            type="hidden"
            name="providerSubjectId"
            value="local-dev-user-001"
          />

          <button
            type="submit"
            className="rounded-md border px-4 py-2 text-sm"
          >
            Sign in as dev user
          </button>
        </form>

        <form action="/api/auth/sign-out" method="post">
          <button
            type="submit"
            className="rounded-md border px-4 py-2 text-sm"
          >
            Sign out
          </button>
        </form>
      </div>
    </section>
  );
}