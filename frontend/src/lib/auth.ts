import { buildApiUrl } from "./api";

// Represents the expected payload returned by the backend auth bootstrap endpoint.
export interface CurrentUserResponse {
    userId: string;
    displayName: string;
    isAuthenticated: boolean;
}

// Calls the backend current-user endpoint.
// This is an early bootstrap request that will later drive app-level auth state.

export async function fetchCurrentUser(): Promise<CurrentUserResponse> {
    const response = await fetch(buildApiUrl("/api/auth/me"), {
        method: "GET",
        headers: {
            "Content-Type": "application/json",
        },
        cache: "no-store",
    });

    if (!response.ok) {
        throw new Error(`Current user endpoint failed with status ${response.status}.`);
    }
    
    return (await response.json()) as CurrentUserResponse;
}