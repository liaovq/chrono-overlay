import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const props = readFileSync(resolve(import.meta.dirname, "../Directory.Build.props"), "utf8");
const version = props.match(/<VersionPrefix>([^<]+)<\/VersionPrefix>/)?.[1];

if (!version) {
  throw new Error("Unable to read VersionPrefix from Directory.Build.props");
}

const releaseAsset = `ChronoOverlay-v${version}-win-x64.exe`;
const downloadUrl = `https://github.com/liaovq/chrono-overlay/releases/download/v${version}/${releaseAsset}`;

export default defineConfig({
  base: "/chrono-overlay/",
  define: {
    __APP_VERSION__: JSON.stringify(version),
    __DOWNLOAD_URL__: JSON.stringify(downloadUrl),
  },
  optimizeDeps: {
    include: ["react", "react-dom/client"],
  },
  server: {
    host: "0.0.0.0",
    allowedHosts: ["terminal.local"],
    warmup: {
      clientFiles: ["./src/main.jsx"],
    },
  },
  plugins: [react()],
});
