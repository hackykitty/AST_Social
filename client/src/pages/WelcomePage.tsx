import { getOAuthApiBaseUrl } from "../api.ts";

export default function WelcomePage() {
  const api = getOAuthApiBaseUrl();

  return (
    <div className="page welcome">
      <div className="card hero">
        <p className="eyebrow">Social Connect</p>
        <h1>Welcome</h1>
        <p className="lede">
          Link your social account to view your profile and recent activity in one place.
        </p>
        <div className="actions">
          <a className="btn btn-li" href={`${api}/api/auth/linkedin`}>
            Connect with LinkedIn
          </a>
          <a className="btn btn-fb" href={`${api}/api/auth/facebook`}>
            Connect with Facebook
          </a>
          <a className="btn btn-tw" href={`${api}/api/auth/twitter`}>
            Connect with Twitter / X
          </a>
        </div>
        <p className="hint">
          You will complete sign-in on the provider&apos;s site, then return here to your profile.
        </p>
      </div>
    </div>
  );
}
