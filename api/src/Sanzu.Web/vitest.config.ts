import path from "node:path";
import { defineConfig } from "vitest/config";

export default defineConfig({
  esbuild: {
    jsx: "automatic"
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname)
    }
  },
  test: {
    include: ["tests/unit/**/*.test.ts?(x)"],
    exclude: ["tests/e2e/**"]
  }
});
