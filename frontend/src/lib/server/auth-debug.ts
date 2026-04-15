/**
 * Shared auth-debug logger for staging diagnostics.
 *
 * Context:
 * - Keeps auth logs consistent across Server Components and Route Handlers.
 * - Avoids leaking raw cookie values or secrets into logs.
 * - Can be enabled per environment with AUTH_DEBUG=true.
 *
 * Usage:
 * - Call logAuthDebug("event_name", { ...details }) anywhere in the auth flow.
 * - Safe to leave in the codebase because it is gated by AUTH_DEBUG.
 */

/**
 * Describes a basic dictionary of structured log fields.
 */
type AuthDebugDetails = Record<string, unknown>;

/**
 * Returns true when verbose auth diagnostics are enabled.
 *
 * Environment:
 * - Set AUTH_DEBUG=true in Vercel staging
 * - Leave unset or false elsewhere
 */
export function isAuthDebugEnabled(): boolean {
  return process.env.AUTH_DEBUG === "true";
}

/**
 * Produces a safe cookie summary for logs.
 *
 * Security:
 * - Does NOT log the raw cookie header
 * - Logs only presence, count, and approximate size
 *
 * @param cookieHeader Raw Cookie header if available
 * @returns Safe cookie summary for structured logs
 */
export function summarizeCookieHeader(cookieHeader?: string): {
  hasCookie: boolean;
  cookieCount: number;
  cookieLength: number;
} {
  if (!cookieHeader) {
    return {
      hasCookie: false,
      cookieCount: 0,
      cookieLength: 0,
    };
  }

  const cookieCount = cookieHeader
    .split(";")
    .map((part) => part.trim())
    .filter(Boolean).length;

  return {
    hasCookie: true,
    cookieCount,
    cookieLength: cookieHeader.length,
  };
}

/**
 * Writes a structured auth log entry when AUTH_DEBUG is enabled.
 *
 * @param eventName Short event name
 * @param details Structured diagnostic fields
 */
export function logAuthDebug(
  eventName: string,
  details: AuthDebugDetails
): void {
  if (!isAuthDebugEnabled()) {
    return;
  }

  console.info(
    JSON.stringify({
      scope: "auth-debug",
      event: eventName,
      timestamp: new Date().toISOString(),
      ...details,
    })
  );
}