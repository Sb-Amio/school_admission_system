
const API_URL = (import.meta.env.VITE_API_URL || "http://localhost:8080").replace(/\/$/, "");

export async function apiRequest<T = unknown>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const headers = new Headers(options.headers);

  if (options.body != null && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  let response: Response;

  try {
    response = await fetch(`${API_URL}${endpoint}`, {
      ...options,
      headers,
    });
  } catch {
    throw new Error("Cannot connect to the server. Check that the backend is running.");
  }

  if (!response.ok) {
    let message = `Request failed (${response.status})`;

    try {
      const data: {
        error?: string;
        title?: string;
        message?: string;
        detail?: string;
      } = await response.json();

      message =
        data.error ||
        data.message ||
        data.detail ||
        data.title ||
        message;
    } catch {
      message = response.statusText || message;
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return null as T;
  }

  return (await response.json()) as T;
}
