# TicketPrime - Engine de Ingressos

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
- SQL Server acessivel em `localhost,1433`
- `sqlcmd` (opcional, mas recomendado para setup rapido do banco)
- Docker Desktop (opcional, recomendado se voce nao tiver SQL Server local)

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
