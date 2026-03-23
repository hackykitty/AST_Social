import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  clearStoredToken,
  fetchProfile,
  getStoredToken,
  type SocialProfile,
} from "../api.ts";

export default function ProfilePage() {
  const [profile, setProfile] = useState<SocialProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const token = getStoredToken();
    if (!token) {
      setError("You are not signed in. Connect from the welcome screen.");
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await fetchProfile(token);
      setProfile(data);
    } catch (e) {
      const msg = e instanceof Error ? e.message : "Failed to load profile.";
      setError(msg);
      setProfile(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  function signOut() {
    clearStoredToken();
    window.location.href = "/";
  }

  if (loading && !profile) {
    return (
      <div className="page">
        <div className="card">
          <p className="muted">Loading your profile…</p>
        </div>
      </div>
    );
  }

  if (error && !profile) {
    return (
      <div className="page narrow">
        <div className="card">
          <div className="alert error" role="alert">
            <strong>Something went wrong</strong>
            <p>{error}</p>
            <div className="row">
              <button type="button" className="btn btn-secondary" onClick={() => void load()}>
                Try again
              </button>
              <Link className="btn btn-ghost" to="/">
                Home
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!profile) return null;

  return (
    <div className="page profile">
      <header className="profile-header">
        <div>
          <p className="eyebrow">{profile.provider}</p>
          <h1>{profile.name}</h1>
        </div>
        <button type="button" className="btn btn-ghost" onClick={signOut}>
          Sign out
        </button>
      </header>

      <div className="card profile-card">
        <div className="profile-main">
          {profile.profilePictureUrl ? (
            <img
              className="avatar"
              src={profile.profilePictureUrl}
              alt=""
              width={120}
              height={120}
            />
          ) : (
            <div className="avatar placeholder" aria-hidden>
              {profile.name.slice(0, 1).toUpperCase()}
            </div>
          )}
          <div className="contact">
            <h2>Contact</h2>
            <dl className="kv">
              <div>
                <dt>Email</dt>
                <dd>{profile.email ?? "—"}</dd>
              </div>
              <div>
                <dt>Phone</dt>
                <dd>{profile.phone ?? "—"}</dd>
              </div>
            </dl>
            <p className="hint small">
              Phone and email depend on what each network shares with your app permissions.
            </p>
          </div>
        </div>
      </div>

      <section className="card posts-section">
        <h2>Last posts</h2>
        {profile.postsNote ? <p className="muted note">{profile.postsNote}</p> : null}
        {profile.recentPosts.length === 0 && !profile.postsNote ? (
          <p className="muted">No posts were returned.</p>
        ) : (
          <ul className="post-list">
            {profile.recentPosts.map((post, i) => (
              <li key={i} className="post">
                {post.imageUrl ? (
                  <img src={post.imageUrl} alt="" className="post-image" loading="lazy" />
                ) : null}
                <div className="post-body">
                  {post.createdAt ? (
                    <time className="post-date" dateTime={post.createdAt}>
                      {new Date(post.createdAt).toLocaleString()}
                    </time>
                  ) : null}
                  <p className="post-text">{post.text || "(No text)"}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
