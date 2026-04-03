import build from "next/dist/build";
import { buildApiURL } from "./api";

export interface HealthResponse {
    status: string;
    service: string;
    environment: string;
    utcTime: string;
}

export async function fetchHealth(): Promise<HealthResponse> {
    const response = await fetch(buildApiURL("/api/health"), {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
        },
        cache: "no-store",
    });

    if (!response.ok) {
        throw new Error(`Health endpoint failed with status ${response.status}`);
    }

    return (await response.json()) as HealthResponse;
}