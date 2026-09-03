import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

/**
 * The UI module's tests.
 *
 * `happy-dom` rather than a browser: these assert structure and rules, never
 * layout or colour. What a suite cannot see is exactly what the capture harness
 * in `preview/` exists for, and neither substitutes for the other.
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
