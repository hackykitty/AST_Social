# Social Connect

Sample social app: **ASP.NET Core 8** API with OAuth to **Facebook**, **Twitter / X**, and **LinkedIn**, plus a **React (Vite + TypeScript)** front end.

**Sign-in status:** I received the assessment on short notice and prioritized shipping a working vertical slice quickly. **LinkedIn** is the only provider I finished end-to-end (OAuth → JWT → profile + parsing/tests) to demonstrate architecture, API integration, and polish within that window. **Facebook** and **Twitter / X** are implemented in the API and UI (same patterns as LinkedIn) but were not fully exercised with live credentials—add keys and follow each provider’s portal steps to enable them.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Node.js 18+ (for the React client)

## Provider apps (required for real logins)

Register each provider and add the **OAuth redirect URI** that matches your API base URL and the middleware callback path:

| Provider   | Typical callback URL (this template, port 5288)   |
|-----------|-----------------------------------------------------|
| Facebook  | `http://localhost:5288/signin-facebook`            |
| Twitter   | `http://localhost:5288/signin-twitter`             |
| LinkedIn  | `http://localhost:5288/signin-linkedin`            |

Use HTTPS and the same paths in production, and set **Frontend:BaseUrl** to your SPA origin so redirects after login land on the correct site.

### Configuration

Set secrets via [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets), environment variables, or `appsettings.Development.json` (do not commit real secrets):

- `Authentication:Facebook:AppId`, `Authentication:Facebook:AppSecret`
- `Authentication:Twitter:ConsumerKey`, `Authentication:Twitter:ConsumerSecret`
- `Authentication:LinkedIn:ClientId`, `Authentication:LinkedIn:ClientSecret`
- `Jwt:SigningKey` — at least **32 characters** (override the placeholder in `appsettings.json` for production)
- `Frontend:BaseUrl` — e.g. `http://localhost:5173`

If a provider’s keys are missing, `GET /api/auth/{provider}` returns **503** with a JSON error instead of throwing.

### API and product limitations (important)

- **Facebook**: Email and feed content depend on **approved permissions** and what the user grants. `mobile_phone` is rarely available without extra permissions.
- **Twitter / X**: Timeline and profile calls require appropriate **API access**; free tiers may block `/2/users/:id/tweets` or related fields.
- **LinkedIn**: In the [LinkedIn app](https://www.linkedin.com/developers/apps), add the product **Sign In with LinkedIn using OpenID Connect** (not only the legacy Sign In product). Use **`AspNet.Security.OAuth.LinkedIn` 8.1.0+**, which calls `https://api.linkedin.com/v2/userinfo`; older versions used `/v2/me` and fail with `ACCESS_DENIED` / `me.GET.NO_VERSION`. **Shares / posts** still often need extra products/scopes and may return an empty list.

The UI shows a **posts note** when the network returns no posts or the app lacks scope.

## Run the API

```bash
cd src/SocialApp.Api
dotnet restore
dotnet run
```

Default URL in `launchSettings.json`: `http://localhost:5288`.

## Run the SPA

```bash
cd client
npm install
npm run dev
```

The Vite dev server proxies `/api` to `http://localhost:5288`. OAuth links still use the API origin (`http://localhost:5288` by default) so redirect URIs match the API.

Optional: set `VITE_API_BASE_URL` (see `client/.env.example`) if the API is not on localhost:5288.

## Tests

```bash
dotnet test
```

## Security notes (production)

- Replace JWT signing keys and use **HTTPS** everywhere.
- Passing tokens in query strings is convenient for local development; for production, prefer **PKCE**, **short-lived codes**, or a **BFF** pattern with **HttpOnly** cookies.
- OAuth access tokens are stored **in memory** server-side (per session id); scale-out requires a shared store (e.g. Redis).

## Solution layout

- `src/SocialApp.Api` — Web API, OAuth, JWT, profile aggregation.
- `client` — React UI (welcome, callback, profile).
- `tests/SocialApp.Tests` — xUnit tests.
