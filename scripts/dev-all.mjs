#!/usr/bin/env node

import { spawn } from "node:child_process";
import { rmSync } from "node:fs";
import { resolve } from "node:path";
import process from "node:process";

const args = new Set(process.argv.slice(2));
const skipRestore = args.has("--skip-restore");
const smokeMode = args.has("--smoke");

const API_PROJECT = "src/RedCodeApi/RedCodeApi.csproj";
const FRONT_PROJECT = "src/RedCodeFront/RedCodeFront.csproj";
const TEST_PROJECT = "tests/RedCodeTests.csproj";
const API_URL = "http://localhost:5246";
const FRONT_URL = "http://localhost:5139";
const DB_FILE = resolve("src/RedCodeApi/bin/Debug/net10.0/redcode.db");

function log(message) {
  console.log(`[dev] ${message}`);
}

function sleep(ms) {
  return new Promise((resolvePromise) => setTimeout(resolvePromise, ms));
}

function run(command, commandArgs, options = {}) {
  const { inherit = false, ignoreError = false, cwd = process.cwd() } = options;

  return new Promise((resolvePromise, rejectPromise) => {
    let settled = false;
    const child = spawn(command, commandArgs, {
      cwd,
      shell: true,
      stdio: inherit ? "inherit" : ["ignore", "pipe", "pipe"]
    });

    let stdout = "";
    let stderr = "";

    if (!inherit && child.stdout) {
      child.stdout.on("data", (data) => { stdout += data.toString(); });
    }
    if (!inherit && child.stderr) {
      child.stderr.on("data", (data) => { stderr += data.toString(); });
    }

    child.on("error", (error) => {
      if (settled) return;
      settled = true;
      if (ignoreError) {
        resolvePromise({ code: 127, stdout, stderr: `${stderr}\n${error.message}`.trim() });
        return;
      }
      rejectPromise(error);
    });

    child.on("close", (code) => {
      if (settled) return;
      settled = true;
      const result = { code, stdout, stderr };
      if (code === 0 || ignoreError) {
        resolvePromise(result);
        return;
      }
      const error = new Error(`${command} ${commandArgs.join(" ")} falhou com codigo ${code}.`);
      error.result = result;
      rejectPromise(error);
    });
  });
}

async function restoreDotnet() {
  log("Restaurando dependencias .NET...");
  await run("dotnet", ["restore", API_PROJECT], { inherit: true });
  await run("dotnet", ["restore", FRONT_PROJECT], { inherit: true });
  await run("dotnet", ["restore", TEST_PROJECT], { inherit: true });
}

function spawnApp(command, commandArgs) {
  return spawn(command, commandArgs, {
    cwd: process.cwd(),
    shell: true,
    stdio: "inherit"
  });
}

async function waitForHttp(url, tries = 45, intervalMs = 1000) {
  for (let i = 0; i < tries; i += 1) {
    try {
      const response = await fetch(url);
      return response.status;
    } catch {
      await sleep(intervalMs);
    }
  }
  throw new Error(`Nao foi possivel acessar ${url}.`);
}

function killProcessTree(child) {
  if (!child || child.exitCode !== null || !child.pid) return;
  if (process.platform === "win32") {
    spawn("taskkill", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
    return;
  }
  child.kill("SIGTERM");
}

async function main() {
  log("=== FlyCompare - Modo SQLite (sem Docker) ===");
  log("O banco SQLite sera criado automaticamente ao iniciar a API.");

  // Remove DB anterior para garantir dados frescos (opcional)
  // Comentado para preservar dados entre execucoes
  // try { rmSync(DB_FILE); log("Banco anterior removido."); } catch {}

  if (!skipRestore) {
    await restoreDotnet();
  } else {
    log("Pulando restore (--skip-restore).");
  }

  log("Subindo API e Front-end...");
  const apiProcess = spawnApp("dotnet", ["run", "--project", API_PROJECT]);
  const frontProcess = spawnApp("dotnet", ["run", "--project", FRONT_PROJECT, "--urls", FRONT_URL]);

  let shuttingDown = false;

  const shutdown = (reason) => {
    if (shuttingDown) return;
    shuttingDown = true;
    log(`Encerrando processos (${reason})...`);
    killProcessTree(apiProcess);
    killProcessTree(frontProcess);
  };

  process.on("SIGINT", () => { shutdown("SIGINT"); process.exit(0); });
  process.on("SIGTERM", () => { shutdown("SIGTERM"); process.exit(0); });

  apiProcess.on("exit", (code) => {
    if (!shuttingDown) shutdown(`API saiu com codigo ${code ?? 0}`);
  });

  frontProcess.on("exit", (code) => {
    if (!shuttingDown) shutdown(`Front saiu com codigo ${code ?? 0}`);
  });

  if (smokeMode) {
    const [apiStatus, frontStatus] = await Promise.all([
      waitForHttp(API_URL),
      waitForHttp(FRONT_URL)
    ]);
    log(`Smoke test OK (API=${apiStatus}, FRONT=${frontStatus}).`);
    shutdown("smoke test finalizado");
    await sleep(500);
    process.exit(0);
  }

  log(`API: ${API_URL}`);
  log(`Front-end: ${FRONT_URL}`);
  log("Pressione Ctrl+C para encerrar.");
}

main().catch((error) => {
  console.error(`[dev] Erro: ${error.message}`);
  if (error.result?.stderr) console.error(error.result.stderr.trim());
  process.exit(1);
});
