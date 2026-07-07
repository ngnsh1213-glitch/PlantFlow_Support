import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// base: './' - WebView2 virtual host asset loading.
// P1 keeps index.html as the Preview B entry and adds catalog.html as a separate React entry.
export default defineConfig({
  base: "./",
  plugins: [react()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
    target: "es2020",
    rollupOptions: {
      input: {
        preview: "index.html",
        catalog: "catalog.html"
      }
    }
  },
  server: {
    port: 5174
  }
});
