#!/usr/bin/env node

import { spawnSync } from "node:child_process";
import process from "node:process";

if (process.platform !== "win32") {
  console.log("[postinstall] Plataforma nao-Windows: pulando setup automatico.");
  process.exit(0);
}

console.log("[postinstall] Executando setup local de dependencias (Windows)...");

const result = spawnSync(
  "powershell",
  [
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    "./setup-local.ps1",
    "-AutoInstall",
    "-SkipRun",
    "-SkipNpmInstall"
  ],
  {
    stdio: "inherit",
    shell: false
  }
);

if (typeof result.status === "number") {
  process.exit(result.status);
}

process.exit(1);
