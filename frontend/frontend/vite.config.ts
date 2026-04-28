import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";

export default defineConfig({
  plugins: [vue()],
  preview: {
    host: "0.0.0.0",
    port: parseInt(process.env.PORT || "4173")
  },
  resolve: {
    alias: {
      "@": "/src",
    },
  },
});