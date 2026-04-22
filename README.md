# TicketPrime — Engine de Ingressos
Alunos: João Lucas Barbosa da Silva(06008695)/ Pedro Neves Pinto Capozi(06010613)/ André Lucas Peterson Leal(0610663)/ Miguel Soares()/ Vinicius Rangel(06010696)

> Disciplina: Engenharia de Software | Prof. Dr. André Campos

Sistema de bilheteria desenvolvido com **C# Minimal API**, **Blazor WebAssembly**, **Dapper** e **SQL Server**.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (Express ou superior) instalado localmente
- SQL Server Management Studio (SSMS) ou Azure Data Studio (opcional)

---

## 1. Configurar o Banco de Dados

Abra o SSMS e execute o script abaixo (ou rode via terminal):

```sql
-- Execute o arquivo: db/script.sql
```

```powershell
# Via terminal (sqlcmd)
sqlcmd -S localhost\SQLEXPRESS -i db\script.sql
```

O script cria o banco `TicketPrime` com as tabelas:
- `Usuarios` (CPF, Nome, Email)
- `Eventos` (Id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao)
- `Cupons` (Codigo, PorcentagemDesconto, ValorMinimoRegra)
- `Reservas` (Id, UsuarioCpf, EventoId, CupomUtilizado, ValorFinalPago, DataReserva)

---

## 2. Executar a API (Backend)

```powershell
# Terminal 1 — entre na pasta da API
cd src\TicketPrimeApi

# Execute a aplicação
dotnet run
```

A API estará disponível em: `http://localhost:5246`

### Endpoints disponíveis

| Método | Rota | Descrição |
|--------|------|-----------|
| POST | `/api/usuarios` | Cadastrar usuário |
| POST | `/api/eventos` | Cadastrar evento |
| GET | `/api/eventos` | Listar todos os eventos |
| GET | `/api/eventos/{id}` | Buscar evento por ID |
| POST | `/api/cupons` | Cadastrar cupom |
| GET | `/api/cupons/{codigo}` | Buscar cupom por código |
| POST | `/api/reservas` | Realizar reserva |
| GET | `/api/reservas/{cpf}` | Consultar reservas por CPF (com JOIN) |

---

## 3. Executar o Front-end (Blazor)

```powershell
# Terminal 2 — entre na pasta do front
cd src\TicketPrimeFront

# Execute a aplicação
dotnet run
```

O front-end abrirá automaticamente no navegador. Caso não abra, acesse: `http://localhost:5139`

> **Importante:** A API deve estar rodando (passo 2) para o front-end funcionar corretamente.

---

## 4. Executar os Testes

```powershell
# Entre na pasta de testes
cd tests

# Execute os testes
dotnet test
```

Os testes cobrem:
- Cálculo matemático do desconto (R4)
- Regra de valor mínimo do cupom (R4)
- Lógica de overbooking (R3)
- Limite de reservas por CPF (R2)
- Validação de CPF (11 dígitos)
- Validação de formato de e-mail
- Validação de porcentagem do cupom

---

## 5. Estrutura de Pastas

```
TicketPrime/
├── db/
│   └── script.sql          # Script CREATE TABLE de todas as entidades
├── docs/
│   └── requisitos.md       # Histórias de usuário e critérios BDD
├── src/
│   ├── TicketPrimeApi/     # Minimal API C# com Dapper
│   └── TicketPrimeFront/   # Blazor WebAssembly
├── tests/
│   ├── UnitTest1.cs        # Testes xUnit (Fact e Theory)
│   └── TicketPrimeTests.csproj
└── README.md
```

---

## 6. Regras de Negócio

| Regra | Descrição |
|-------|-----------|
| **R1 — Integridade** | `POST /api/reservas` valida existência de `UsuarioCpf` e `EventoId` antes do INSERT |
| **R2 — Limite por CPF** | Máximo 2 reservas por CPF no mesmo evento (bloqueia a 3ª tentativa) |
| **R3 — Estoque** | Bloqueia reservas quando total >= `CapacidadeTotal` do evento |
| **R4 — Motor de Cupons** | Desconto aplicado somente se `PrecoPadrao >= ValorMinimoRegra` do cupom |

---

## 7. Segurança

- Todas as queries usam **parâmetros Dapper** (`@NomeParam`) — zero SQL Injection
- Conexão via **Windows Authentication** (`Trusted_Connection=True`) — sem senha em texto plano
- **Entity Framework** não utilizado — apenas Dapper com SQL manual
