import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";
import plugin from "@vitejs/plugin-react";
import fs from "fs";
import path from "path";
import child_process from "child_process";
import { env } from "process";

const baseFolder =
  env.APPDATA !== undefined && env.APPDATA !== ""
    ? `${env.APPDATA}/ASP.NET/http`
    : `${env.HOME}/.aspnet/http`;

const certificateName = "Onion.CleanArchitecture.WebApp.Client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
  if (
    0 !==
    child_process.spawnSync(
      "dotnet",
      [
        "dev-certs",
        "https",
        "--export-path",
        certFilePath,
        "--format",
        "Pem",
        "--no-password",
      ],
      { stdio: "inherit" }
    ).status
  ) {
    throw new Error("Could not create certificate.");
  }
}

// const target = env.ASPNETCORE_HTTPS_PORT
//   ? `https://localhost:7058`
//   : env.ASPNETCORE_URLS
//     ? env.ASPNETCORE_URLS.split(";")[0]
//     : "http://localhost:5220";
const target = "https://localhost:7058"
// https://vitejs.dev/config/
export default defineConfig({
  plugins: [plugin()],
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url)),
      "@providers": path.resolve(__dirname, "./src/providers"),
      "@authens": path.resolve(__dirname, "./src/pages/authens"),
      "@components": path.resolve(__dirname, "./src/components"),
      "@containers": path.resolve(__dirname, "./src/containers"),
      "@features": path.resolve(__dirname, "./src/features"),
      "@pages": path.resolve(__dirname, "./src/pages"),
      "@routes": path.resolve(__dirname, "./src/routes"),
      "@utilities": path.resolve(__dirname, "./src/utilities"),
      "@config": path.resolve(__dirname, "./src/config"),
    },
  },
  server: {
    proxy: {
      "^/weatherforecast": {
        target,
        secure: false,
      },
      "^/api": {
        target,
        secure: false,
      },
      "^/hubs": {
        target: "https://localhost:7201",
        secure: false,
        ws: true,
      },
    },
    port: 5173,
    https: {
      key: fs.readFileSync(keyFilePath),
      cert: fs.readFileSync(certFilePath),
    },
  },
});
