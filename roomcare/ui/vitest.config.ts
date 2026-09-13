import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

/**
 * The UI module's tests — `happy-dom`, structure and rules, never layout or
 * colour; the capture harness in `preview/` is for what a suite cannot see.
 */
export default defineConfig({
  test: {
    environment: "happy-dom",
    include: ["tests/**/*.test.ts"],
  },
  resolve: {
    alias: {
      "@hotelos/sdk": fileURLToPath(
        new URL("../../../HosPilotOS/packages/sdk-typescript/src/index.ts", import.meta.url),
      ),
    },
  },
});
