import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { fetchCurrentUser, type CurrentUserResponse } from "@/lib/auth";

/**
 * Loads the current authenticated user for a Server Component request.
 *
 * Context:
 * - Reads the incoming browser cookies from the current request.
 * - Forwards them to the frontend BFF auth route.
 * - Returns the authenticated user when available.
 * - Redirects to the public homepage when the user is not authenticated.
 *
 * This helper centralizes the auth-bootstrap pattern so protected pages/layouts
 * do not duplicate cookie forwarding and redirect logic.
 *
 * @returns The authenticated current-user payload
 */
export async function getCurrentUserOrRedirect(): Promise<CurrentUserResponse> {
  const cookieStore = await cookies();
  const cookieHeader = cookieStore.toString();

  try {
    const currentUser = await fetchCurrentUser(cookieHeader);

    if (!currentUser.isAuthenticated) {
      redirect("/");
    }

    return currentUser;
  } catch {
    redirect("/");
  }
}