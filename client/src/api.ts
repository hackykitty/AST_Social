const TOKEN_KEY = "socialapp.jwt";

export function getApiBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_API_BASE_URL;
  if (fromEnv && fromEnv.length > 0) return fromEnv.replace(/\/$/, "");
  return "";
}

/** OAuth start must hit the API origin (not only the Vite proxy). */
export function getOAuthApiBaseUrl(): string {
  const explicit = import.meta.env.VITE_API_BASE_URL;
  if (explicit && explicit.length > 0) return explicit.replace(/\/$/, "");
  return "http://localhost:5288";
}

export function getStoredToken(): string | null {
  try {
    return sessionStorage.getItem(TOKEN_KEY);
  } catch {
    return null;
  }
}

export function setStoredToken(token: string): void {
  sessionStorage.setItem(TOKEN_KEY, token);
}

export function clearStoredToken(): void {
  sessionStorage.removeItem(TOKEN_KEY);
}

export type SocialPost = {
  text: string;
  imageUrl: string | null;
  createdAt: string | null;
};

export type SocialProfile = {
  provider: string;
  name: string;
  profilePictureUrl: string | null;
  email: string | null;
  phone: string | null;
  recentPosts: SocialPost[];
  postsNote: string | null;
};

export type ApiError = {
  code: string;
  message: string;
};

export async function fetchProfile(token: string): Promise<SocialProfile> {
  const base = getApiBaseUrl();
  const url = `${base}/api/profile/me`;
  const res = await fetch(url, {
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: "application/json",
    },
  });

  const text = await res.text();
  let body: unknown = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = null;
  }

  if (!res.ok) {
    const err = body as ApiError | null;
    const message =
      err?.message ?? `Request failed (${res.status})`;
    throw new Error(message);
  }

  return body as SocialProfile;
}
