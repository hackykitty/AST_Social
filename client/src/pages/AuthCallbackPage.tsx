import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { setStoredToken } from "../api.ts";

export default function AuthCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    const token = params.get("token");
    const error = params.get("error");

    if (error) {
      try {
        setMessage(decodeURIComponent(error));
      } catch {
        setMessage(error);
      }
      return;
    }

    if (token) {
      setStoredToken(token);
      navigate("/profile", { replace: true });
      return;
    }

    setMessage("Missing token or error from the server.");
  }, [navigate, params]);

  return (
    <div className="page narrow">
      <div className="card">
        <h1>Signing you in</h1>
        {message ? (
          <div className="alert error" role="alert">
            <strong>Could not complete sign-in.</strong>
            <p>{message}</p>
            <Link className="btn btn-secondary" to="/">
              Back to welcome
            </Link>
          </div>
        ) : (
          <p className="muted">Redirecting to your profile…</p>
        )}
      </div>
    </div>
  );
}
