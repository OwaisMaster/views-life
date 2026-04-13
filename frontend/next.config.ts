import type { NextConfig } from "next";

const backendUrl = process.env.RENDER_BACKEND_URL;

const nextConfig: NextConfig = {
  async rewrites() {
    if (!backendUrl) {
      return []; // Skip proxy in CI / local where var isn't set
    }
    return [
      {
        source: "/api/:path*",
        destination: `${backendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;