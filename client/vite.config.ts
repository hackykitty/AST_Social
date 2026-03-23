import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    // Bind to IPv4 + IPv6 so http://localhost:5173 works when the OS resolves localhost to 127.0.0.1
    host: true,
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5288",
        changeOrigin: true,
      },
    },
  },
});
