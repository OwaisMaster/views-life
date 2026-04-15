// // import { NextRequest, NextResponse } from "next/server";
// // import { buildBackendApiUrl } from "@/lib/api";
// // import { appendBackendSetCookieHeader } from "@/lib/server/cookies";

// // /**
// //  * Frontend BFF sign-in route.
// //  *
// //  * Behavior:
// //  * - Accepts browser form POST
// //  * - Forwards JSON to backend sign-in endpoint
// //  * - On success:
// //  *   - forwards backend auth cookie
// //  *   - redirects to /dashboard
// //  * - On failure:
// //  *   - redirects back to /sign-in with a friendly error code
// //  */
// // export async function POST(request: NextRequest): Promise<NextResponse> {
// //   const formData = await request.formData();

// //   const payload = {
// //     email: String(formData.get("email") ?? ""),
// //     password: String(formData.get("password") ?? ""),
// //   };

// //   const backendResponse = await fetch(buildBackendApiUrl("/api/auth/sign-in"), {
// //     method: "POST",
// //     headers: {
// //       "Content-Type": "application/json",
// //     },
// //     cache: "no-store",
// //     body: JSON.stringify(payload),
// //   });

// //   if (!backendResponse.ok) {
// //     const responseText = await backendResponse.text();

// //     let errorCode = "signin_failed";

// //     // Matches the current backend behavior for bad credentials.
// //     if (
// //       backendResponse.status === 401 ||
// //       responseText.includes("Invalid email or password")
// //     ) {
// //       errorCode = "invalid_credentials";
// //     } else if (backendResponse.status === 400) {
// //       errorCode = "invalid_input";
// //     }

// //     const redirectUrl = new URL("/sign-in", request.url);
// //     redirectUrl.searchParams.set("error", errorCode);

// //     return NextResponse.redirect(redirectUrl, { status: 303 });
// //   }

// //   const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
// //     status: 303,
// //   });

// //   appendBackendSetCookieHeader(
// //     nextResponse,
// //     backendResponse.headers.get("set-cookie")
// //   );

// //   return nextResponse;
// // }
// //-----------------------------------
// import { NextRequest, NextResponse } from "next/server";
// import { buildBackendApiUrl } from "@/lib/api";
// import { appendBackendSetCookieHeader } from "@/lib/server/cookies";
// import { logAuthDebug } from "@/lib/server/auth-debug";

// /**
//  * Frontend BFF sign-in route.
//  *
//  * Behavior:
//  * - Accepts browser form POST
//  * - Forwards JSON to backend sign-in endpoint
//  * - On success:
//  *   - forwards backend auth cookie
//  *   - redirects to /dashboard
//  * - On failure:
//  *   - redirects back to /sign-in with a friendly error code
//  *
//  * Diagnostic behavior:
//  * - Logs the backend URL
//  * - Logs backend status and response preview
//  * - Logs whether a Set-Cookie header was present
//  */
// export async function POST(request: NextRequest): Promise<NextResponse> {
//   const formData = await request.formData();

//   const payload = {
//     email: String(formData.get("email") ?? ""),
//     password: String(formData.get("password") ?? ""),
//   };

//   const backendUrl = buildBackendApiUrl("/api/auth/sign-in");

//   logAuthDebug("bff_sign_in_request_start", {
//     backendUrl,
//     emailLength: payload.email.length,
//     hasPassword: payload.password.length > 0,
//   });

//   const backendResponse = await fetch(backendUrl, {
//     method: "POST",
//     headers: {
//       "Content-Type": "application/json",
//     },
//     cache: "no-store",
//     body: JSON.stringify(payload),
//   });

//   const responseText = await backendResponse.text();
//   const setCookieHeader = backendResponse.headers.get("set-cookie");

//   logAuthDebug("bff_sign_in_response", {
//     backendUrl,
//     status: backendResponse.status,
//     ok: backendResponse.ok,
//     hasSetCookieHeader: Boolean(setCookieHeader),
//     responsePreview: responseText.slice(0, 500),
//   });

//   if (!backendResponse.ok) {
//     let errorCode = "signin_failed";

//     if (
//       backendResponse.status === 401 ||
//       responseText.includes("Invalid email or password")
//     ) {
//       errorCode = "invalid_credentials";
//     } else if (backendResponse.status === 400) {
//       errorCode = "invalid_input";
//     }

//     const redirectUrl = new URL("/sign-in", request.url);
//     redirectUrl.searchParams.set("error", errorCode);

//     logAuthDebug("bff_sign_in_redirect_failure", {
//       errorCode,
//       backendStatus: backendResponse.status,
//     });

//     return NextResponse.redirect(redirectUrl, { status: 303 });
//   }

//   const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
//     status: 303,
//   });

//   appendBackendSetCookieHeader(nextResponse, setCookieHeader);

//   logAuthDebug("bff_sign_in_redirect_success", {
//     redirectTo: "/dashboard",
//     hasSetCookieHeader: Boolean(setCookieHeader),
//   });

//   return nextResponse;
// }
//----------------------------------------
import { NextRequest, NextResponse } from "next/server";
import { buildBackendApiUrl } from "@/lib/api";
import { appendBackendSetCookieHeaders } from "@/lib/server/cookies";
import { logAuthDebug } from "@/lib/server/auth-debug";

/**
 * Frontend BFF sign-in route.
 *
 * Behavior:
 * - Accepts browser form POST
 * - Forwards JSON to backend sign-in endpoint
 * - On success:
 *   - forwards backend auth cookie(s)
 *   - redirects to /dashboard
 * - On failure:
 *   - redirects back to /sign-in with a friendly error code
 *
 * Diagnostic behavior:
 * - Logs the backend URL
 * - Logs backend status and response preview
 * - Logs whether Set-Cookie headers were present
 */
export async function POST(request: NextRequest): Promise<NextResponse> {
  const formData = await request.formData();

  const payload = {
    email: String(formData.get("email") ?? ""),
    password: String(formData.get("password") ?? ""),
  };

  const backendUrl = buildBackendApiUrl("/api/auth/sign-in");

  logAuthDebug("bff_sign_in_request_start", {
    backendUrl,
    emailLength: payload.email.length,
    hasPassword: payload.password.length > 0,
  });

  const backendResponse = await fetch(backendUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    cache: "no-store",
    body: JSON.stringify(payload),
  });

  const responseText = await backendResponse.text();

  const setCookieHeaders =
    typeof backendResponse.headers.getSetCookie === "function"
      ? backendResponse.headers.getSetCookie()
      : (() => {
          const singleHeader = backendResponse.headers.get("set-cookie");

          return singleHeader ? [singleHeader] : [];
        })();

  logAuthDebug("bff_sign_in_response", {
    backendUrl,
    status: backendResponse.status,
    ok: backendResponse.ok,
    setCookieHeaderCount: setCookieHeaders.length,
    responsePreview: responseText.slice(0, 500),
  });

  if (!backendResponse.ok) {
    let errorCode = "signin_failed";

    if (
      backendResponse.status === 401 ||
      responseText.includes("Invalid email or password")
    ) {
      errorCode = "invalid_credentials";
    } else if (backendResponse.status === 400) {
      errorCode = "invalid_input";
    }

    const redirectUrl = new URL("/sign-in", request.url);
    redirectUrl.searchParams.set("error", errorCode);

    logAuthDebug("bff_sign_in_redirect_failure", {
      errorCode,
      backendStatus: backendResponse.status,
    });

    return NextResponse.redirect(redirectUrl, { status: 303 });
  }

  const nextResponse = NextResponse.redirect(new URL("/dashboard", request.url), {
    status: 303,
  });

  appendBackendSetCookieHeaders(nextResponse, setCookieHeaders);

  logAuthDebug("bff_sign_in_redirect_success", {
    redirectTo: "/dashboard",
    setCookieHeaderCount: setCookieHeaders.length,
  });

  return nextResponse;
}