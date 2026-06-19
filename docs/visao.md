# FlyCompare — Visão Geral do Projeto

> **Metabuscador de Passagens Aéreas** | .NET 10 + Blazor WebAssembly + SQLite

---

## Índice

1. [Propósito do Sistema](#1-propósito-do-sistema)
2. [História do Projeto](#2-história-do-projeto)
3. [Funcionalidades](#3-funcionalidades)
4. [Stack de Tecnologias](#4-stack-de-tecnologias)
5. [Arquitetura](#5-arquitetura)
6. [Estrutura do Projeto](#6-estrutura-do-projeto)
7. [Modelo de Dados](#7-modelo-de-dados)
8. [Endpoints da API](#8-endpoints-da-api)
9. [Frontend — Páginas e Rotas](#9-frontend--páginas-e-rotas)
10. [Estratégia de Scraping](#10-estratégia-de-scraping)
11. [Sistema de Cache](#11-sistema-de-cache)
12. [Jobs Agendados (Hangfire)](#12-jobs-agendados-hangfire)
13. [Padrões de Projeto](#13-padrões-de-projeto)
14. [Considerações de Segurança e Resiliência](#14-considerações-de-segurança-e-resiliência)
15. [Roadmap](#15-roadmap)

---

## 1. Propósito do Sistema

O **FlyCompare** é um **metabuscador de passagens aéreas** que permite ao usuário **pesquisar, comparar e monitorar preços de voos** de múltiplas companhias aéreas em um único lugar.

Diferente de OTAs (Online Travel Agencies) como Decolar ou Skyscanner, o FlyCompare atua como um **agregador de dados**: ele não vende passagens diretamente, mas coleta preços via web scraping de sites oficiais (LATAM, GOL, Azul) e OTAs (Decolar), normaliza os resultados e os apresenta de forma unificada.

### Problema Resolvido

- Usuários precisam visitar **4+ sites diferentes** para comparar preços de voos
- Cada site tem **interface, filtros e experiência** diferentes
- Não há uma forma simples de **monitorar queda de preços** automaticamente
- Ferramentas existentes (Skyscanner, Kayak) nem sempre cobrem todas as rotas domésticas brasileiras

### Público-Alvo

- Viajantes frequentes que buscam o **melhor preço**
- Usuários que desejam **monitorar preços** sem visitar sites diariamente
- Desenvolvedores interessados em **arquitetura de scraping e cache**

---

## 2. História do Projeto

O projeto foi originalmente concebido como **RedCode**, um sistema de **compra e reserva de ingressos para eventos** (shows, festivais, teatros). O código ainda reflete esse histórico em alguns nomes de arquivos e namespaces.

### Linha do Tempo

| Fase | Período | Descrição |
|------|---------|-----------|
| **RedCode (Original)** | Início | Sistema de eventos com cadastro de evento, cupom, usuário, reserva, controle de capacidade e limite por CPF. Requisitos definidos em [`requisitos.md`](requisitos.md) (raiz). |
| **Pivot → FlyCompare** | Planejado | Decisão arquitetural documentada no [ADR-001](docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md) e no [PIVOTAGEM.md](docs/pivotagem/PIVOTAGEM.md). Migração de SQL Server → SQLite para desenvolvimento local; adoção de Dapper; implementação de scraping. |
| **FlyCompare (Atual)** | Implementado | Metabuscador funcional com endpoints REST, scraping de 4 fontes, cache em duas camadas, alertas de preço e frontend Blazor WebAssembly. |

### Artefatos do Pivot

- [`docs/pivotagem/PIVOTAGEM.md`](docs/pivotagem/PIVOTAGEM.md) — Plano completo de migração em 6 fases
- [`docs/pivotagem/REQUISITOS-FLYCOMPARE.md`](docs/pivotagem/REQUISITOS-FLYCOMPARE.md) — Histórias de usuário (FC-01 a FC-08) e critérios de aceitação BDD
- [`docs/pivotagem/ROADMAP.md`](docs/pivotagem/ROADMAP.md) — 33 SPECs técnicas (F0 a F6) com instruções detalhadas de implementação
- [`docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md`](docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md) — Architecture Decision Record

---

## 3. Funcionalidades

### 3.1 Busca de Voos com Autocomplete

- Campo de busca com **autocomplete de aeroportos** por nome, cidade ou código IATA
- *Debounce* de 300ms e mínimo de 2 caracteres antes de consultar a API
- Botão de **inversão** origem/destino
- Seletor de data de ida, data de volta (opcional) e quantidade de passageiros
- Sugestão de **rotas populares** carregadas da API

### 3.2 Resultados de Busca com Filtros e Ordenação

- Cards de voo com: companhia (badge), código do voo, horários, duração, escalas, preço, bagagem
- Filtros por companhia aérea (Todas / LATAM / GOL / AZUL)
- Filtros por paradas (Todas / Direto / 1 parada)
- Ordenação por: preço (menor), duração (mais curto), horário de partida, horário de chegada
- Estados de carregamento (spinner) e erro

### 3.3 Alertas de Preço

- Criação de alertas informando: **e-mail**, **rota** (origem/destino), **preço alvo**
- Consulta de alertas ativos por e-mail
- Verificação periódica automática a cada **2 horas**
- Quando o preço cai **abaixo do valor alvo**, o alerta é **automaticamente desativado**
- Tabela de alertas com status (Ativo / Disparado)

### 3.4 Histórico de Preços

- Consulta do histórico de preços de um voo específico (por ID)
- Dados coletados ao longo do tempo pelos scrapings periódicos

### 3.5 Cache Inteligente

- **Cache em duas camadas**: memória (`IMemoryCache`) + Redis (`IDistributedCache`)
- TTL de 30 minutos com sliding expiration de 10 minutos na memória
- Cache warming automático para rotas populares a cada 6 horas
- Chaves no formato `voo:{ORIGEM}:{DESTINO}:{yyyyMMdd}`

### 3.6 Scraping Automatizado

- Coleta de preços de **4 fontes**: LATAM, GOL, Azul e Decolar
- Execução paralela com ordem de prioridade
- Normalização de dados: padronização, deduplicação, remoção de *outliers*
- *Graceful degradation*: se uma fonte falha, as demais continuam
- Dados mock como fallback quando scraping não retorna resultados

---

## 4. Stack de Tecnologias

### Backend

| Tecnologia | Versão | Uso |
|-----------|--------|-----|
| [.NET](https://dotnet.microsoft.com/) 10 (ASP.NET Core) | net10.0 | Runtime e framework web |
| [Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) | — | Definição de endpoints REST |
| [Dapper](https://github.com/DapperLib/Dapper) | 2.1.72 | Micro-ORM para acesso a dados |
| [Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite) | 10.0.0 | Banco de dados SQLite |
| [Hangfire](https://www.hangfire.io/) | 1.8.23 | Agendamento de jobs em background |
| [Hangfire.MemoryStorage](https://www.nuget.org/packages/Hangfire.MemoryStorage) | 1.8.1.1 | Storage in-memory para Hangfire |
| [HtmlAgilityPack](https://html-agility-pack.net/) | 1.12.4 | Parsing de HTML para scraping |
| [PuppeteerSharp](https://www.puppeteersharp.com/) | 24.42.0 | Automação de navegador headless para scraping |
| [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) | 2.8.31 | Cache distribuído (Redis) |

### Frontend

| Tecnologia | Versão | Uso |
|-----------|--------|-----|
| [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) | 10.0.0 | SPA no browser via WebAssembly |
| [Microsoft.AspNetCore.Components.WebAssembly](https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly) | 10.0.0 | SDK Blazor WASM |

### Ferramentas de Desenvolvimento

| Ferramenta | Uso |
|-----------|-----|
| [Node.js](https://nodejs.org/) + [npm scripts](package.json) | Automação de build, dev, setup |
| [Docker](https://www.docker.com/) (Redis image) | Cache distribuído em desenvolvimento |
| [PowerShell](setup-local.ps1) | Setup automatizado local |
| [xUnit](https://xunit.net/) + [Selenium](https://www.selenium.dev/) (WebDriver) | Testes unitários e de integração |

---

## 5. Arquitetura

```
┌──────────────────────────────────────────────────────────────┐
│                   Navegador (Blazor WASM)                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────────┐ │
│  │  Index   │  │ Busca    │  │Resultados│  │Meus Alertas │ │
│  │ (Home)   │  │ Voos     │  │ Busca    │  │             │ │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └──────┬──────┘ │
│       └──────────────┼──────────────┼────────────────┘       │
│                      │     HTTP     │                        │
│              ┌───────┴───────┐──────┘                        │
│              │  HttpClient   │                               │
│              └───────┬───────┘                               │
└──────────────────────┼───────────────────────────────────────┘
                       │ CORS
┌──────────────────────┼───────────────────────────────────────┐
│           ASP.NET Core Minimal API (Kestrel)                 │
│                                                              │
│  ┌────────────┐  ┌──────────────┐  ┌────────────────────┐   │
│  │  Endpoints  │  │  Hangfire    │  │  CORS Middleware   │   │
│  │  REST       │  │  Dashboard   │  │  BlazorPolicy      │   │
│  └──────┬──────┘  └──────┬───────┘  └────────────────────┘   │
│         │                │                                    │
│  ┌──────┴──────────────────┴──────────────────────────────┐  │
│  │                    Services                             │  │
│  │  ┌───────────────┐  ┌────────────────┐                 │  │
│  │  │ CacheService  │  │ ScrapingScheduler                │  │
│  │  │ (2 camadas)   │  │ (Hangfire jobs) │                │  │
│  │  └───────┬───────┘  └───────┬────────┘                 │  │
│  │          │                  │                            │  │
│  │  ┌───────┴──────────────────┴────────────────────┐      │  │
│  │  │           Scrapers (Strategy Pattern)          │      │  │
│  │  │  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────────┐ │      │  │
│  │  │  │ LATAM    │ │ GOL  │ │ Azul │ │ Decolar  │ │      │  │
│  │  │  │(HtmlAgil)│ │(Html)│ │(Html)│ │(Puppeteer)│ │      │  │
│  │  │  └──────────┘ └──────┘ └──────┘ └──────────┘ │      │  │
│  │  │  ┌──────────────────────────────────────────┐ │      │  │
│  │  │  │      NormalizadorDados                   │ │      │  │
│  │  │  │  (Padronizar → Deduplicar → Remover      │ │      │  │
│  │  │  │   Outliers → Sort)                       │ │      │  │
│  │  │  └──────────────────────────────────────────┘ │      │  │
│  │  └───────────────────────────────────────────────┘      │  │
│  │                                                          │
│  │  ┌──────────────────────────────────────────────────┐    │
│  │  │           Data Access (Dapper + SQLite)          │    │
│  │  └──────────────────────────────────────────────────┘    │
│  └──────────────────────────────────────────────────────────┘
│                                                              │
│  ┌──────────────┐  ┌──────────────────────────────────┐      │
│  │   Memória    │  │         Redis (opcional)         │      │
│  │ IMemoryCache │  │       IDistributedCache          │      │
│  └──────────────┘  └──────────────────────────────────┘      │
└──────────────────────────────────────────────────────────────┘
```

### Fluxo de Requisição (Busca de Voos)

```
Usuário → Frontend (Blazor) → GET /api/voos/busca
                                    │
                                    ▼
                             CacheService.Obter()
                                    │
                          ┌─────────┴──────────┐
                          ▼                    ▼
                     Redis hit?          Redis miss?
                          │                    │
                     Retorna cache      MemoryCache hit?
                                          │          │
                                     Retorna         Memory miss?
                                     cache               │
                                                     Execute scrapers
                                                     (paralelo)
                                                          │
                                                     Normalizador
                                                          │
                                                     CacheService.Armazenar()
                                                          │
                                                     Retorna resultado
```

---

## 6. Estrutura do Projeto

```
Red-code-master/
├── .gitignore
├── README.md                          # Documentação principal
├── requisitos.md                      # Requisitos originais RedCode
├── CORRECAO.md                        # Correções
├── setup-local.ps1                    # Script de setup PowerShell
├── package.json                       # Scripts npm (dev, setup, test)
├── teste.http                         # Testes manuais HTTP
├── Red-code-master.sln                # Solução .NET
│
├── docs/
│   ├── requisitos.md                  # Requisitos RedCode (docs)
│   ├── visao.md                       # ← Este arquivo
│   └── pivotagem/
│       ├── PIVOTAGEM.md               # Plano de migração RedCode → FlyCompare
│       ├── REQUISITOS-FLYCOMPARE.md   # Requisitos FlyCompare (histórias + BDD)
│       ├── ROADMAP.md                 # Roadmap técnico com 33 SPECs
│       └── ADR-001-arquitetura-metabuscador-passagens-aereas.md  # ADR
│
├── db/
│   ├── script.sql                     # Schema original RedCode (SQL Server)
│   ├── script-flycompare.sql          # Schema FlyCompare (SQL Server)
│   └── cleanup-legado.sql             # Script de limpeza de tabelas legadas
│
├── scripts/
│   ├── dev-all.mjs                    # Script Node.js para dev completo
│   └── postinstall.mjs                # Pós-instalação npm
│
├── src/
│   ├── RedCodeApi/                # Backend (Minimal API)
│   │   ├── Program.cs                 # Entry point + endpoints + DI
│   │   ├── RedCodeApi.csproj      # Dependências do projeto
│   │   ├── appsettings.json           # Configurações
│   │   ├── appsettings.Development.json
│   │   ├── redcode.db             # Banco SQLite (gerado)
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── Models/FlyCompare/         # Modelos de domínio
│   │   │   ├── Aeroporto.cs
│   │   │   ├── AlertaPreco.cs
│   │   │   ├── CompanhiaAerea.cs
│   │   │   ├── PrecoVoo.cs
│   │   │   ├── Rota.cs
│   │   │   └── Voo.cs
│   │   ├── Dtos/FlyCompare/           # DTOs de entrada/saída
│   │   │   ├── AlertaRequest.cs
│   │   │   ├── BuscaRequest.cs
│   │   │   ├── PrecoHistoricoResponse.cs
│   │   │   └── ResultadoBusca.cs
│   │   └── Services/
│   │       ├── CacheService.cs        # Cache em duas camadas
│   │       ├── ScrapingScheduler.cs   # Jobs Hangfire
│   │       └── Scrapers/              # Implementações Strategy Pattern
│   │           ├── IVooScraper.cs     # Interface do scraper
│   │           ├── NormalizadorDados.cs  # Normalização de resultados
│   │           ├── ScraperLatam.cs    # LATAM (HtmlAgilityPack)
│   │           ├── ScraperGol.cs      # GOL (HtmlAgilityPack)
│   │           ├── ScraperAzul.cs     # Azul (HtmlAgilityPack)
│   │           └── ScraperDecolar.cs  # Decolar (PuppeteerSharp)
│   │
│   ├── RedCodeFront/              # Frontend (Blazor WASM)
│   │   ├── Program.cs                 # Entry point Blazor
│   │   ├── RedCodeFront.csproj    # Dependências
│   │   ├── _Imports.razor             # Global usings
│   │   ├── App.razor                  # Router
│   │   ├── Models/FlyCompare/
│   │   │   ├── Aeroporto.cs           # Modelo frontend
│   │   │   └── ResultadoBusca.cs      # Modelo frontend
│   │   ├── Pages/
│   │   │   ├── Index.razor            # Home page
│   │   │   ├── BuscarVoos.razor       # Busca de voos (/flycompare)
│   │   │   ├── ResultadosBusca.razor  # Resultados (/flycompare/resultados/...)
│   │   │   └── MeusAlertas.razor      # Alertas (/alertas)
│   │   ├── Shared/
│   │   │   ├── MainLayout.razor       # Layout com sidebar
│   │   │   └── Alerta.razor           # Componente de alerta
│   │   └── wwwroot/
│   │       ├── index.html             # HTML host
│   │       ├── appsettings.json       # Config (URL da API)
│   │       └── css/app.css            # Estilos globais
│   │
│   └── (outras pastas omitidas)
│
└── tests/
    ├── RedCodeTests.csproj        # Projeto de testes
    └── bin/Debug/net10.0/             # Binários compilados
```

---

## 7. Modelo de Dados

### DER Conceitual

```
┌──────────────────┐       ┌──────────────┐       ┌──────────────────┐
│  CompanhiasAereas │       │  Aeroportos   │       │     Rotas        │
├──────────────────┤       ├──────────────┤       ├──────────────────┤
│ Id (PK)          │       │ Id (PK)      │       │ Id (PK)          │
│ Codigo (UNIQUE)  │       │ CodigoIATA    │◄──────┤ OrigemId (FK)    │
│ Nome             │       │   (UNIQUE)   │       │ DestinoId (FK)   │
│ SiteBase         │       │ Nome         │       │ UQ(Origem,Dest)  │
│ Ativo            │       │ Cidade       │       └────────┬─────────┘
│ DataCadastro     │       │ Estado       │                │
└────────┬─────────┘       │ Pais         │                │
         │                 │ Latitude     │                │
         │                 │ Longitude    │                │
         │                 └──────────────┘                │
         │                                                │
         ▼                                                ▼
┌──────────────────┐                          ┌──────────────────┐
│      Voos        │                          │     Precos       │
├──────────────────┤                          ├──────────────────┤
│ Id (PK)          │                          │ Id (PK)          │
│ RotaId (FK)      │◄─────────────────────────┤ VooId (FK)       │
│ CompanhiaId (FK) │                          │ Preco            │
│ CodigoVoo        │                          │ Taxas            │
│ DataPartida      │                          │ PrecoTotal       │
│ DataChegada      │                          │ Moeda (BRL)      │
│ DuracaoMinutos   │                          │ TipoTarifa       │
│ Paradas          │                          │ BagagemIncluida  │
│ AeroportoEscalaId│                          │ FranquiaBagagemKg│
│ Classe           │                          │ UrlDestino       │
└──────────────────┘                          │ DataColeta       │
                                              └──────────────────┘

┌──────────────────┐
│  AlertasPreco    │
├──────────────────┤
│ Id (PK)          │
│ Email            │
│ OrigemIATA       │
│ DestinoIATA      │
│ PrecoAlvo        │
│ Status           │
│ DataCriacao      │
└──────────────────┘
```

### Tabelas (6 no total)

| # | Tabela | Finalidade |
|---|--------|------------|
| 1 | `CompanhiasAereas` | Cadastro de companhias (LATAM, GOL, Azul) |
| 2 | `Aeroportos` | Aeroportos com código IATA, cidade, estado, coordenadas |
| 3 | `Rotas` | Relação origem-destino entre aeroportos |
| 4 | `Voos` | Resultados de scraping (código, horários, duração, escalas) |
| 5 | `Precos` | Histórico de preços (preço, taxas, total, bagagem, URL) |
| 6 | `AlertasPreco` | Alertas de preço criados pelos usuários |

### Seed Data

- **3 companhias**: LATAM (código `LATAM`), GOL (`GOL`), Azul (`AZUL`)
- **15 aeroportos**: GRU, CGH, GIG, SDU, BSB, CNF, POA, REC, SSA, FOR, MAO, BEL, CWB, FLN, VCP
- **Rotas populares**: GRU→GIG, GRU→BSB, GRU→REC, GRU→POA, GRU→FOR, GRU→SSA, GRU→CNF, GRU→CWB, GRU→FLN, GRU→MAO, GIG→GRU, GIG→SSA

---

## 8. Endpoints da API

| Método | Rota | Descrição | Documentação no Código |
|--------|------|-----------|------------------------|
| `GET` | `/api/aeroportos` | Lista todos os aeroportos | [`Program.cs:270-274`](src/RedCodeApi/Program.cs#270) |
| `GET` | `/api/aeroportos/busca?q=` | Autocomplete de aeroportos (busca por nome/cidade/IATA) | [`Program.cs:277-290`](src/RedCodeApi/Program.cs#277) |
| `GET` | `/api/companhias` | Lista todas as companhias aéreas | [`Program.cs:294-297`](src/RedCodeApi/Program.cs#294) |
| `GET` | `/api/rotas/populares` | Lista rotas populares para sugestão na busca | [`Program.cs:301-304`](src/RedCodeApi/Program.cs#301) |
| `GET` | `/api/voos/busca?origem=&destino=&dataPartida=` | Busca principal de voos (cache → scrapers → mock fallback) | [`Program.cs:307-381`](src/RedCodeApi/Program.cs#307) |
| `GET` | `/api/voos/precos/{vooId}` | Histórico de preços de um voo específico | [`Program.cs:385-397`](src/RedCodeApi/Program.cs#385) |
| `POST` | `/api/alertas` | Cria um novo alerta de preço | [`Program.cs:401-421`](src/RedCodeApi/Program.cs#401) |
| `GET` | `/api/alertas/{email}` | Lista alertas de preço de um e-mail | [`Program.cs:425-442`](src/RedCodeApi/Program.cs#425) |

### Parâmetros da Rota de Busca

```
GET /api/voos/busca?origem=GRU&destino=REC&dataPartida=2025-06-15
```

**Fluxo interno**:
1. Verifica cache (Redis → memória)
2. Se cache miss: executa scrapers em paralelo (LATAM → GOL → Azul → Decolar)
3. Normaliza resultados (padroniza → deduplica → outliers → ordena)
4. Armazena no cache e persiste no banco
5. Se scraping retorna vazio: gera dados mock como fallback

---

## 9. Frontend — Páginas e Rotas

| Rota | Página | Funcionalidade |
|------|--------|----------------|
| `/` (Index) | [`Index.razor`](src/RedCodeFront/Pages/Index.razor) | Home com hero branding, 3 cards de navegação, 4 cards de features |
| `/flycompare` | [`BuscarVoos.razor`](src/RedCodeFront/Pages/BuscarVoos.razor) | Formulário de busca com autocomplete, datas, passageiros, rotas populares |
| `/flycompare/resultados/{origem}/{destino}/{dataPartida}` | [`ResultadosBusca.razor`](src/RedCodeFront/Pages/ResultadosBusca.razor) | Resultados com filtros (companhia, paradas) e ordenação (preço, duração, horário) |
| `/alertas` | [`MeusAlertas.razor`](src/RedCodeFront/Pages/MeusAlertas.razor) | Criar e consultar alertas de preço |

### Layout

- [`MainLayout.razor`](src/RedCodeFront/Shared/MainLayout.razor): Sidebar fixa com logo, navegação e indicador de status da API
- [`Alerta.razor`](src/RedCodeFront/Shared/Alerta.razor): Componente de alerta reutilizável

### Aspectos de UX

- **Autocomplete** com debounce de 300ms e mínimo de 2 caracteres
- **Botão swap** para inverter origem/destino
- **Seletor de passageiros**: 1–9 adultos
- **Filtros** por companhia (Todas / LATAM / GOL / AZUL) e paradas (Todas / Direto / 1 parada)
- **Ordenação**: preço, duração, partida, chegada
- **Estados**: carregamento (spinner), erro (mensagem), vazio

---

## 10. Estratégia de Scraping

### Interface (Strategy Pattern)

Definida em [`IVooScraper.cs`](src/RedCodeApi/Services/Scrapers/IVooScraper.cs):

```csharp
public interface IVooScraper
{
    string Nome { get; }           // Identificador do scraper
    int Ordem { get; }             // Prioridade de execução
    Task<List<ResultadoBusca>> BuscarVoosAsync(
        string origem,
        string destino,
        DateTime dataPartida,
        CancellationToken cancellationToken = default);
}
```

### Implementações

| Scraper | Classe | Técnica | Ordem | Linhas |
|---------|--------|---------|-------|--------|
| **LATAM** | [`ScraperLatam.cs`](src/RedCodeApi/Services/Scrapers/ScraperLatam.cs) | HttpClient + HtmlAgilityPack | 1º | 266 |
| **GOL** | [`ScraperGol.cs`](src/RedCodeApi/Services/Scrapers/ScraperGol.cs) | HttpClient + HtmlAgilityPack | 2º | 263 |
| **Azul** | [`ScraperAzul.cs`](src/RedCodeApi/Services/Scrapers/ScraperAzul.cs) | HttpClient + HtmlAgilityPack | 3º | 257 |
| **Decolar** | [`ScraperDecolar.cs`](src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs) | PuppeteerSharp (headless browser) | 4º | 234 |

### Técnicas de Scraping

- **HtmlAgilityPack** (LATAM, GOL, Azul): sites com HTML estático; parse via XPath/expressões de seleção
- **PuppeteerSharp** (Decolar): sites com JavaScript pesado (SPA); navegador headless Chromium com User-Agent personalizado
- **Browser compartilhado** (Decolar): instância única `IBrowser` reutilizada entre chamadas com `SemaphoreSlim` para controle de concorrência

### Resiliência

- Cada scraper tem seu próprio `try/catch` — falha em um não afeta os demais (*graceful degradation*)
- Timeout de 30 segundos para Puppeteer
- Logs estruturados com `ILogger<T>` em todos os scrapers
- Dados mock gerados caso todos os scrapers falhem ou retornem vazio

### Normalização

Processo em 4 etapas em [`NormalizadorDados.cs`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs):

1. **PadronizarCampos**: Normaliza nomes de companhias (LATAM → LATAM Airlines, GOL → GOL Linhas Aéreas), formata tipos de tarifa, valida códigos IATA
2. **Deduplicar**: Remove voos duplicados (mesmo código), mantendo o mais barato
3. **RemoverOutliers**: Remove preços extremos usando método de **3 desvios padrão**
4. **Sort**: Ordena resultados por preço (menor → maior)

---

## 11. Sistema de Cache

Implementado em [`CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) — 243 linhas.

### Arquitetura de Duas Camadas

```
┌───────────────────────────────────────┐
│           IDistributedCache           │  ← Redis (primário)
│   TTL: 30 minutos                     │
├───────────────────────────────────────┤
│           IMemoryCache                │  ← Memória local (fallback)
│   Sliding Expiration: 10 minutos      │
└───────────────────────────────────────┘
```

### Estratégia de Leitura

1. Tenta Redis (`IDistributedCache`). Se hit → retorna.
2. Se Redis miss (excception ou null) → tenta memória (`IMemoryCache`). Se hit → retorna.
3. Se ambos miss → busca dos scrapers.

### Estratégia de Escrita

1. Salva no Redis com TTL de 30 minutos
2. Salva na memória com sliding expiration de 10 minutos

### Chaves

```
voo:{ORIGEM}:{DESTINO}:{yyyyMMdd}
Exemplo: voo:GRU:REC:20250615
```

### Cache Warming

O job `AtualizarRotasPopulares()` (Hangfire, a cada 6 horas) pré-carrega o cache para 12 rotas populares, garantindo que as primeiras buscas dos usuários sejam rápidas.

---

## 12. Jobs Agendados (Hangfire)

Configurado em [`Program.cs:198-220`](src/RedCodeApi/Program.cs#198) e implementado em [`ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs).

### Job 1: `AtualizarRotasPopulares`

- **Frequência**: A cada 6 horas (cron: `0 */6 * * *`)
- **Função**: Executa scraping para todas as 12 rotas populares e armazena no cache
- **Propósito**: Cache warming para que buscas de usuários comuns sejam servidas do cache

### Job 2: `VerificarAlertas`

- **Frequência**: A cada 2 horas (cron: `0 */2 * * *`)
- **Função**: Para cada alerta ativo, verifica se há voos no banco com preço menor que o alvo
- **Ação**: Se preço ≤ preço alvo, marca o alerta como "Disparado" (`Status` = 1)

### Dashboard Hangfire

Acessível em `http://localhost:5246/hangfire` durante o desenvolvimento.

---

## 13. Padrões de Projeto

| Padrão | Onde | Descrição |
|--------|------|-----------|
| **Strategy** | [`IVooScraper`](src/RedCodeApi/Services/Scrapers/IVooScraper.cs) + 4 implementações | Algoritmos de scraping intercambiáveis por companhia |
| **Singleton** | [`ScraperDecolar.cs:13-14`](src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs#13) | Instância única do browser Puppeteer compartilhada entre chamadas |
| **Facade** | [`ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) | Abstrai a orquestração de scrapers, cache e banco |
| **Repository (implícito)** | [`Program.cs`](src/RedCodeApi/Program.cs) (SQL direto com Dapper) | Acesso a dados encapsulado nas queries SQL do Program.cs |
| **Proxy/Cache-Aside** | [`CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) | Duas camadas de cache com fallback automático |
| **Pipeline** | [`NormalizadorDados.cs`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs) | Sequência de transformações (Padronizar → Deduplicar → Outliers → Sort) |
| **Minimal API** | [`Program.cs`](src/RedCodeApi/Program.cs) | Definição de endpoints sem controllers |
| **Injeção de Dependência** | Todo o backend | DI nativa do .NET para serviços, HttpClients, cache |

---

## 14. Considerações de Segurança e Resiliência

### Segurança

- **CORS**: Política "BlazorPolicy" configurada para permitir qualquer origem (desenvolvimento)
- **SQL Injection**: Uso de Dapper com parâmetros nomeados (`new { origem, destino, ... }`) — proteção nativa contra injeção
- **Validação de IATA**: [`NormalizadorDados.cs:160-164`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs#160) valida código de 3 letras via regex `^[A-Z]{3}$`
- **Sem autenticação**: Sistema atual não implementa login/usuário (planejado para versões futuras)

### Resiliência

- **Graceful degradation**: Falha de um scraper não impede os demais
- **Cache como proteção**: Cache reduz dependência de scrapers externos
- **Outlier removal**: Preços extremos são removidos estatisticamente (3σ)
- **Fallback mock**: Dados gerados artificialmente quando scraping falha completamente
- **Timeout Puppeteer**: 30 segundos para navegação headless
- **Logs estruturados**: Todos os componentes registram operações com `ILogger<T>`

### Limitações Conhecidas

- Scrapers dependem da estrutura HTML dos sites parceiros — mudanças no layout podem quebrar o parsing
- PuppeteerSharp requer download do Chromium na primeira execução
- Redis é opcional — sem Redis o sistema funciona apenas com cache em memória
- Sem escalabilidade horizontal implementada (Redis + session affinity seriam necessários)

---

## 15. Roadmap

O projeto segue um roadmap de 6 fases (F0–F6) detalhado em [`docs/pivotagem/ROADMAP.md`](docs/pivotagem/ROADMAP.md) com 33 SPECs técnicas.

| Fase | Foco | SPECs | Status |
|------|------|-------|--------|
| **F0** | Setup inicial, estrutura de pastas, configuração do projeto | 1–5 | ✅ |
| **F1** | Modelos de domínio, DTOs, banco de dados | 6–10 | ✅ |
| **F2** | Endpoints da API e frontend básico | 11–17 | ✅ |
| **F3** | Scrapers, normalização, cache | 18–24 | ✅ |
| **F4** | Alertas, Hangfire, testes | 25–30 | ✅ |
| **F5** | Melhorias, refatoração, deploy | 31–33 | ⬜ |

---

## Apêndice: Referências

| Documento | Descrição |
|-----------|-----------|
| [`README.md`](README.md) | Instruções de setup, stack, endpoints |
| [`./docs/pivotagem/PIVOTAGEM.md`](docs/pivotagem/PIVOTAGEM.md) | Plano completo de migração RedCode → FlyCompare |
| [`./docs/pivotagem/REQUISITOS-FLYCOMPARE.md`](docs/pivotagem/REQUISITOS-FLYCOMPARE.md) | Requisitos FlyCompare (FC-01 a FC-08) |
| [`./docs/pivotagem/ROADMAP.md`](docs/pivotagem/ROADMAP.md) | Roadmap técnico com 33 SPECs |
| [`./docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md`](docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md) | Architecture Decision Record |
| [`db/script-flycompare.sql`](db/script-flycompare.sql) | Script de banco SQL Server (legado) |
| [`package.json`](package.json) | Scripts npm de automação |
| [`setup-local.ps1`](setup-local.ps1) | Script de setup automático |

---

> **Documento gerado em:** 21 de maio de 2026  
> **Versão do projeto:** FlyCompare (pós-pivot RedCode)  
> **Stack principal:** .NET 10 Minimal API · Blazor WebAssembly · SQLite · Dapper · Hangfire · HtmlAgilityPack · PuppeteerSharp
