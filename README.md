# TicketPrime - Engine de Ingressos

 
 Alunos: João Lucas Barbosa da Silva(06008695)/ Pedro Neves Pinto Capozi(06010613)/ André Lucas Peterson Leal(0610663)/ Miguel Soares dos Santos(06009538)/ Vinicius Rangel(06010696)

Sistema de bilheteria desenvolvido com **C# Minimal API**, **Blazor WebAssembly**, **Dapper** e **SQL Server**.

## Stack

- .NET 10
- SQL Server
- Dapper
- Blazor WebAssembly
- xUnit

## Pre-requisitos

- Git
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Node.js 20+ (para `npm run`)
- SQL Server acessivel em `localhost,1433`
- `sqlcmd` (opcional, mas recomendado para setup rapido do banco)
- Docker Desktop (opcional, recomendado se voce nao tiver SQL Server local)

## Rodar Em PC Limpo (Fluxo Principal)

Para quem nunca rodou nada na maquina (Windows), use somente:

```powershell
git clone https://github.com/Andrelealx/TicketPrime.git
cd TicketPrime
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

- Sobe (ou cria) o container `ticketprime-sql` no Docker
- Aguarda SQL Server ficar pronto
- Executa `db/script.sql`
- Restaura os projetos .NET
- Inicia API (`http://localhost:5246`) e Front (`http://localhost:5139`)

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
git clone https://github.com/Andrelealx/TicketPrime.git
cd TicketPrime
git pull
```

### 2. Restaurar dependencias

```powershell
dotnet restore src/TicketPrimeApi/TicketPrimeApi.csproj
dotnet restore src/TicketPrimeFront/TicketPrimeFront.csproj
dotnet restore tests/TicketPrimeTests.csproj
```

### 3. Subir SQL Server (se necessario)

Opcao recomendada para ambiente novo:

```powershell
docker run --name ticketprime-sql `
  -e "ACCEPT_EULA=Y" `
  -e "MSSQL_SA_PASSWORD=TicketPrime@2024" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Se voce ja tiver SQL Server local, garanta que ele esteja acessivel em `localhost,1433`.

### 4. Criar banco e tabelas

```powershell
sqlcmd -S localhost,1433 -U sa -P "TicketPrime@2024" -i db/script.sql
```

Se voce nao tiver `sqlcmd`, abra o arquivo `db/script.sql` no SSMS/Azure Data Studio e execute manualmente.

O script `db/script.sql` e idempotente, entao pode ser executado mais de uma vez sem quebrar.

### 5. Configurar string de conexao (se precisar)

Padrao atual do backend em `src/TicketPrimeApi/appsettings.Development.json`:

```json
"ConnectionStrings": {
  "TicketPrime": "Server=localhost,1433;Database=TicketPrime;User Id=sa;Password=TicketPrime@2024;TrustServerCertificate=True;"
}
```

Se quiser sobrescrever sem editar arquivo:

```powershell
$env:ConnectionStrings__TicketPrime = "Server=localhost,1433;Database=TicketPrime;User Id=sa;Password=SuaSenhaAqui;TrustServerCertificate=True;"
```

### 6. Rodar API

```powershell
dotnet run --project src/TicketPrimeApi/TicketPrimeApi.csproj
```

API: `http://localhost:5246`

### 7. Rodar Front-end

```powershell
dotnet run --project src/TicketPrimeFront/TicketPrimeFront.csproj --urls http://localhost:5139
```

Front-end: `http://localhost:5139`

Base URL da API no front:

- Arquivo: `src/TicketPrimeFront/wwwroot/appsettings.json`
- Chave: `ApiBaseUrl`

### 8. Rodar testes

```powershell
dotnet test tests/TicketPrimeTests.csproj
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
| POST | `/api/usuarios` | Cadastrar usuario |
| POST | `/api/eventos` | Cadastrar evento |
| GET | `/api/eventos` | Listar eventos |
| GET | `/api/eventos/{id}` | Buscar evento por ID |
| POST | `/api/cupons` | Cadastrar cupom |
| GET | `/api/cupons/{codigo}` | Buscar cupom por codigo |
| POST | `/api/reservas` | Realizar reserva |
| GET | `/api/reservas/{cpf}` | Consultar reservas por CPF |

## Estrutura do Projeto

```text
TicketPrime/
|-- db/
|   `-- script.sql
|-- docs/
|   `-- requisitos.md
|-- src/
|   |-- TicketPrimeApi/
|   `-- TicketPrimeFront/
|-- tests/
|   `-- TicketPrimeTests.csproj
`-- README.md
```

## Solucao de Problemas Rapida

- Erro de conexao no backend:
  - Verifique se o SQL Server esta ativo em `localhost,1433`
  - Confirme usuario/senha e a `ConnectionStrings:TicketPrime`
- Front nao carrega dados:
  - Garanta API rodando em `http://localhost:5246`
  - Confira `ApiBaseUrl` em `src/TicketPrimeFront/wwwroot/appsettings.json`
- Porta em uso:
  - Troque com `--urls` no `dotnet run` e ajuste configuracoes correspondentes
