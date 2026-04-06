/**
 * Simple authenticated sign-out button.
 *
 * Context:
 * - Uses a normal browser form POST to the frontend BFF sign-out route.
 * - This keeps sign-out aligned with the current auth architecture.
 */
export default function SignOutButton() {
  return (
    <form action="/api/auth/sign-out" method="post">
      <button
        type="submit"
        className="rounded-md border px-4 py-2 text-sm"
      >
        Sign out
      </button>
    </form>
  );
}