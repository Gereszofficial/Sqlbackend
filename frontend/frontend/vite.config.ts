import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  preview: {
    host: "0.0.0.0",
    allowedHosts: ["sqltraining.up.railway.app"],
  },
  resolve: {
    alias: {
      "@": "/src",
    },
  },
});