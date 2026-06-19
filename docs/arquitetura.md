# FlyCompare — Arquitetura do Sistema

> **Metabuscador de Passagens Aéreas** | .NET 10 + Blazor WebAssembly + SQLite
>
> **Documento de Arquitetura** — Baseado na [visão do sistema](visao.md) e no [ADR-001](pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md)

---

## Índice

1. [Introdução](#1-introdução)
2. [Visão Geral da Arquitetura](#2-visão-geral-da-arquitetura)
3. [Diagrama de Contexto (C4 — Nível 1)](#3-diagrama-de-contexto-c4--nível-1)
4. [Diagrama de Containers (C4 — Nível 2)](#4-diagrama-de-containers-c4--nível-2)
5. [Diagrama de Componentes (C4 — Nível 3)](#5-diagrama-de-componentes-c4--nível-3)
6. [Estratégia de Scraping](#6-estratégia-de-scraping)
7. [Sistema de Cache em Duas Camadas](#7-sistema-de-cache-em-duas-camadas)
8. [Jobs Agendados (Hangfire)](#8-jobs-agendados-hangfire)
9. [Modelo de Dados e Persistência](#9-modelo-de-dados-e-persistência)
10. [Fluxos de Dados Críticos](#10-fluxos-de-dados-críticos)
11. [Padrões de Projeto](#11-padrões-de-projeto)
12. [Segurança e Resiliência](#12-segurança-e-resiliência)
13. [Arquitetura de Deploy](#13-arquitetura-de-deploy)
14. [Decisões Arquiteturais (ADRs)](#14-decisões-arquiteturais-adrs)
15. [Evolução e Roadmap Técnico](#15-evolução-e-roadmap-técnico)

---

## 1. Introdução

### 1.1 Propósito

Este documento descreve a **arquitetura de software** do FlyCompare, um metabuscador de passagens aéreas que coleta, normaliza e apresenta preços de voos de múltiplas fontes (LATAM, GOL, Azul, Decolar) em uma interface unificada.

### 1.2 Escopo

Abrange desde a arquitetura de alto nível (containers, componentes) até detalhes de implementação (padrões, fluxos de dados, estratégias de cache e scraping). É o documento de referência para desenvolvedores e arquitetos que precisam entender, modificar ou dar continuidade ao sistema.

### 1.3 Público-Alvo

- Desenvolvedores que irão manter ou evoluir o sistema
- Arquitetos avaliando decisões técnicas
- Novos membros da equipe em processo de onboarding

### 1.4 Convenções e Notação

- **Diagramas**: Notação C4 (Contexto → Containers → Componentes → Código)
- **Código**: Referências diretas a arquivos e linhas no formato [`arquivo.cs:N`](src/RedCodeApi/Program.cs)
- **Decisões**: Architecture Decision Records (ADRs) no diretório [`docs/pivotagem/`](docs/pivotagem/)

---

## 2. Visão Geral da Arquitetura

O FlyCompare adota uma **arquitetura monolítica modular** com separação clara de responsabilidades:

```
┌──────────────────────────────────────────────────────────────────┐
│                     Navegador (Cliente)                          │
│                Blazor WebAssembly (SPA)                          │
└────────────────────────────┬─────────────────────────────────────┘
                             │ HTTPS / CORS
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Minimal API                       │
│                                                                  │
│  ┌──────────────┐  ┌────────────────┐  ┌───────────────────┐   │
│  │  Endpoints    │  │   Hangfire     │  │  Middleware       │   │
│  │  REST         │  │   Dashboard    │  │  (CORS, Logging)  │   │
│  └──────┬───────┘  └───────┬────────┘  └───────────────────┘   │
│         │                  │                                     │
│  ┌──────┴──────────────────┴────────────────────────────────┐   │
│  │                    Services Layer                         │   │
│  │  ┌─────────────────┐  ┌─────────────────────────────┐    │   │
│  │  │  CacheService   │  │   ScrapingScheduler         │    │   │
│  │  │  (2 camadas)    │  │   (Hangfire Jobs)           │    │   │
│  │  └────────┬────────┘  └───────────────┬─────────────┘    │   │
│  │           │                           │                   │   │
│  │  ┌────────┴───────────────────────────┴─────────────┐    │   │
│  │  │           Scrapers (Strategy Pattern)             │    │   │
│  │  │  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────────┐    │    │   │
│  │  │  │ LATAM    │ │ GOL  │ │ Azul │ │ Decolar  │    │    │   │
│  │  │  └──────────┘ └──────┘ └──────┘ └──────────┘    │    │   │
│  │  │  ┌──────────────────────────────────────────┐    │    │   │
│  │  │  │      NormalizadorDados                   │    │    │   │
│  │  │  └──────────────────────────────────────────┘    │    │   │
│  │  └──────────────────────────────────────────────────┘    │   │
│  │                                                           │   │
│  │  ┌──────────────────────────────────────────────────┐    │   │
│  │  │      Data Access (Dapper + SQL/ SQLite)          │    │   │
│  │  └──────────────────────────────────────────────────┘    │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                  │
│  ┌──────────────────────┐  ┌────────────────────────────┐       │
│  │    IMemoryCache       │  │   Redis (IDistributedCache) │       │
│  │    (L1 - Local)       │  │   (L2 - Distribuído)       │       │
│  └──────────────────────┘  └────────────────────────────┘       │
└──────────────────────────────────────────────────────────────────┘
```

### 2.1 Princípios Arquiteturais

| Princípio | Descrição |
|-----------|-----------|
| **Separação de Responsabilidades** | Cada camada tem responsabilidades bem definidas: endpoints (apresentação), serviços (negócio), scrapers (coleta), dados (persistência) |
| **Programação para Interfaces** | Scrapers implementam [`IVooScraper`](src/RedCodeApi/Services/Scrapers/IVooScraper.cs), permitindo adicionar novas fontes sem modificar código existente |
| **Graceful Degradation** | Falha de um scraper não afeta os demais; cache protege contra fontes indisponíveis; dados mock servem como fallback |
| **Cache-Aside** | Dados são buscados do cache primeiro; apenas em cache miss os scrapers são executados |
| **Pipeline de Transformação** | Dados brutos dos scrapers passam por um pipeline de normalização (padronizar → deduplicar → remover outliers → ordenar) |
| **Assincronicidade Seletiva** | Scraping síncrono na request (com cache); jobs de fundo (Hangfire) para cache warming e verificação de alertas |

---

## 3. Diagrama de Contexto (C4 — Nível 1)

```
┌──────────────────────────────────────────────────────────────────┐
│                    SISTEMA FLYCOMPARE                             │
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Usuário    │  │   Hangfire   │  │   Redis      │          │
│  │  (Navegador) │  │  (Dashboard) │  │  (Cache)     │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                  │                   │
│         ▼                 ▼                  ▼                   │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              FlyCompare API + Frontend                      ││
│  │           (ASP.NET Core + Blazor WASM)                      ││
│  └────────────┬───────────────────────────────────────────────┘│
│               │                                                │
│               ▼                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    Fontes Externas                        │  │
│  │  ┌────────┐  ┌────────┐  ┌────────┐  ┌──────────────┐   │  │
│  │  │ LATAM  │  │  GOL   │  │  Azul  │  │   Decolar    │   │  │
│  │  │ (HTTP) │  │ (HTTP) │  │ (HTTP) │  │ (JavaScript) │   │  │
│  │  └────────┘  └────────┘  └────────┘  └──────────────┘   │  │
│  └──────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

### Interações com Sistemas Externos

| Sistema | Tipo | Protocolo | Dependência |
|---------|------|-----------|-------------|
| **LATAM** | Site de busca | HTTPS (HTML) | HtmlAgilityPack + HttpClient |
| **GOL** | Site de busca | HTTPS (HTML) | HtmlAgilityPack + HttpClient |
| **Azul** | Site de busca | HTTPS (HTML) | HtmlAgilityPack + HttpClient |
| **Decolar** | OTA (SPA) | HTTPS (JS) | PuppeteerSharp (headless Chromium) |
| **Redis** | Cache distribuído | TCP (RESP) | StackExchange.Redis — **opcional** |

---

## 4. Diagrama de Containers (C4 — Nível 2)

```
┌─────────────────────────────────────────────────────────────┐
│                    Navegador (Cliente)                       │
│  ┌────────────────────────────────────────────────────┐    │
│  │           Blazor WebAssembly App                   │    │
│  │  ┌──────────┐  ┌──────────┐  ┌─────────────────┐ │    │
│  │  │  Index   │  │  Busca   │  │  Meus Alertas   │ │    │
│  │  │  (Home)  │  │  Voos    │  │                 │ │    │
│  │  └──────────┘  └──────────┘  └─────────────────┘ │    │
│  │  ┌──────────────────────────────────────────────┐ │    │
│  │  │         HttpClient (API Calls)               │ │    │
│  │  └──────────────────────────────────────────────┘ │    │
│  └────────────────────────────────────────────────────┘    │
└─────────────────────────┬───────────────────────────────────┘
                          │ HTTP /api/*
                          │ CORS: BlazorPolicy (*)
                          ▼
┌────────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Minimal API (Kestrel)              │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │  Middleware Pipeline                                     │     │
│  │  ┌──────────┐  ┌───────────┐  ┌────────────┐           │     │
│  │  │ Logging  │  │   CORS    │  │ Hangfire   │           │     │
│  │  │          │  │ BlazorPol │  │ Dashboard  │           │     │
│  │  └──────────┘  └───────────┘  └────────────┘           │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │  Endpoints REST (Minimal API)                            │     │
│  │  ┌───────────────┐ ┌────────────────┐ ┌───────────────┐ │     │
│  │  │ GET /api/     │ │ GET /api/      │ │ POST /api/    │ │     │
│  │  │ aeroportos    │ │ voos/busca     │ │ alertas       │ │     │
│  │  ├───────────────┤ ├────────────────┤ ├───────────────┤ │     │
│  │  │ GET /api/     │ │ GET /api/voos/ │ │ GET /api/     │ │     │
│  │  │ companhias   │ │ precos/{id}   │ │ alertas/{email}│ │     │
│  │  ├───────────────┤ └────────────────┘ └───────────────┘ │     │
│  │  │ GET /api/     │                                       │     │
│  │  │ rotas/popular │                                       │     │
│  │  └───────────────┘                                       │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │  Services                                                │     │
│  │  ┌────────────────┐ ┌────────────────┐ ┌──────────────┐ │     │
│  │  │ CacheService   │ │ScrapingSched. │ │Normalizador  │ │     │
│  │  └────────────────┘ └────────────────┘ └──────────────┘ │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │  Scrapers (Strategy Pattern - IVooScraper)               │     │
│  │  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────────┐           │     │
│  │  │ LATAM    │ │ GOL  │ │ Azul │ │ Decolar  │           │     │
│  │  │HtmlAgil. │ │Html  │ │Html  │ │Puppeteer │           │     │
│  │  └──────────┘ └──────┘ └──────┘ └──────────┘           │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌──────────────────────────────────────────────────────────┐     │
│  │  Data Access (Dapper)                                    │     │
│  │  ┌──────────────────────────────────────────────────┐   │     │
│  │  │  SQLite (via Microsoft.Data.Sqlite)               │   │     │
│  │  │  Tabelas: CompanhiasAereas, Aeroportos, Rotas,   │   │     │
│  │  │  Voos, Precos, AlertasPreco                       │   │     │
│  │  └──────────────────────────────────────────────────┘   │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                    │
│  ┌────────────────┐  ┌────────────────────────────────────┐       │
│  │  IMemoryCache   │  │  IDistributedCache (Redis)        │       │
│  │  (L1 - Local)   │  │  (L2 - Opcional)                  │       │
│  └────────────────┘  └────────────────────────────────────┘       │
└────────────────────────────────────────────────────────────────────┘
```

### 4.1 Container: Frontend (Blazor WebAssembly)

| Característica | Valor |
|---------------|-------|
| **Tecnologia** | Blazor WebAssembly (.NET 10) |
| **Responsabilidade** | Interface do usuário, SPA executada no navegador |
| **Comunicação** | HTTP via `HttpClient` — consome a API REST |
| **Páginas** | [`Index.razor`](src/RedCodeFront/Pages/Index.razor) (Home), [`BuscarVoos.razor`](src/RedCodeFront/Pages/BuscarVoos.razor), [`ResultadosBusca.razor`](src/RedCodeFront/Pages/ResultadosBusca.razor), [`MeusAlertas.razor`](src/RedCodeFront/Pages/MeusAlertas.razor) |
| **Models** | [`Aeroporto.cs`](src/RedCodeFront/Models/FlyCompare/Aeroporto.cs), [`ResultadoBusca.cs`](src/RedCodeFront/Models/FlyCompare/ResultadoBusca.cs) |

### 4.2 Container: API (ASP.NET Core Minimal API)

| Característica | Valor |
|---------------|-------|
| **Tecnologia** | .NET 10 — ASP.NET Core Minimal API (Kestrel) |
| **Responsabilidade** | Orquestrar scraping, cache, persistência; expor endpoints REST |
| **Entry Point** | [`Program.cs`](src/RedCodeApi/Program.cs) — ~450 linhas com endpoints, DI, middleware |
| **Endpoints** | 8 endpoints REST (consultas + alertas) |
| **Porta padrão** | `5246` (desenvolvimento) |

### 4.3 Container: Banco de Dados (SQLite)

| Característica | Valor |
|---------------|-------|
| **Tecnologia** | SQLite via `Microsoft.Data.Sqlite` |
| **Arquivo** | [`redcode.db`](src/RedCodeApi/redcode.db) (gerado automaticamente) |
| **ORM** | Dapper (micro-ORM, queries SQL manuais parametrizadas) |
| **Tabelas** | 6: `CompanhiasAereas`, `Aeroportos`, `Rotas`, `Voos`, `Precos`, `AlertasPreco` |

### 4.4 Container: Cache (Redis — Opcional)

| Característica | Valor |
|---------------|-------|
| **Tecnologia** | Redis via `StackExchange.Redis` (`IDistributedCache`) |
| **Responsabilidade** | Cache distribuído compartilhado entre instâncias |
| **Disponibilidade** | **Opcional** — sem Redis, o sistema opera apenas com `IMemoryCache` |
| **Setup dev** | Docker: `docker run -p 6379:6379 redis` |

---

## 5. Diagrama de Componentes (C4 — Nível 3)

### 5.1 Componentes do Backend

```
┌─────────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core Minimal API                       │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Endpoints                                                  │   │
│  │                                                             │   │
│  │  ┌────────────────┐ ┌────────────────┐ ┌────────────────┐   │   │
│  │  │ AeroportosEP   │ │  VoosEP        │ │  AlertasEP     │   │   │
│  │  │                │ │                │ │                │   │   │
│  │  │ GET /api/      │ │ GET /api/voos/ │ │ POST /api/     │   │   │
│  │  │ aeroportos     │ │ busca          │ │ alertas        │   │   │
│  │  │                │ │                │ │                │   │   │
│  │  │ GET /api/      │ │ GET /api/voos/ │ │ GET /api/      │   │   │
│  │  │ aeroportos/    │ │ precos/{vooId} │ │ alertas/{email}│   │   │
│  │  │ busca          │ │                │ │                │   │   │
│  │  └────────────────┘ └────────────────┘ └────────────────┘   │   │
│  │  ┌────────────────┐ ┌────────────────┐                      │   │
│  │  │ CompanhiasEP   │ │  RotasEP       │                      │   │
│  │  │                │ │                │                      │   │
│  │  │ GET /api/      │ │ GET /api/rotas/│                      │   │
│  │  │ companhias     │ │ populares      │                      │   │
│  │  └────────────────┘ └────────────────┘                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Services                                                   │   │
│  │                                                             │   │
│  │  ┌──────────────────────┐  ┌────────────────────────────┐   │   │
│  │  │  CacheService        │  │  ScrapingScheduler         │   │   │
│  │  │  ───────────────     │  │  ─────────────────         │   │   │
│  │  │  + Obter(chave)      │  │  + AtualizarRotasPopulares │   │   │
│  │  │  + Armazenar(chave)  │  │  + VerificarAlertas()      │   │   │
│  │  │  + Remover(chave)    │  │                            │   │   │
│  │  │  + GerarChave(orig,  │  │  (Hangfire jobs)           │   │   │
│  │  │    dest, data)       │  │                            │   │   │
│  │  └──────────────────────┘  └────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Scrapers (Strategy Pattern)                                │   │
│  │                                                             │   │
│  │  ┌────────────────────────────────────────────────────┐     │   │
│  │  │  <<interface>>                                     │     │   │
│  │  │  IVooScraper                                       │     │   │
│  │  │  ───────────────────────                            │     │   │
│  │  │  + Fonte: string                                   │     │   │
│  │  │  + BuscarVoosAsync(origem, destino, dataPartida,   │     │   │
│  │  │    cancellationToken): Task<List<ResultadoBusca>>  │     │   │
│  │  └────────────────────────────────────────────────────┘     │   │
│  │         ▲              ▲              ▲              ▲      │   │
│  │         │              │              │              │      │   │
│  │  ┌──────┴──────┐ ┌────┴────┐ ┌────┴────┐ ┌──────┴──────┐  │   │
│  │  │ ScraperLatam│ │Scraper  │ │Scraper  │ │Scraper     │  │   │
│  │  │             │ │Gol      │ │Azul     │ │Decolar     │  │   │
│  │  │ HttpClient  │ │Http     │ │Http     │ │Puppeteer   │  │   │
│  │  │ HtmlAgility │ │Client   │ │Client   │ │Sharp       │  │   │
│  │  │ Pack        │ │+ Html   │ │+ Html   │ │(Headless   │  │   │
│  │  │             │ │Agility  │ │Agility  │ │ Browser)   │  │   │
│  │  │ Fonte:      │ │Pack     │ │Pack     │ │            │  │   │
│  │  │ "Latam"     │ │         │ │         │ │ Fonte:     │  │   │
│  │  │ Ordem: 1    │ │Fonte:   │ │Fonte:   │ │ "Decolar"  │  │   │
│  │  │             │ │"Gol"    │ │"Azul"   │ │ Ordem: 4   │  │   │
│  │  │             │ │Ordem: 2 │ │Ordem: 3 │ │            │  │   │
│  │  └─────────────┘ └────────┘ └────────┘ └──────────────┘  │   │
│  │                                                             │   │
│  │  ┌──────────────────────────────────────────────────┐      │   │
│  │  │  NormalizadorDados                                │      │   │
│  │  │  ───────────────────────                           │      │   │
│  │  │  + Normalizar(resultados): List<ResultadoBusca>   │      │   │
│  │  │    1. PadronizarCampos                            │      │   │
│  │  │    2. Deduplicar (mesmo código + companhia)       │      │   │
│  │  │    3. RemoverOutliers (3 desvios padrão)          │      │   │
│  │  │    4. Sort (por preço crescente)                  │      │   │
│  │  │  + ValidarCodigoIATA(codigo): bool                │      │   │
│  │  └──────────────────────────────────────────────────┘      │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  Data Access (Dapper)                                       │   │
│  │                                                             │   │
│  │  ┌────────────────────────────────────────────────────┐     │   │
│  │  │  SQLite Database (redcode.db)                   │     │   │
│  │  │                                                    │     │   │
│  │  │  Tabelas:                                          │     │   │
│  │  │  ┌──────────────┐  ┌─────────────┐                 │     │   │
│  │  │  │Companhias    │  │ Aeroportos  │                 │     │   │
│  │  │  │Aereas        │  │             │                 │     │   │
│  │  │  ├──────────────┤  ├─────────────┤                 │     │   │
│  │  │  │Id (PK)       │  │Id (PK)      │                 │     │   │
│  │  │  │Codigo (UQ)   │  │CodigoIATA   │                 │     │   │
│  │  │  │Nome          │  │(UQ)         │                 │     │   │
│  │  │  │SiteBase      │  │Nome         │                 │     │   │
│  │  │  │Ativo         │  │Cidade       │                 │     │   │
│  │  │  │DataCadastro  │  │Estado       │                 │     │   │
│  │  │  └──────────────┘  │Pais         │                 │     │   │
│  │  │                     │Latitude     │                 │     │   │
│  │  │                     │Longitude    │                 │     │   │
│  │  │                     └──────┬──────┘                 │     │   │
│  │  │                            │                        │     │   │
│  │  │  ┌─────────────────────────┼────────────────────┐   │     │   │
│  │  │  │              ┌──────────┴──────────┐         │   │     │   │
│  │  │  │              │      Rotas          │         │   │     │   │
│  │  │  │              ├─────────────────────┤         │   │     │   │
│  │  │  │              │ Id (PK)             │         │   │     │   │
│  │  │  │              │ OrigemId (FK)       │◄────────┤   │     │   │
│  │  │  │              │ DestinoId (FK)      │         │   │     │   │
│  │  │  │              └─────────┬───────────┘         │   │     │   │
│  │  │  │                        │                     │   │     │   │
│  │  │  │              ┌─────────┴───────────┐         │   │     │   │
│  │  │  │              │       Voos          │         │   │     │   │
│  │  │  │              ├─────────────────────┤         │   │     │   │
│  │  │  │         ┌───►│ Id (PK)             │         │   │     │   │
│  │  │  │         │    │ RotaId (FK)         │         │   │     │   │
│  │  │  │         │    │ CompanhiaId (FK)    │◄────────┤   │     │   │
│  │  │  │         │    │ CodigoVoo           │         │   │     │   │
│  │  │  │         │    │ DataPartida         │         │   │     │   │
│  │  │  │         │    │ DataChegada         │         │   │     │   │
│  │  │  │         │    │ DuracaoMinutos      │         │   │     │   │
│  │  │  │         │    │ Paradas             │         │   │     │   │
│  │  │  │         │    │ AeroportoEscalaId   │         │   │     │   │
│  │  │  │         │    │ Classe              │         │   │     │   │
│  │  │  │         │    └────────┬────────────┘         │   │     │   │
│  │  │  │         │             │                      │   │     │   │
│  │  │  │         │    ┌────────┴────────────┐         │   │     │   │
│  │  │  │         └────┤      Precos         │         │   │     │   │
│  │  │  │              ├─────────────────────┤         │   │     │   │
│  │  │  │              │ Id (PK)             │         │   │     │   │
│  │  │  │              │ VooId (FK)          │         │   │     │   │
│  │  │  │              │ PrecoTotal          │         │   │     │   │
│  │  │  │              │ Moeda (BRL)         │         │   │     │   │
│  │  │  │              │ DataColeta          │         │   │     │   │
│  │  │  │              │ Fonte               │         │   │     │   │
│  │  │  │              └─────────────────────┘         │   │     │   │
│  │  │  │                                                │   │     │   │
│  │  │  │  ┌────────────────────────────────────┐        │   │     │   │
│  │  │  └──┤        AlertasPreco               │        │   │     │   │
│  │  │     ├────────────────────────────────────┤        │   │     │   │
│  │  │     │ Id (PK)                            │        │   │     │   │
│  │  │     │ Email                              │        │   │     │   │
│  │  │     │ RotaId (FK)                        │        │   │     │   │
│  │  │     │ PrecoAlvo                          │        │   │     │   │
│  │  │     │ Status (Ativo/Disparado)           │        │   │     │   │
│  │  │     │ DataCriacao                        │        │   │     │   │
│  │  │     └────────────────────────────────────┘        │   │     │   │
│  │  └────────────────────────────────────────────────────┘   │     │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### 5.2 Injeção de Dependências (Registros no [`Program.cs`](src/RedCodeApi/Program.cs))

```csharp
// Cache
builder.Services.AddMemoryCache();                          // L1 - Local
builder.Services.AddStackExchangeRedisCache(options => {    // L2 - Redis (opcional)
    options.Configuration = connectionString;
});

// Banco de Dados
builder.Services.AddTransient<SqlConnection>(_ => new SqlConnection(connStr));

// Serviços
builder.Services.AddScoped<NormalizadorDados>();
builder.Services.AddScoped<CacheService>();

// Scrapers (Strategy Pattern - múltiplas implementações da mesma interface)
builder.Services.AddScoped<IVooScraper, ScraperLatam>();
builder.Services.AddScoped<IVooScraper, ScraperGol>();
builder.Services.AddScoped<IVooScraper, ScraperAzul>();
builder.Services.AddScoped<IVooScraper, ScraperDecolar>();

// Hangfire (Background Jobs)
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

// HttpClient
builder.Services.AddHttpClient();
```

---

## 6. Estratégia de Scraping

### 6.1 Arquitetura do Motor de Scraping

```
┌─────────────────────────────────────────────────────────────────┐
│                     Endpoint de Busca                           │
│              GET /api/voos/busca?origem=&destino=&dataPartida=  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    CacheService.Obter(chave)                     │
│                                                                  │
│  ┌──────────┐    ┌──────────┐    ┌────────────────────┐        │
│  │ Redis    │───►│ Memory   │───►│ Cache Miss         │        │
│  │ Hit?     │    │ Cache    │    │ → Executar Scrapers│        │
│  │ Retorna  │    │ Hit?     │    └─────────┬──────────┘        │
│  └──────────┘    │ Retorna  │              │                    │
│                  └──────────┘              ▼                    │
└─────────────────────────────────────────────────────────────────┘
                                             │
                    ┌────────────────────────┼────────────────────┐
                    │                        │                     │
                    ▼                        ▼                     ▼
           ┌────────────────┐    ┌────────────────┐    ┌────────────────┐
           │  ScraperLatam  │    │  ScraperGol    │    │  ScraperAzul  │
           │  Ordem: 1      │    │  Ordem: 2      │    │  Ordem: 3     │
           │  HttpClient    │    │  HttpClient    │    │  HttpClient   │
           │  + HtmlAgility │    │  + HtmlAgility │    │  + HtmlAgility│
           └────────┬───────┘    └────────┬───────┘    └───────┬────────┘
                    │                     │                    │
                    └─────────────────────┼────────────────────┘
                                          │
                                          ▼
                              ┌─────────────────────┐
                              │  ScraperDecolar     │
                              │  Ordem: 4           │
                              │  PuppeteerSharp     │
                              │  (Headless Browser) │
                              └─────────────────────┘
                                          │
                                          ▼
                              ┌─────────────────────┐
                              │  NormalizadorDados  │
                              │  ─────────────      │
                              │  1. Padronizar      │
                              │  2. Deduplicar      │
                              │  3. Remover Outliers│
                              │  4. Ordenar (preço) │
                              └──────────┬──────────┘
                                         │
                                         ▼
                              ┌─────────────────────┐
                              │  CacheService       │
                              │  .Armazenar(chave)  │
                              └──────────┬──────────┘
                                         │
                                         ▼
                              ┌─────────────────────┐
                              │  Salvar Preços no   │
                              │  Histórico (async)  │
                              └─────────────────────┘
```

### 6.2 Estratégias por Fonte

| Scraper | Classe | Técnica | Ferramenta | Complexidade | Ordem |
|---------|--------|---------|------------|--------------|-------|
| **LATAM** | [`ScraperLatam.cs`](src/RedCodeApi/Services/Scrapers/ScraperLatam.cs) | Parse de HTML estático | HttpClient + HtmlAgilityPack | Média | 1º |
| **GOL** | [`ScraperGol.cs`](src/RedCodeApi/Services/Scrapers/ScraperGol.cs) | Parse de HTML estático | HttpClient + HtmlAgilityPack | Média | 2º |
| **Azul** | [`ScraperAzul.cs`](src/RedCodeApi/Services/Scrapers/ScraperAzul.cs) | Parse de HTML estático | HttpClient + HtmlAgilityPack | Média | 3º |
| **Decolar** | [`ScraperDecolar.cs`](src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs) | Browser Automation | PuppeteerSharp (headless Chromium) | Alta | 4º |

### 6.3 Pipeline de Normalização

Definido em [`NormalizadorDados.cs`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs):

```
Dados Brutos (scrapers)
       │
       ▼
┌──────────────────┐
│  1. Padronizar   │  → Normaliza nomes de companhias
│     Campos       │    (LATAM → LATAM Airlines)
│                  │  → Formata tipos de tarifa
│                  │  → Valida códigos IATA (regex ^[A-Z]{3}$)
└──────────────────┘
       │
       ▼
┌──────────────────┐
│  2. Deduplicar   │  → Remove voos duplicados (mesmo código)
│                  │  → Mantém o mais barato
└──────────────────┘
       │
       ▼
┌──────────────────┐
│  3. Remover      │  → Remove preços extremos
│     Outliers     │  → Método: 3 desvios padrão (3σ)
└──────────────────┘
       │
       ▼
┌──────────────────┐
│  4. Sort         │  → Ordena por preço (menor → maior)
└──────────────────┘
       │
       ▼
  Dados Normalizados
```

### 6.4 Resiliência do Motor de Scraping

- **Isolamento**: Cada scraper tem `try/catch` próprio — falha em um não afeta os demais
- **Timeout**: 30 segundos para PuppeteerSharp
- **Logging**: Todos os scrapers usam `ILogger<T>` com níveis adequados (Warning para falhas esperadas, Error para exceções)
- **Fallback Mock**: Quando todos os scrapers falham ou retornam vazio, dados mock são gerados como fallback
- **Browser Compartilhado** (Decolar): Instância única `IBrowser` reutilizada com `SemaphoreSlim` para controle de concorrência

---

## 7. Sistema de Cache em Duas Camadas

### 7.1 Arquitetura

```
                    ┌────────────────────────────┐
                    │     Request de Busca       │
                    └────────────┬───────────────┘
                                 │
                                 ▼
                    ┌────────────────────────────┐
                    │    CacheService.Obter()    │
                    └────────────┬───────────────┘
                                 │
                    ┌────────────┴───────────────┐
                    │         Redis Hit?         │
                    └────────────┬───────────────┘
                           ┌────┴────┐
                           ▼         ▼
                      (Sim)       (Não)
                         │          │
                         │    ┌─────┴──────────────┐
                         │    │   MemoryCache Hit? │
                         │    └─────┬──────────────┘
                         │      ┌───┴────┐
                         │      ▼        ▼
                         │    (Sim)    (Não)
                         │      │        │
                         │      │        ▼
                         │      │   ┌──────────────┐
                         │      │   │ Cache Miss   │
                         │      │   │ → Scrapers   │
                         │      │   │ → Normalizar │
                         │      │   │ → Armazenar  │
                         │      │   └──────────────┘
                         │      │        │
                         ▼      ▼        ▼
                    ┌────────────────────────────┐
                    │      Retorna Resultado     │
                    └────────────────────────────┘
```

### 7.2 Configuração

| Camada | Tecnologia | TTL | Expiração | Finalidade |
|--------|-----------|-----|-----------|------------|
| **L1 — Memória** | `IMemoryCache` | 30 minutos | Sliding: 10 min | Cache local de acesso rápido |
| **L2 — Redis** | `IDistributedCache` (StackExchange.Redis) | 30 minutos | Absoluta | Cache distribuído compartilhado |

### 7.3 Chaves de Cache

```
Formato: voo:{ORIGEM}:{DESTINO}:{yyyyMMdd}
Exemplo: voo:GRU:REC:20250615
```

### 7.4 Estratégia de Leitura (Cache-Aside)

1. Tenta **Redis** (`IDistributedCache`). Se hit → retorna.
2. Se Redis miss (exceção ou `null`) → tenta **memória** (`IMemoryCache`). Se hit → retorna.
3. Se ambos miss → executa scrapers, normaliza, armazena em ambas as camadas, retorna.

### 7.5 Estratégia de Escrita

1. Salva no **Redis** com TTL de 30 minutos
2. Salva na **memória** com sliding expiration de 10 minutos

### 7.6 Cache Warming

O job `AtualizarRotasPopulares()` (Hangfire, a cada 6 horas) pré-carrega o cache para 12 rotas populares, garantindo que as primeiras buscas dos usuários sejam servidas do cache.

---

## 8. Jobs Agendados (Hangfire)

### 8.1 Arquitetura

```
┌─────────────────────────────────────────────────────────────────┐
│                     Hangfire Server                             │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  MemoryStorage (in-memory, desenvolvimento)             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌────────────────────────┐  ┌──────────────────────────┐      │
│  │  Job: AtualizarRotas   │  │  Job: VerificarAlertas   │      │
│  │  Populares             │  │                          │      │
│  ├────────────────────────┤  ├──────────────────────────┤      │
│  │  Cron: 0 */6 * * *    │  │  Cron: 0 */2 * * *      │      │
│  │  (a cada 6 horas)      │  │  (a cada 2 horas)        │      │
│  ├────────────────────────┤  ├──────────────────────────┤      │
│  │  Propósito:            │  │  Propósito:              │      │
│  │  Cache warming para    │  │  Verificar alertas       │      │
│  │  rotas populares       │  │  ativos e disparar se    │      │
│  │                        │  │  preço ≤ preço alvo      │      │
│  └────────────────────────┘  └──────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 Job 1: `AtualizarRotasPopulares`

| Propriedade | Valor |
|-------------|-------|
| **Classe** | [`ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) |
| **Cron** | `0 */6 * * *` (a cada 6 horas) |
| **Função** | Executa scraping para todas as 12 rotas populares e armazena no cache |
| **Propósito** | Cache warming — buscas de usuários comuns são servidas do cache |

### 8.3 Job 2: `VerificarAlertas`

| Propriedade | Valor |
|-------------|-------|
| **Classe** | [`ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) |
| **Cron** | `0 */2 * * *` (a cada 2 horas) |
| **Função** | Para cada alerta ativo, verifica se há voos no banco com preço ≤ preço alvo |
| **Ação** | Se condição satisfeita, marca o alerta como "Disparado" (`Status = 1`) |

### 8.4 Dashboard

Acessível em `http://localhost:5246/hangfire` durante o desenvolvimento (configurado em [`Program.cs:198-220`](src/RedCodeApi/Program.cs#198)).

---

## 9. Modelo de Dados e Persistência

### 9.1 Estratégia de Persistência

| Aspecto | Decisão | Motivação |
|---------|---------|-----------|
| **ORM** | Dapper (Micro-ORM) | Performance, controle total sobre SQL, consistência com o legado RedCode |
| **Banco** | SQLite (desenvolvimento) / SQL Server (produção futuro) | Simplicidade local, sem necessidade de servidor de banco |
| **Queries** | SQL puro com parâmetros nomeados (`new { origem, destino, ... }`) | Proteção contra SQL injection, clareza |
| **Schema** | 6 tabelas, modelagem normalizada | Integridade referencial com FKs |

### 9.2 Modelo Relacional

```
CompanhiasAereas (1) ────── (N) Voos (N) ────── (N) Precos
                                    │
Aeroportos (1) ────── (N) Rotas (N) ────── (1) Voos
                                    │
                                    └────────── (N) AlertasPreco
```

### 9.3 Tabelas

| # | Tabela | Finalidade | PK | FKs |
|---|--------|-----------|----|-----|
| 1 | [`CompanhiasAereas`](db/script-flycompare.sql) | Cadastro de companhias (LATAM, GOL, Azul) | `Id` | — |
| 2 | [`Aeroportos`](db/script-flycompare.sql) | Aeroportos com código IATA, cidade, coordenadas | `Id` | — |
| 3 | [`Rotas`](db/script-flycompare.sql) | Relação origem-destino | `Id` | `OrigemId → Aeroportos.Id`, `DestinoId → Aeroportos.Id` |
| 4 | [`Voos`](db/script-flycompare.sql) | Resultados de scraping | `Id` | `RotaId → Rotas.Id`, `CompanhiaId → CompanhiasAereas.Id` |
| 5 | [`Precos`](db/script-flycompare.sql) | Histórico de preços | `Id` | `VooId → Voos.Id` |
| 6 | [`AlertasPreco`](db/script-flycompare.sql) | Alertas de preço dos usuários | `Id` | `RotaId → Rotas.Id` |

### 9.4 Seed Data

- **3 companhias**: LATAM, GOL, Azul
- **15 aeroportos**: GRU, CGH, GIG, SDU, BSB, CNF, POA, REC, SSA, FOR, MAO, BEL, CWB, FLN, VCP
- **Rotas populares**: GRU↔GIG, GRU↔BSB, GRU↔REC, GRU↔POA, GRU↔FOR, GRU↔SSA, GRU↔CNF, GRU↔CWB, GRU↔FLN, GRU↔MAO, GIG↔GRU, GIG↔SSA

---

## 10. Fluxos de Dados Críticos

### 10.1 Fluxo de Busca de Voos (Happy Path)

```
Usuário                  Frontend                    API                      Cache                Scrapers              Banco
   │                        │                        │                        │                     │                     │
   │  Preenche origem,      │                        │                        │                     │                     │
   │  destino, data         │                        │                        │                     │                     │
   │───────────────────────►│                        │                        │                     │                     │
   │                        │  GET /api/voos/busca   │                        │                     │                     │
   │                        │───────────────────────►│                        │                     │                     │
   │                        │                        │  CacheService.Obter()  │                     │                     │
   │                        │                        │───────────────────────►│                     │                     │
   │                        │                        │                        │  Cache Miss          │                     │
   │                        │                        │◄───────────────────────┤                     │                     │
   │                        │                        │                        │                     │                     │
   │                        │                        │  Executa scrapers      │                     │                     │
   │                        │                        │  em paralelo           │                     │                     │
   │                        │                        │────────────────────────┬────────────────────►│                     │
   │                        │                        │                        │                     │                     │
   │                        │                        │◄───────────────────────┴─────────────────────┤                     │
   │                        │                        │                        │                     │                     │
   │                        │                        │  Normalizador          │                     │                     │
   │                        │                        │  .Normalizar()         │                     │                     │
   │                        │                        │                        │                     │                     │
   │                        │                        │  CacheService          │                     │                     │
   │                        │                        │  .Armazenar()          │                     │                     │
   │                        │                        │───────────────────────►│                     │                     │
   │                        │                        │                        │                     │                     │
   │                        │                        │  Salvar histórico      │                     │                     │
   │                        │                        │  (async, fire-and-     │                     │                     │
   │                        │                        │   forget)              │                     │                     │
   │                        │                        │───────────────────────────────────────────────►│                     │
   │                        │                        │                        │                     │                     │
   │                        │◄───────────────────────┤                        │                     │                     │
   │                        │                        │                        │                     │                     │
   │  Exibe resultados      │                        │                        │                     │                     │
   │◄───────────────────────┤                        │                        │                     │                     │
```

### 10.2 Fluxo de Criação de Alerta

```
Usuário                  Frontend                    API                      Banco
   │                        │                        │                        │
   │  Preenche email,       │                        │                        │
   │  origem, destino,      │                        │                        │
   │  preço alvo            │                        │                        │
   │───────────────────────►│                        │                        │
   │                        │  POST /api/alertas     │                        │
   │                        │  {email, origem,       │                        │
   │                        │   destino, precoAlvo}  │                        │
   │                        │───────────────────────►│                        │
   │                        │                        │                        │
   │                        │                        │  Valida parâmetros     │
   │                        │                        │  Busca RotaId          │
   │                        │                        │───────────────────────►│
   │                        │                        │◄───────────────────────┤
   │                        │                        │                        │
   │                        │                        │  INSERT INTO           │
   │                        │                        │  AlertasPreco          │
   │                        │                        │───────────────────────►│
   │                        │                        │◄───────────────────────┤
   │                        │                        │                        │
   │                        │◄─────── 201 Created ───┤                        │
   │                        │                        │                        │
   │  "Alerta criado!"      │                        │                        │
   │◄───────────────────────┤                        │                        │
```

### 10.3 Fluxo de Verificação de Alertas (Hangfire Job)

```
Hangfire                    API/Scheduler                      Banco
   │                            │                                │
   │  Cron: 0 */2 * * *        │                                │
   │──────────────────────────►│                                │
   │                            │  SELECT * FROM AlertasPreco   │
   │                            │  WHERE Ativo = 1              │
   │                            │──────────────────────────────►│
   │                            │◄──────────────────────────────┤
   │                            │                                │
   │                            │  Para cada alerta:            │
   │                            │  SELECT MIN(PrecoTotal)       │
   │                            │  FROM Precos p                │
   │                            │  JOIN Voos v ON p.VooId=v.Id  │
   │                            │  JOIN Rotas r ON v.RotaId=... │
   │                            │  WHERE ...                    │
   │                            │──────────────────────────────►│
   │                            │◄──────────────────────────────┤
   │                            │                                │
   │                            │  Se preço ≤ PrecoAlvo:        │
   │                            │  UPDATE AlertasPreco          │
   │                            │  SET Ativo = 0                │
   │                            │──────────────────────────────►│
```

---

## 11. Padrões de Projeto

| Padrão | Onde | Descrição | Benefício |
|--------|------|-----------|-----------|
| **Strategy** | [`IVooScraper`](src/RedCodeApi/Services/Scrapers/IVooScraper.cs) + 4 implementações | Algoritmos de scraping intercambiáveis por companhia | Nova fonte = nova classe, sem modificar código existente |
| **Singleton** | [`ScraperDecolar.cs:13-14`](src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs#13) | Instância única do browser Puppeteer | Reduz consumo de memória (~100-200MB por instância) |
| **Facade** | [`ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) | Abstrai orquestração de scrapers, cache e banco | Endpoints não precisam conhecer detalhes dos scrapers |
| **Repository (implícito)** | [`Program.cs`](src/RedCodeApi/Program.cs) — SQL direto com Dapper | Acesso a dados encapsulado nas queries | Simplicidade, sem camada extra de abstração |
| **Cache-Aside / Proxy** | [`CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) | Duas camadas de cache com fallback automático | Resiliência: se Redis cai, memória ainda serve |
| **Pipeline** | [`NormalizadorDados.cs`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs) | Sequência de transformações (4 etapas) | Cada etapa é testável isoladamente |
| **Minimal API** | [`Program.cs`](src/RedCodeApi/Program.cs) | Definição de endpoints sem controllers | Menos boilerplate, endpoints co-locados |
| **Injeção de Dependência** | Todo o backend | DI nativa do .NET | Testabilidade, desacoplamento |
| **Fire-and-Forget** | Persistência de histórico | `Task.Run(async () => await SalvarPrecos())` | Não bloqueia resposta da busca |

---

## 12. Segurança e Resiliência

### 12.1 Segurança

| Aspecto | Implementação | Referência |
|---------|--------------|------------|
| **CORS** | Política "BlazorPolicy" permitindo qualquer origem (desenvolvimento) | [`Program.cs`](src/RedCodeApi/Program.cs) |
| **SQL Injection** | Dapper com parâmetros nomeados (`new { origem, destino }`) | Todas as queries |
| **Validação IATA** | Regex `^[A-Z]{3}$` | [`NormalizadorDados.cs:160-164`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs#160) |
| **Autenticação** | Não implementada (planejada para versões futuras) | — |

### 12.2 Resiliência

| Padrão | Implementação | Impacto |
|--------|--------------|---------|
| **Graceful Degradation** | Cada scraper em `try/catch` independente; falha de um não afeta os demais | Disponibilidade parcial mesmo com fontes indisponíveis |
| **Cache-Aside com Fallback** | Redis → Memory → Scrapers → Mock | Múltiplas camadas de proteção |
| **Outlier Removal** | Método dos 3 desvios padrão (3σ) | Preços extremos não poluem resultados |
| **Fallback Mock** | Dados gerados artificialmente se scraping falha | Usuário nunca vê tela vazia |
| **Timeout** | 30 segundos para PuppeteerSharp | Evita requests pendentes infinitas |
| **Logging Estruturado** | `ILogger<T>` em todos os componentes | Rastreabilidade e debug |

### 12.3 Limitações Conhecidas

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Scrapers dependem do HTML dos sites | Mudanças de layout quebram parsing | Testes de integração periódicos |
| PuppeteerSharp requer Chromium | ~150MB download na primeira execução | Documentado no setup |
| Redis é opcional | Sem Redis, cache não persiste restart | MemoryCache como fallback |
| Sem escalabilidade horizontal | Redis + session affinity seriam necessários | Planejado para versões futuras |
| Sem autenticação | Qualquer e-mail pode consultar alertas | Sistema de login planejado |

---

## 13. Arquitetura de Deploy

### 13.1 Diagrama de Deploy (Desenvolvimento)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Máquina Local (Dev)                          │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  Processo .NET (Kestrel)                                │   │
│  │  Porta: 5246                                            │   │
│  │                                                         │   │
│  │  ┌──────────────────┐  ┌──────────────────┐             │   │
│  │  │  API + Frontend  │  │  Hangfire Server │             │   │
│  │  │  (Blazor WASM    │  │  (+ Dashboard)   │             │   │
│  │  │   servido pelo   │  │                  │             │   │
│  │  │   Kestrel)       │  └──────────────────┘             │   │
│  │  └──────────────────┘                                   │   │
│  │                                                         │   │
│  │  ┌──────────────────┐  ┌──────────────────┐             │   │
│  │  │  SQLite          │  │  Redis (Docker)  │             │   │
│  │  │  redcode.db  │  │  Porta: 6379     │             │   │
│  │  └──────────────────┘  └──────────────────┘             │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### 13.2 Stack de Deploy (Produção Futuro)

```
┌─────────────────────────────────────────────────────────────────┐
│                    Load Balancer                                │
└────────────────────────────┬────────────────────────────────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
┌─────────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
│  Instância API #1   │ │ Instância #2    │ │ Instância #3       │
│  Kestrel :5246      │ │ Kestrel :5246   │ │ Kestrel :5246      │
│  + Hangfire         │ │ + Hangfire      │ │ + Hangfire         │
└──────────┬──────────┘ └────────┬────────┘ └──────────┬──────────┘
           │                     │                     │
           └─────────────────────┼─────────────────────┘
                                 │
                    ┌────────────┴────────────┐
                    ▼                         ▼
           ┌────────────────┐      ┌─────────────────────┐
           │  Redis Cluster  │      │  SQL Server (Azure) │
           │  (Cache Comp.)  │      │  (Persistência)     │
           └────────────────┘      └─────────────────────┘
```

### 13.3 Scripts de Automação

| Script | Propósito |
|--------|-----------|
| [`setup-local.ps1`](setup-local.ps1) | Setup automatizado do ambiente de desenvolvimento |
| [`dev-all.mjs`](scripts/dev-all.mjs) | Inicia API e frontend simultaneamente |
| [`postinstall.mjs`](scripts/postinstall.mjs) | Pós-instalação npm (restore dotnet tools) |

---

## 14. Decisões Arquiteturais (ADRs)

As principais decisões arquiteturais estão documentadas como **Architecture Decision Records** (ADRs) no diretório [`docs/pivotagem/`](docs/pivotagem/).

### ADR-001: Arquitetura do FlyCompare

**Arquivo**: [`ADR-001-arquitetura-metabuscador-passagens-aereas.md`](docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md)  
**Status**: ✅ Aceito  
**Data**: 2026-05-14

**Decisões principais**:
1. **Scraping** como estratégia primária de coleta de dados (vs. APIs pagas)
2. **Cache em duas camadas** (memória + Redis)
3. **Dapper** como ORM (mantido do legado)
4. **Scraping síncrono** na request (Fases 3-4) → assíncrono (Fase 5+)
5. **Blazor WASM** mantido como frontend
6. **Strategy Pattern** para scrapers (`IVooScraper`)

### ADRs Incorporados no Código

| Decisão | Contexto | Alternativa Rejeitada | Referência |
|---------|----------|-----------------------|------------|
| Dapper > EF Core | Performance, consistência com legado | Entity Framework Core | [`Program.cs`](src/RedCodeApi/Program.cs) |
| HtmlAgility + Puppeteer | Flexibilidade por fonte | Apenas Puppeteer (lento) ou apenas HtmlAgility (limitado) | [`Services/Scrapers/`](src/RedCodeApi/Services/Scrapers/) |
| Cache em duas camadas | Resiliência e performance | Apenas memória (perde ao restart) ou apenas Redis (ponto único de falha) | [`CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) |
| Minimal API | Menos boilerplate, endpoints co-locados | Controllers tradicionais | [`Program.cs`](src/RedCodeApi/Program.cs) |
| Hangfire com MemoryStorage | Simplicidade em dev | SQL Server Storage (produção) | [`Program.cs:198-220`](src/RedCodeApi/Program.cs#198) |
| SQLite em dev | Zero dependência de servidor | SQL Server em Docker | [`appsettings.Development.json`](src/RedCodeApi/appsettings.Development.json) |

---

## 15. Evolução e Roadmap Técnico

### 15.1 Arquitetura Atual (F0-F4 implementadas)

```
✅ F0: Setup, models, banco de dados
✅ F1: Endpoints REST, frontend básico
✅ F2: Scrapers, normalização, cache em memória
✅ F3: Scrapers expandidos (Gol, Azul, Decolar), cache Redis, Hangfire
✅ F4: Alertas de preço, jobs agendados, filtros frontend
```

### 15.2 Próximos Passos (F5+)

```
⬜ F5: Melhorias, refatoração, deploy
├── Scraping assíncrono com SignalR (resultados parciais em tempo real)
├── Autenticação JWT / Identity
├── Testes de integração para scrapers
├── Deploy em cloud (Azure / AWS)
└── Monitoramento de scrapers (health checks, alertas de falha)

⬜ F6: Qualidade e escala
├── Cache warming preditivo (baseado em histórico de buscas)
├── Rate limiting para scrapers (proxies rotativos)
├── Escalabilidade horizontal (Redis + session affinity)
└── Pipeline CI/CD
```

### 15.3 Padrão de Evolução

A arquitetura foi projetada para evolução gradual:

```
Simplicidade ─────────────────────────────────► Complexidade
    │                        │                        │
    ▼                        ▼                        ▼
Cache em        ──►   Cache em duas     ──►   Cache distribuído
memória               camadas                   + warming
(L1)                  (L1 + L2)                 preditivo

Scraping        ──►   Scraping          ──►   Scraping assíncrono
síncrono              paralelo                  + SignalR
(mock)                (4 fontes)

Sem             ──►   Alertas por       ──►   Autenticação +
autenticação          e-mail                   notificação por email

SQLite local    ──►   SQLite + Redis     ──►   SQL Server + Redis
                                                  + escalabilidade
```

---

## Apêndice A: Referências

| Documento | Descrição | Link |
|-----------|-----------|------|
| **Visão do Sistema** | Documento de visão geral do FlyCompare | [`docs/visao.md`](visao.md) |
| **ADR-001** | Architecture Decision Record principal | [`docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md`](pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md) |
| **Plano de Pivotagem** | Migração RedCode → FlyCompare | [`docs/pivotagem/PIVOTAGEM.md`](pivotagem/PIVOTAGEM.md) |
| **Requisitos FlyCompare** | Histórias de usuário e critérios BDD | [`docs/pivotagem/REQUISITOS-FLYCOMPARE.md`](pivotagem/REQUISITOS-FLYCOMPARE.md) |
| **Roadmap Técnico** | 33 SPECs de implementação | [`docs/pivotagem/ROADMAP.md`](pivotagem/ROADMAP.md) |
| **Script SQL** | Schema do banco de dados | [`db/script-flycompare.sql`](db/script-flycompare.sql) |
| **README** | Instruções de setup e uso | [`README.md`](README.md) |

## Apêndice B: Glossário

| Termo | Definição |
|-------|-----------|
| **Metabuscador** | Sistema que agrega resultados de múltiplas fontes em uma única interface |
| **OTA** | Online Travel Agency (ex: Decolar, Kayak, Skyscanner) |
| **Scraping** | Extração automatizada de dados de sites web |
| **IATA** | Código de 3 letras que identifica aeroportos (ex: GRU, REC) |
| **Cache Warming** | Pré-carregamento do cache com dados antes que usuários solicitem |
| **Graceful Degradation** | Capacidade de manter funcionalidade parcial mesmo com falha de componentes |
| **Outlier** | Valor extremo que se distancia significativamente da média |
| **TTL** | Time-To-Live — tempo de vida de um dado em cache |
| **Sliding Expiration** | Expiração que renova a cada acesso ao cache |

---

> **Documento gerado em:** 21 de maio de 2026  
> **Baseado em:** [`docs/visao.md`](visao.md), [`docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md`](pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md), [`docs/pivotagem/PIVOTAGEM.md`](pivotagem/PIVOTAGEM.md), [`docs/pivotagem/ROADMAP.md`](pivotagem/ROADMAP.md)  
> **Versão do projeto:** FlyCompare (pós-pivot RedCode)  
> **Stack principal:** .NET 10 Minimal API · Blazor WebAssembly · SQLite · Dapper · Hangfire · HtmlAgilityPack · PuppeteerSharp
