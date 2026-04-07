import Link from "next/link";
import { getAuthErrorMessage } from "@/lib/auth-errors";

/**
 * Props for the public registration page.
 */
interface RegisterPageProps {
  searchParams?: Promise<{
    error?: string;
  }>;
}

/**
 * Public registration page.
 *
 * Context:
 * - Posts to the frontend BFF register route.
 * - On validation/auth errors, the BFF redirects back here with an error code.
 * - This page renders a user-friendly message instead of exposing raw API output.
 */
export default async function RegisterPage({
  searchParams,
}: RegisterPageProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const errorMessage = getAuthErrorMessage(resolvedSearchParams?.error);

  return (
    <main className="min-h-screen px-6 py-12">
      <div className="mx-auto max-w-xl space-y-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-bold">Create your ViewsLife account</h1>
          <p className="text-sm text-gray-600">
            Create your account and personal note space
          </p>
        </header>

        {errorMessage ? (
          <section className="rounded-xl border border-red-300 bg-red-50 p-4">
            <p className="text-sm text-red-800">{errorMessage}</p>
          </section>
        ) : null}

        <form
          action="/api/auth/register"
          method="post"
          className="space-y-4 rounded-xl border p-6"
        >
          <div className="space-y-1">
            <label htmlFor="displayName" className="text-sm font-medium">
              Display name
            </label>
            <input
              id="displayName"
              name="displayName"
              type="text"
              required
              className="w-full rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="tenantName" className="text-sm font-medium">
              Space name
            </label>
            <input
              id="tenantName"
              name="tenantName"
              type="text"
              required
              className="w-full rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="email" className="text-sm font-medium">
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              required
              className="w-full rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <div className="space-y-1">
            <label htmlFor="password" className="text-sm font-medium">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              required
              minLength={8}
              className="w-full rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <button
            type="submit"
            className="rounded-md border px-4 py-2 text-sm"
          >
            Create account
          </button>
        </form>

        <p className="text-sm text-gray-600">
          Already have an account?{" "}
          <Link href="/sign-in" className="underline">
            Sign in
          </Link>
        </p>
      </div>
    </main>
  );
}