// import { buildFrontendApiUrl } from "@/lib/api";

// /**
//  * Represents the lightweight current-user payload returned by the auth API.
//  * This is used to bootstrap frontend auth/session state.
//  */
// export interface CurrentUserResponse {
//   userId: string;
//   displayName: string;
//   isAuthenticated: boolean;
//   tenantId: string;
//   tenantName: string;
//   tenantRole: string;
// }

// /**
//  * Calls the frontend BFF current-user endpoint.
//  *
//  * Context:
//  * - In the browser, auth cookies are sent automatically.
//  * - In SSR, the caller must forward the incoming cookie header manually.
//  *
//  * @param cookieHeader Optional raw cookie header for SSR requests
//  * @returns The current-user response
//  */
// export async function fetchCurrentUser(
//   cookieHeader?: string
// ): Promise<CurrentUserResponse> {
//   const response = await fetch(buildFrontendApiUrl("/api/auth/me"), {
//     method: "GET",
//     headers: {
//       "Content-Type": "application/json",
//       ...(cookieHeader ? { Cookie: cookieHeader } : {}),
//     },
//     cache: "no-store",
//     credentials: "include",
//   });

//   if (!response.ok) {
//     throw new Error(
//       `Current user endpoint failed with status ${response.status}.`
//     );
//   }

//   return (await response.json()) as CurrentUserResponse;
// }
//----------------------------------------
import { buildFrontendApiUrl } from "@/lib/api";
import {
  logAuthDebug,
  summarizeCookieHeader,
} from "@/lib/server/auth-debug";

export interface CurrentUserResponse {
  userId: string;
  displayName: string;
  isAuthenticated: boolean;
  tenantId: string;
  tenantName: string;
  tenantRole: string;
}

export async function fetchCurrentUser(
  cookieHeader?: string
): Promise<CurrentUserResponse> {
  const url = buildFrontendApiUrl("/api/auth/me");
  const cookieSummary = summarizeCookieHeader(cookieHeader);

  logAuthDebug("fetch_current_user_request_start", {
    url,
    ...cookieSummary,
    runtime: typeof window === "undefined" ? "server" : "browser",
  });

  const response = await fetch(url, {
    method: "GET",
    headers: {
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
    },
    cache: "no-store",
  });

  const responseText = await response.text();

  logAuthDebug("fetch_current_user_response", {
    url,
    ...cookieSummary,
    status: response.status,
    ok: response.ok,
    responsePreview: responseText.slice(0, 500),
  });

  if (!response.ok) {
    throw new Error(
      `Current user endpoint failed with status ${response.status}. URL=${url}. Body=${responseText.slice(
        0,
        500
      )}`
    );
  }

  return JSON.parse(responseText) as CurrentUserResponse;
}