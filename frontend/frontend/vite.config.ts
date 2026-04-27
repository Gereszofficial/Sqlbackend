import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import basicSsl from "@vitejs/plugin-basic-ssl";

export default defineConfig({
  plugins: [vue(), basicSsl()],
  server: {
    https: true,
    host: "localhost",
    port: 5173,
    proxy: {
      "/api": {
        target: "https://localhost:64628",
        changeOrigin: true,
        secure: false,
      },
    },
  },
  resolve: {
    alias: {
      "@": "/src",
    },
  },
});
