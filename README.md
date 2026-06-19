# FlyCompare - Metabuscador de Passagens Aéreas

Alunos: André Lucas Peterson Leal(0610663) / João Lucas Barbosa da Silva(06008695) / Miguel Soares dos Santos(06009538) / Pedro Neves Pinto Capozi(06010613) / Vinicius Rangel(06010696)

Sistema metabuscador de passagens aéreas desenvolvido com **C# Minimal API**, **Blazor WebAssembly**, **Dapper**, **SQLite** e **Hangfire**.

O FlyCompare pesquisa preços e rotas em múltiplas fontes online (sites de companhias aéreas, OTAs como Decolar) e apresenta os resultados consolidados para o usuário, permitindo comparação de preços, horários e escalas.

## Stack

- .NET 10
- SQLite (desenvolvimento) / SQL Server (produção — script em `db/script-flycompare.sql`)
- Dapper
- Blazor WebAssembly
- Hangfire (agendamento de jobs)
- xUnit
- Redis (cache distribuído, opcional)

## Pre-requisitos

- Git
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ (para `npm run`)
- SQLite (incluído no .NET — sem instalação adicional)
- Docker Desktop (opcional, apenas se quiser usar SQL Server em vez de SQLite)

## Rodar Em PC Limpo (Fluxo Principal)

Para quem nunca rodou nada na maquina (Windows), use somente:

```powershell
git clone https://github.com/Andrelealx/RedCode.git
cd Red-code-master
npm install
npm run dev
```

O `npm install` (Windows) faz setup automatico de dependencias com `winget` se faltar:

- Git
- Node.js/npm
- .NET SDK 10+
- Docker Desktop
- Inicializacao do Docker Desktop

Depois, o `npm run dev` faz automaticamente:

- Restaura os projetos .NET
- Inicia API (`http://localhost:5246`) e Front (`http://localhost:5139`)
- O banco SQLite (`redcode.db`) é criado automaticamente na primeira execução

Importante:

- No primeiro `npm install`, algumas instalacoes podem abrir prompt do Windows/winget.
- Se pedir para reabrir terminal apos instalacao, abra novamente e rode `npm install` e depois `npm run dev`.

Se voce ja tiver banco pronto e quiser subir so API + Front:

```powershell
npm run dev:apps
```

## Windows Totalmente Limpo (Opcional Manual)

Se a pessoa estiver em Windows e quiser setup guiado (instala dependencias via `winget`), rode:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup-local.ps1 -AutoInstall
```

Esse script:

- Instala (se faltar): Git, Node.js, .NET SDK 10+ e Docker Desktop
- Inicia o Docker Desktop
- Executa `npm install`
- Sobe tudo com `npm run dev`

Se quiser so preparar a maquina sem subir app:

```powershell
powershell -ExecutionPolicy Bypass -File .\setup-local.ps1 -AutoInstall -SkipRun
```

## Setup de Maquina Zero (Passo a Passo)

### 1. Clonar e atualizar

```powershell
git clone https://github.com/Andrelealx/RedCode.git
cd Red-code-master
git pull
```

### 2. Restaurar dependencias

```powershell
dotnet restore src/RedCodeApi/RedCodeApi.csproj
dotnet restore src/RedCodeFront/RedCodeFront.csproj
dotnet restore tests/RedCodeTests.csproj
```

### 3. Rodar API

```powershell
dotnet run --project src/RedCodeApi/RedCodeApi.csproj
```

API: `http://localhost:5246`

O banco SQLite (`redcode.db`) é criado automaticamente na primeira execução com tabelas e seed data (3 companhias, 15 aeroportos, 22 rotas).

### 4. Rodar Front-end

```powershell
dotnet run --project src/RedCodeFront/RedCodeFront.csproj --urls http://localhost:5139
```

Front-end: `http://localhost:5139`

### 5. (Opcional) Usar SQL Server em vez de SQLite

Se preferir SQL Server para ambiente mais próximo de produção:

```powershell
docker run --name redcode-sql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=RedCode@2024" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Depois execute o script SQL:

```powershell
sqlcmd -S localhost,1433 -U sa -P "RedCode@2024" -i db/script-flycompare.sql
```

E atualize a string de conexão em `src/RedCodeApi/appsettings.json`:

```json
"ConnectionStrings": {
  "RedCode": "Server=localhost,1433;Database=RedCode;User Id=sa;Password=RedCode@2024;TrustServerCertificate=True;"
}
```

### 6. Rodar testes

```powershell
dotnet test tests/RedCodeTests.csproj
```

### Scripts NPM disponiveis

```powershell
npm run dev       # sobe tudo (db + restore + api + front)
npm run dev:apps  # sobe somente api + front
npm run restore   # restore dos 3 projetos .NET
npm run test      # roda testes
npm run setup     # instala dependencias no Windows
npm run setup:win # setup guiado para Windows
```

## Endpoints da API

| Metodo | Rota | Descricao |
|--------|------|-----------|
| GET | `/api/aeroportos` | Listar aeroportos |
| GET | `/api/aeroportos/busca?q=` | Buscar aeroportos (autocomplete) |
| GET | `/api/companhias` | Listar companhias aereas |
| GET | `/api/rotas/populares` | Listar rotas populares |
| GET | `/api/voos/busca?origem=&destino=&dataPartida=` | Buscar voos (com scraping + cache) |
| GET | `/api/voos/precos/{vooId}` | Historico de precos de um voo |
| POST | `/api/alertas` | Criar alerta de preco |
| GET | `/api/alertas/{email}` | Listar alertas por email |

## Estrutura do Projeto

```text
Red-code-master/
|-- db/
|   |-- script-flycompare.sql      # Script SQL Server (produção)
|   `-- cleanup-legado.sql         # Cleanup das tabelas legadas
|-- docs/
|   |-- adr/                       # 5 ADRs (Architecture Decision Records)
|   |-- pivotagem/
|   |   |-- PIVOTAGEM.md           # Plano de pivotagem
|   |   |-- ROADMAP.md             # Roadmap completo
|   |   `-- REQUISITOS-FLYCOMPARE.md  # Requisitos do FlyCompare
|   |-- SPECS-FLYCOMPARE.md        # 33 SPECs técnicas
|   |-- ESTADO-DO-PROJETO.md       # Estado atual do projeto
|   |-- arquitetura.md             # Arquitetura do sistema
|   `-- visao.md                   # Visão geral
|-- src/
|   |-- RedCodeApi/            # API .NET Minimal API
|   |   |-- Data/                  # DbInitializer, MockVoosGenerator
|   |   |-- Dtos/FlyCompare/       # DTOs de request/response
|   |   |-- Endpoints/             # Endpoints modulares (Aeroportos, Voos, Alertas, etc.)
|   |   |-- Models/FlyCompare/     # Modelos do domínio de voos
|   |   |-- Services/
|   |   |   |-- Scrapers/          # Scrapers de companhias aéreas + Normalizador
|   |   |   |-- CacheService.cs    # Serviço de cache (memória + Redis)
|   |   |   `-- ScrapingScheduler.cs # Jobs Hangfire
|   |   `-- Program.cs             # Pipeline de configuração (93 linhas)
|   |-- RedCodeFront/          # Frontend Blazor WASM
|   |   |-- Pages/
|   |   |   |-- Index.razor        # Página inicial (home)
|   |   |   |-- BuscarVoos.razor   # Busca de passagens
|   |   |   |-- ResultadosBusca.razor  # Resultados da busca
|   |   |   `-- MeusAlertas.razor  # Gerenciamento de alertas
|   |   |-- Shared/
|   |   |   |-- MainLayout.razor   # Layout principal com sidebar
|   |   |   `-- Alerta.razor       # Componente de alerta reutilizável
|   |   `-- Models/
|   |       `-- FlyCompare/        # Modelos do frontend
|-- tests/                         # Testes xUnit (27 testes)
`-- scripts/
    |-- dev-all.mjs                # Script de dev
    `-- postinstall.mjs
```

## Cache

O FlyCompare utiliza cache em duas camadas:

1. **Memory Cache** (sempre disponivel): fallback padrao para resultados de busca
2. **Redis** (opcional): cache distribuido, configurado via `appsettings.json`

Para habilitar Redis, adicione ao `appsettings.json`:

```json
"Redis": {
  "ConnectionString": "localhost:6379"
}
```

## Scraping Automatizado

O Hangfire gerencia jobs recorrentes de scraping:

- **Scraping de rotas populares**: a cada 6 horas
- **Verificacao de alertas de preco**: a cada 2 horas

Os scrapers disponiveis:
- Latam Airlines
- GOL Linhas Aereas
- Azul Linhas Aereas
- Decolar (via PuppeteerSharp)
