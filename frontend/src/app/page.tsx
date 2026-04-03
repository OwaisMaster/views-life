"use client";

import { useEffect, useState } from "react";
import { fetchHealth, type HealthResponse } from "@/lib/health";

/**
 * Home page for the initial ViewsLife frontend shell.
 * This version runs the backend health check in the browser so local
 * HTTPS certificate issues on the Next.js server do not block progress.
 */
export default function Home() {
  // Stores a successful backend response.
  const [data, setData] = useState<HealthResponse | null>(null);

  // Stores a user-visible error message if the request fails.
  const [error, setError] = useState<string | null>(null);

  // Tracks whether the request is still in progress.
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    /**
     * Calls the backend health endpoint from the browser after the page loads.
     * This is useful during local development to verify browser-to-backend
     * connectivity separately from server-side rendering concerns.
     */
    async function loadHealth(): Promise<void> {
      try {
        const result = await fetchHealth();
        setData(result);
      } catch (requestError) {
        setError(
          requestError instanceof Error
            ? requestError.message
            : "Unknown error while calling the backend health endpoint."
        );
      } finally {
        setIsLoading(false);
      }
    }

    void loadHealth();
  }, []);

  return (
    <main className="min-h-screen px-6 py-12">
      <div className="mx-auto max-w-3xl space-y-6">
        {/* Basic heading for the starter app shell */}
        <header className="space-y-2">
          <h1 className="text-3xl font-bold">ViewsLife</h1>
          <p className="text-sm text-gray-600">
            Frontend to backend connectivity check
          </p>
        </header>

        {/* Displays the configured backend URL so environment issues are easier to diagnose */}
        <section className="rounded-xl border p-4">
          <h2 className="mb-2 text-lg font-semibold">Frontend Configuration</h2>
          <p className="text-sm">
            API Base URL: {process.env.NEXT_PUBLIC_API_BASE_URL ?? "Not set"}
          </p>
        </section>

        {/* Displays the current health-check state */}
        <section className="rounded-xl border p-4">
          <h2 className="mb-2 text-lg font-semibold">Backend Health Check</h2>

          {isLoading ? (
            <p className="text-sm">Checking backend connection...</p>
          ) : data ? (
            <div className="space-y-2 text-sm">
              <p>
                <strong>Connection:</strong> Successful
              </p>
              <p>
                <strong>Status:</strong> {data.status}
              </p>
              <p>
                <strong>Service:</strong> {data.service}
              </p>
              <p>
                <strong>Environment:</strong> {data.environment}
              </p>
              <p>
                <strong>UTC Time:</strong> {data.utcTime}
              </p>
            </div>
          ) : (
            <div className="space-y-2 text-sm">
              <p>
                <strong>Connection:</strong> Failed
              </p>
              <p>
                <strong>Error:</strong> {error ?? "Unknown error"}
              </p>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}