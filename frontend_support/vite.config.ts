import { defineConfig } from "vite";

// base: './' — WebView2 가상호스트(http://pfsupport.local/) 하위에서 상대경로로 에셋 로드.
// 폐쇄망: 모든 의존성 번들로 dist 자체포함(CDN 미사용).
export default defineConfig({
  base: "./",
  build: {
    outDir: "dist",
    emptyOutDir: true,
    target: "es2020"
  },
  server: {
    port: 5174
  }
});
