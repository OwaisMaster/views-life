import Link from "next/link";
import { getAuthErrorMessage } from "@/lib/auth-errors";

/**
 * Props for the public sign-in page.
 */
interface SignInPageProps {
  searchParams?: Promise<{
    error?: string;
  }>;
}

/**
 * Public sign-in page for local accounts.
 *
 * Context:
 * - Posts to the frontend BFF sign-in route.
 * - On invalid credentials or request errors, the BFF redirects back here with
 *   an error code.
 * - This page renders a polished, user-friendly message.
 */
export default async function SignInPage({
  searchParams,
}: SignInPageProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const errorMessage = getAuthErrorMessage(resolvedSearchParams?.error);

  return (
    <main className="min-h-screen px-6 py-12">
      <div className="mx-auto max-w-xl space-y-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-bold">Sign in</h1>
          <p className="text-sm text-gray-600">
            Continue into your ViewsLife space
          </p>
        </header>

        {errorMessage ? (
          <section className="rounded-xl border border-red-300 bg-red-50 p-4">
            <p className="text-sm text-red-800">{errorMessage}</p>
          </section>
        ) : null}

        <form
          action="/api/auth/sign-in"
          method="post"
          className="space-y-4 rounded-xl border p-6"
        >
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
              className="w-full rounded-md border px-3 py-2 text-sm"
            />
          </div>

          <button
            type="submit"
            className="rounded-md border px-4 py-2 text-sm"
          >
            Sign in
          </button>
        </form>

        <p className="text-sm text-gray-600">
          Need an account?{" "}
          <Link href="/register" className="underline">
            Create one
          </Link>
        </p>
      </div>
    </main>
  );
}