import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import DevAuthPanel from "@/components/auth/DevAuthPanel";
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
    let message = "Unknown request error.";

    if (requestError instanceof Error) {
      message = `${requestError.name}: ${requestError.message}`;
    }

    return {
      data: null,
      error: message,
    };
  }
}

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
      <div className="mx-auto max-w-3xl space-y-6">
        <header className="space-y-2">
          <h1 className="text-3xl font-bold">ViewsLife</h1>
          <p className="text-sm text-gray-600">
            Public entry point and development auth check
          </p>
        </header>

        <section className="rounded-xl border p-4">
          <h2 className="mb-2 text-lg font-semibold">Frontend Configuration</h2>
          <p className="text-sm">
            Backend API Base URL:{" "}
            {process.env.NEXT_PUBLIC_API_BASE_URL ?? "Not set"}
          </p>
        </section>

        <section className="rounded-xl border p-4">
          <h2 className="mb-2 text-lg font-semibold">Backend Health Check</h2>

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

        <DevAuthPanel />

        <section className="rounded-xl border p-4">
          <h2 className="mb-2 text-lg font-semibold">Current User</h2>

          {currentUserResult.data ? (
            <div className="space-y-2 text-sm">
              <p>
                <strong>User ID:</strong> {currentUserResult.data.userId}
              </p>
              <p>
                <strong>Display Name:</strong>{" "}
                {currentUserResult.data.displayName}
              </p>
              <p>
                <strong>Is Authenticated:</strong>{" "}
                {currentUserResult.data.isAuthenticated ? "true" : "false"}
              </p>
            </div>
          ) : (
            <div className="space-y-2 text-sm">
              <p>
                <strong>Request:</strong> Failed
              </p>
              <p>
                <strong>Error:</strong>{" "}
                {currentUserResult.error ?? "Unknown error"}
              </p>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}