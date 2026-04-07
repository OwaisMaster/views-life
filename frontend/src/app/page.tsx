import Link from "next/link";
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { fetchHealth, type HealthResponse } from "@/lib/health";
import { fetchCurrentUser, type CurrentUserResponse } from "@/lib/auth";

interface RequestResult<T> {
  data: T | null;
  error: string | null;
}

async function getSafeResult<T>(
  request: () => Promise<T>
): Promise<RequestResult<T>> {
  try {
    const data = await request();

    return {
      data,
      error: null,
    };
  } catch (requestError) {
    const message =
      requestError instanceof Error
        ? `${requestError.name}: ${requestError.message}`
        : "Unknown request error.";

    return {
      data: null,
      error: message,
    };
  }
}

/**
 * Public homepage.
 *
 * Context:
 * - Shows the polished entry points for account creation and sign-in.
 * - Redirects authenticated users into the protected dashboard.
 */
export default async function Home() {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  const [healthResult, currentUserResult] = await Promise.all([
    getSafeResult<HealthResponse>(fetchHealth),
    getSafeResult<CurrentUserResponse>(() => fetchCurrentUser(cookieHeader)),
  ]);

  if (currentUserResult.data?.isAuthenticated) {
    redirect("/dashboard");
  }

  return (
    <main className="min-h-screen px-6 py-12">
      <div className="mx-auto max-w-4xl space-y-10">
        <header className="space-y-4">
          <div className="space-y-2">
            <p className="text-sm font-medium text-gray-600">ViewsLife</p>
            <h1 className="text-4xl font-bold tracking-tight">
              Archive your thoughts into an explorable space
            </h1>
            <p className="max-w-2xl text-sm text-gray-600">
              Create your account, bootstrap your personal note space, and begin
              building the foundation for your archive.
            </p>
          </div>

          <div className="flex flex-wrap gap-3">
            <Link
              href="/register"
              className="rounded-md border px-4 py-2 text-sm font-medium"
            >
              Create account
            </Link>

            <Link
              href="/sign-in"
              className="rounded-md border px-4 py-2 text-sm font-medium"
            >
              Sign in
            </Link>
          </div>
        </header>

        <section className="rounded-xl border p-6">
          <h2 className="mb-3 text-lg font-semibold">What happens next</h2>
          <div className="space-y-2 text-sm text-gray-700">
            <p>1. You create an account.</p>
            <p>2. Your personal tenant space is created automatically.</p>
            <p>3. You are signed in and redirected to your dashboard.</p>
            <p>4. Future notes, collections, and collaboration features attach to that tenant.</p>
          </div>
        </section>

        <section className="rounded-xl border p-6">
          <h2 className="mb-3 text-lg font-semibold">Backend Health Check</h2>

          {healthResult.data ? (
            <div className="space-y-2 text-sm">
              <p>
                <strong>Connection:</strong> Successful
              </p>
              <p>
                <strong>Status:</strong> {healthResult.data.status}
              </p>
              <p>
                <strong>Service:</strong> {healthResult.data.service}
              </p>
              <p>
                <strong>Environment:</strong> {healthResult.data.environment}
              </p>
              <p>
                <strong>UTC Time:</strong> {healthResult.data.utcTime}
              </p>
            </div>
          ) : (
            <div className="space-y-2 text-sm">
              <p>
                <strong>Connection:</strong> Failed
              </p>
              <p>
                <strong>Error:</strong> {healthResult.error ?? "Unknown error"}
              </p>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}