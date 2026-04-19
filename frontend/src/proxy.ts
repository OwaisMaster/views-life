// frontend/proxy.ts
// Or frontend/src/proxy.ts depending on your app structure.
// Purpose:
// Return a 503 response for incoming requests so the site behaves as down/maintenance.
//
// Dependencies / prerequisites:
// - Next.js project
// - File must be named proxy.ts and placed at the project root,
//   or inside src if your project uses src/
// - No manual imports elsewhere are needed

import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

/**
 * Intercepts requests before they reach routes/pages.
 * Returns a maintenance response for nearly all app traffic.
 */
export function proxy(_request: NextRequest) {
  return new NextResponse('Site temporarily unavailable', {
    status: 503,
    headers: {
      'Content-Type': 'text/plain; charset=utf-8',
      'Cache-Control': 'no-store, no-cache, must-revalidate',
      'Retry-After': '3600',
    },
  });
}

/**
 * Excludes static assets and common metadata files from the matcher.
 */
export const config = {
  matcher: [
    '/((?!_next/static|_next/image|favicon.ico|robots.txt|sitemap.xml).*)',
  ],
};