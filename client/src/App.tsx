import { Navigate, Route, Routes } from "react-router-dom";
import AuthCallbackPage from "./pages/AuthCallbackPage.tsx";
import ProfilePage from "./pages/ProfilePage.tsx";
import WelcomePage from "./pages/WelcomePage.tsx";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<WelcomePage />} />
      <Route path="/auth/callback" element={<AuthCallbackPage />} />
      <Route path="/profile" element={<ProfilePage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
