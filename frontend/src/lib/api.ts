const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL;

export function getApiBaseUrl(): string {
    if (!apiBaseUrl) {
        throw new Error (
            "NEXT_PUBLIC_API_BASE_URL is not configured in the frontend environment."
        );
    }
    return apiBaseUrl;
}

export function buildApiURL(path: string): string {
    return `${getApiBaseUrl()}${path}`;
}