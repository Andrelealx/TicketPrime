#!/usr/bin/env node

import { spawn } from "node:child_process";
import { existsSync } from "node:fs";
import { resolve } from "node:path";
import process from "node:process";

const args = new Set(process.argv.slice(2));
const skipDb = args.has("--skip-db");
const skipRestore = args.has("--skip-restore");
const smokeMode = args.has("--smoke");

const SQL_CONTAINER = "ticketprime-sql";
const SQL_PASSWORD = "TicketPrime@2024";
const DB_SCRIPT = resolve("db/script.sql");
const API_PROJECT = "src/TicketPrimeApi/TicketPrimeApi.csproj";
const FRONT_PROJECT = "src/TicketPrimeFront/TicketPrimeFront.csproj";
const TEST_PROJECT = "tests/TicketPrimeTests.csproj";
const API_URL = "http://localhost:5246";
const FRONT_URL = "http://localhost:5139";

function log(message) {
  console.log(`[dev] ${message}`);
}

function sleep(ms) {
  return new Promise((resolvePromise) => setTimeout(resolvePromise, ms));
}

function run(command, commandArgs, options = {}) {
  const {
    inherit = false,
    ignoreError = false,
    cwd = process.cwd()
  } = options;

  return new Promise((resolvePromise, rejectPromise) => {
    const child = spawn(command, commandArgs, {
      cwd,
      shell: false,
      stdio: inherit ? "inherit" : ["ignore", "pipe", "pipe"]
    });

    let stdout = "";
    let stderr = "";

    if (!inherit && child.stdout) {
      child.stdout.on("data", (data) => {
        stdout += data.toString();
      });
    }

    if (!inherit && child.stderr) {
      child.stderr.on("data", (data) => {
        stderr += data.toString();
      });
    }

    child.on("error", (error) => {
      rejectPromise(error);
    });

    child.on("close", (code) => {
      const result = { code, stdout, stderr };
      if (code === 0 || ignoreError) {
        resolvePromise(result);
        return;
      }
      const error = new Error(
        `${command} ${commandArgs.join(" ")} falhou com codigo ${code}.`
      );
      error.result = result;
      rejectPromise(error);
    });
  });
}

async function ensureDockerAvailable() {
  const result = await run("docker", ["--version"], { ignoreError: true });
  if (result.code !== 0) {
    throw new Error(
      "Docker nao encontrado. Instale/inicie o Docker Desktop ou rode com --skip-db."
    );
  }
}

async function ensureSqlContainer() {
  await ensureDockerAvailable();
  log("Verificando container SQL...");

  const existing = await run("docker", [
    "ps",
    "-a",
    "--filter",
    `name=^/${SQL_CONTAINER}$`,
    "--format",
    "{{.Names}}"
  ]);

  if (existing.stdout.trim() !== SQL_CONTAINER) {
    log("Criando container SQL Server...");
    await run("docker", [
      "run",
      "--name",
      SQL_CONTAINER,
      "-e",
      "ACCEPT_EULA=Y",
      "-e",
      `MSSQL_SA_PASSWORD=${SQL_PASSWORD}`,
      "-p",
      "1433:1433",
      "-d",
      "mcr.microsoft.com/mssql/server:2022-latest"
    ], { inherit: true });
  } else {
    log("Iniciando container SQL existente...");
    await run("docker", ["start", SQL_CONTAINER], { inherit: true, ignoreError: true });
  }
}

async function waitForSqlReady() {
  log("Aguardando SQL Server ficar pronto...");
  for (let i = 0; i < 60; i += 1) {
    const result = await run("docker", [
      "exec",
      SQL_CONTAINER,
      "/opt/mssql-tools18/bin/sqlcmd",
      "-S",
      "localhost",
      "-U",
      "sa",
      "-P",
      SQL_PASSWORD,
      "-C",
      "-Q",
      "SELECT 1"
    ], { ignoreError: true });

    if (result.code === 0) {
      log("SQL Server pronto.");
      return;
    }

    await sleep(2000);
  }

  throw new Error("SQL Server nao ficou pronto a tempo.");
}

async function applyDatabaseScript() {
  if (!existsSync(DB_SCRIPT)) {
    throw new Error(`Script de banco nao encontrado em ${DB_SCRIPT}.`);
  }

  log("Aplicando db/script.sql...");
  await run("docker", ["cp", DB_SCRIPT, `${SQL_CONTAINER}:/tmp/script.sql`], { inherit: true });
  await run("docker", [
    "exec",
    SQL_CONTAINER,
    "/opt/mssql-tools18/bin/sqlcmd",
    "-S",
    "localhost",
    "-U",
    "sa",
    "-P",
    SQL_PASSWORD,
    "-C",
    "-i",
    "/tmp/script.sql"
  ], { inherit: true });
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
    shell: false,
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
  if (!child || child.exitCode !== null || !child.pid) {
    return;
  }

  if (process.platform === "win32") {
    spawn("taskkill", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
    return;
  }

  child.kill("SIGTERM");
}

async function main() {
  log("Iniciando ambiente local...");

  if (!skipDb) {
    await ensureSqlContainer();
    await waitForSqlReady();
    await applyDatabaseScript();
  } else {
    log("Pulando etapa de banco (--skip-db).");
  }

  if (!skipRestore) {
    await restoreDotnet();
  } else {
    log("Pulando restore (--skip-restore).");
  }

  log("Subindo API e Front-end...");
  const apiProcess = spawnApp("dotnet", ["run", "--project", API_PROJECT]);
  const frontProcess = spawnApp("dotnet", [
    "run",
    "--project",
    FRONT_PROJECT,
    "--urls",
    FRONT_URL
  ]);

  let shuttingDown = false;

  const shutdown = (reason) => {
    if (shuttingDown) {
      return;
    }
    shuttingDown = true;
    log(`Encerrando processos (${reason})...`);
    killProcessTree(apiProcess);
    killProcessTree(frontProcess);
  };

  process.on("SIGINT", () => {
    shutdown("SIGINT");
    process.exit(0);
  });

  process.on("SIGTERM", () => {
    shutdown("SIGTERM");
    process.exit(0);
  });

  apiProcess.on("exit", (code) => {
    if (!shuttingDown) {
      shutdown(`API saiu com codigo ${code ?? 0}`);
      process.exit(code ?? 1);
    }
  });

  frontProcess.on("exit", (code) => {
    if (!shuttingDown) {
      shutdown(`Front saiu com codigo ${code ?? 0}`);
      process.exit(code ?? 1);
    }
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
  if (error.result?.stderr) {
    console.error(error.result.stderr.trim());
  }
  process.exit(1);
});
