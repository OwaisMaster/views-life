// import { cookies } from "next/headers";
// import { redirect } from "next/navigation";
// import { fetchCurrentUser, type CurrentUserResponse } from "@/lib/auth";

// /**
//  * Loads the current authenticated user for a Server Component request.
//  *
//  * Context:
//  * - Reads the incoming browser cookies from the current request.
//  * - Forwards them to the frontend BFF auth route.
//  * - Returns the authenticated user when available.
//  * - Redirects to the public homepage when the user is not authenticated.
//  *
//  * This helper centralizes the auth-bootstrap pattern so protected pages/layouts
//  * do not duplicate cookie forwarding and redirect logic.
//  *
//  * @returns The authenticated current-user payload
//  */
// export async function getCurrentUserOrRedirect(): Promise<CurrentUserResponse> {
//   const cookieStore = await cookies();
//   const cookieHeader = cookieStore.toString();

//   try {
//     const currentUser = await fetchCurrentUser(cookieHeader);

//     if (!currentUser.isAuthenticated) {
//       redirect("/");
//     }

//     return currentUser;
//   } catch {
//     redirect("/");
//   }
// }
//----------------------------------------
import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { fetchCurrentUser, type CurrentUserResponse } from "@/lib/auth";
import {
  logAuthDebug,
  summarizeCookieHeader,
} from "@/lib/server/auth-debug";

/**
 * Loads the current authenticated user for a Server Component request.
 *
 * Context:
 * - Reads the incoming browser cookies from the current request.
 * - Forwards them to the frontend BFF auth route.
 * - Returns the authenticated user when available.
 * - Redirects to the public homepage when the user is not authenticated.
 *
 * Diagnostic behavior:
 * - Logs the outgoing auth-bootstrap attempt
 * - Logs the exact status / error path before redirecting
 *
 * @returns The authenticated current-user payload
 */
export async function getCurrentUserOrRedirect(): Promise<CurrentUserResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();
  const cookieSummary = summarizeCookieHeader(cookieHeader);

  logAuthDebug("server_auth_helper_start", {
    ...cookieSummary,
  });

  try {
    const currentUser = await fetchCurrentUser(cookieHeader);

    logAuthDebug("server_auth_helper_fetch_success", {
      ...cookieSummary,
      isAuthenticated: currentUser.isAuthenticated,
      userId: currentUser.userId,
      tenantId: currentUser.tenantId,
    });

    if (!currentUser.isAuthenticated) {
      logAuthDebug("server_auth_helper_redirect_unauthenticated", {
        reason: "current_user_reported_unauthenticated",
        ...cookieSummary,
      });

      redirect("/");
    }

    return currentUser;
  } catch (error) {
    const errorMessage =
      error instanceof Error ? error.message : "Unknown auth error";

    logAuthDebug("server_auth_helper_redirect_exception", {
      reason: "fetch_current_user_threw",
      ...cookieSummary,
      errorMessage,
    });

    redirect("/");
  }
}