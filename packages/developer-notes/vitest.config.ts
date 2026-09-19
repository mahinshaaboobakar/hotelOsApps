import { defineConfig } from "vitest/config";

/** The guard's own tests, in the DOM the applications' walks run in. */
export default defineConfig({
  test: {
    environment: "happy-dom",
    include: ["tests/**/*.test.ts"],
  },
});
