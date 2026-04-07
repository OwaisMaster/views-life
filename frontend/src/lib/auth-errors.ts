/**
 * Maps route-level auth error codes to user-friendly UI messages.
 *
 * Context:
 * - Route handlers redirect back to the page with a short error code.
 * - Pages translate that code into a polished, user-safe message.
 */
export function getAuthErrorMessage(
  errorCode: string | undefined
): string | null {
  switch (errorCode) {
    case "email_exists":
      return "An account with that email already exists. Try signing in instead.";
    case "invalid_credentials":
      return "The email or password you entered is incorrect.";
    case "invalid_input":
      return "Please review the form and try again.";
    case "registration_failed":
      return "We could not create your account right now. Please try again.";
    case "signin_failed":
      return "We could not sign you in right now. Please try again.";
    default:
      return null;
  }
}