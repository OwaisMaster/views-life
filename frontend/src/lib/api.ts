/**
 * Reads the backend API base URL from environment variables.
 * This is used by Next.js route handlers when forwarding requests
 * to the ASP.NET backend.
 */
const backendApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;

/**
 * Reads the frontend app base URL from environment variables.
 * This is required for server-side code that needs to call same-origin
 * Next.js route handlers using absolute URLs.
 */
const appBaseUrl = process.env.NEXT_PUBLIC_APP_URL;

/**
 * Returns the configured backend API base URL.
 *
 * @returns The backend API base URL
 */
export function getBackendApiBaseUrl(): string {
  if (!backendApiBaseUrl) {
    throw new Error(
      "NEXT_PUBLIC_API_BASE_URL is not configured in the frontend environment."
    );
  }

  return backendApiBaseUrl;
}

/**
 * Returns the configured frontend app base URL.
 *
 * @returns The frontend app base URL
 */
export function getAppBaseUrl(): string {
  if (!appBaseUrl) {
    throw new Error(
      "NEXT_PUBLIC_APP_URL is not configured in the frontend environment."
    );
  }

  return appBaseUrl;
}

/**
 * Builds a full ASP.NET backend URL from a relative backend path.
 *
 * @param path Relative backend API path beginning with "/"
 * @returns Full backend API URL
 */
export function buildBackendApiUrl(path: string): string {
  return `${getBackendApiBaseUrl()}${path}`;
}

/**
 * Builds a frontend BFF API path.
 *
 * Behavior:
 * - In the browser, returns a relative path such as /api/auth/me
 * - On the server, returns an absolute URL such as https://localhost:3000/api/auth/me
 *
 * This is necessary because server-side fetch requires an absolute URL.
 *
 * @param path Relative frontend API path beginning with "/"
 * @returns Relative or absolute frontend API path depending on runtime
 */
export function buildFrontendApiUrl(path: string): string {
  if (typeof window === "undefined") {
    return `${getAppBaseUrl()}${path}`;
  }

  return path;
}