# FlyCompare — Roadmap do Projeto

> **Metabuscador de Passagens Aéreas** | .NET 10 + Blazor WebAssembly + SQLite
>
> Baseado na [visão do sistema](visao.md) e na [arquitetura](arquitetura.md)

---

## Índice

1. [Visão Geral do Roadmap](#1-visão-geral-do-roadmap)
2. [Fase 0 — Fundação (Concluída)](#2-fase-0--fundação-concluída)
3. [Fase 1 — API de Consulta (Concluída)](#3-fase-1--api-de-consulta-concluída)
4. [Fase 2 — Motor de Scraping (Concluída)](#4-fase-2--motor-de-scraping-concluída)
5. [Fase 3 — Expansão do Scraping (Concluída)](#5-fase-3--expansão-do-scraping-concluída)
6. [Fase 4 — Alertas e Experiência (Concluída)](#6-fase-4--alertas-e-experiência-concluída)
7. [Fase 5 — Produção e Qualidade (Próxima)](#7-fase-5--produção-e-qualidade-próxima)
8. [Fase 6 — Escala e Avançado (Futuro)](#8-fase-6--escala-e-avançado-futuro)
9. [Dependências entre Fases](#9-dependências-entre-fases)
10. [Critérios de Progresso](#10-critérios-de-progresso)

---

## 1. Visão Geral do Roadmap

```
         Passado                         Presente                        Futuro
    ─────────────────────────────────────────────────────────────────────────────►
    
    ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐
    │  Fase 0 │  │  Fase 1 │  │  Fase 2 │  │  Fase 3 │  │  Fase 4 │  │  Fase 5 │
    │ Fundação│  │ Consulta│  │ Scraping│  │Expansão │  │ Alertas │  │Produção │
    │         │  │         │  │         │  │         │  │         │  │         │
    │ ✅      │  │ ✅      │  │ ✅      │  │ ✅      │  │ ✅      │  │ 🏗️     │
    └─────────┘  └─────────┘  └─────────┘  └─────────┘  └─────────┘  └─────────┘
                                                                      ┌─────────┐
                                                                      │  Fase 6 │
                                                                      │ Escala  │
                                                                      │         │
                                                                      │ 🔭      │
                                                                      └─────────┘
```

### Status Atual

| Métrica | Valor |
|---------|-------|
| **Fase atual** | **Fase 5** (em planejamento) |
| **Fases concluídas** | F0, F1, F2, F3, F4 |
| **SPECs implementadas** | 30 de 33 (ver [`docs/pivotagem/ROADMAP.md`](pivotagem/ROADMAP.md)) |
| **Próximo marco** | F5 — Produção e Qualidade (SPECs 31-33) |
| **Visão completa** | [`docs/visao.md`](visao.md) |
| **Arquitetura** | [`docs/arquitetura.md`](arquitetura.md) |

---

## 2. Fase 0 — Fundação (Concluída)

**Objetivo**: Preparar o projeto para o novo domínio sem quebrar nada do existente.

### Estrutura de Pastas

```
┌──────────────────────────────────────────────────────────────┐
│                   Organização do Projeto                     │
│                                                              │
│  src/RedCodeApi/                                         │
│  ├── Models/FlyCompare/       Aeroporto, Voo, Preco, etc.   │
│  ├── Dtos/FlyCompare/         BuscaRequest, ResultadoBusca   │
│  └── Services/Scrapers/       IVooScraper, Scrapers, etc.   │
│                                                              │
│  db/script-flycompare.sql     Schema + seed data             │
│  docs/pivotagem/              ADR, plano, requisitos         │
└──────────────────────────────────────────────────────────────┘
```

### Resultados

| Item | Status | Referência |
|------|--------|------------|
| Estrutura de pastas do FlyCompare | ✅ | [`src/RedCodeApi/Models/FlyCompare/`](src/RedCodeApi/Models/FlyCompare/) |
| Script SQL das 6 tabelas | ✅ | [`db/script-flycompare.sql`](db/script-flycompare.sql) |
| Seed data (3 companhias, 15 aeroportos, rotas) | ✅ | [`db/script-flycompare.sql`](db/script-flycompare.sql) |
| Models C# do domínio | ✅ | [`src/RedCodeApi/Models/FlyCompare/`](src/RedCodeApi/Models/FlyCompare/) |
| DTOs de request/response | ✅ | [`src/RedCodeApi/Dtos/FlyCompare/`](src/RedCodeApi/Dtos/FlyCompare/) |

---

## 3. Fase 1 — API de Consulta (Concluída)

**Objetivo**: Implementar endpoints que retornam dados do banco e dados mockados, permitindo o desenvolvimento do frontend.

### Diagrama

```
Frontend (Blazor) ──HTTP──► API (Minimal API) ──Dapper──► SQLite
                                │
                                └── Dados Mock (fallback para busca de voos)
```

### Resultados

| Item | Endpoint | Status | Código |
|------|----------|--------|--------|
| Listar aeroportos | `GET /api/aeroportos` | ✅ | [`Program.cs:270-274`](src/RedCodeApi/Program.cs#270) |
| Autocomplete aeroportos | `GET /api/aeroportos/busca?q=` | ✅ | [`Program.cs:277-290`](src/RedCodeApi/Program.cs#277) |
| Listar companhias | `GET /api/companhias` | ✅ | [`Program.cs:294-297`](src/RedCodeApi/Program.cs#294) |
| Rotas populares | `GET /api/rotas/populares` | ✅ | [`Program.cs:301-304`](src/RedCodeApi/Program.cs#301) |
| Busca de voos (mock) | `GET /api/voos/busca` | ✅ | [`Program.cs:307-381`](src/RedCodeApi/Program.cs#307) |
| Página de busca | `/flycompare` | ✅ | [`BuscarVoos.razor`](src/RedCodeFront/Pages/BuscarVoos.razor) |
| Página de resultados | `/flycompare/resultados/...` | ✅ | [`ResultadosBusca.razor`](src/RedCodeFront/Pages/ResultadosBusca.razor) |

---

## 4. Fase 2 — Motor de Scraping (Concluída)

**Objetivo**: Implementar scraping real de companhias aéreas com Strategy Pattern, normalização e cache.

### Arquitetura

```
                    ┌─────────────────────┐
                    │   GET /api/voos/     │
                    │   busca              │
                    └──────────┬──────────┘
                               │
                    ┌──────────▼──────────┐
                    │   Cache Hit?        │── Sim ──► Retorna cache
                    └──────────┬──────────┘
                               │ Não
                               ▼
              ┌─────────────────────────────────┐
              │        Scrapers (Paralelo)       │
              │  ┌────────┐ ┌──────┐ ┌────────┐ │
              │  │ LATAM  │ │ GOL  │ │ Azul   │ │
              │  └────────┘ └──────┘ └────────┘ │
              └──────────────────┬──────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   NormalizadorDados     │
                    │   (4 etapas)             │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │   Cache + Resposta      │
                    └─────────────────────────┘
```

### Resultados

| Item | Status | Referência |
|------|--------|------------|
| Interface `IVooScraper` | ✅ | [`Services/Scrapers/IVooScraper.cs`](src/RedCodeApi/Services/Scrapers/IVooScraper.cs) |
| Scraper LATAM (HtmlAgilityPack) | ✅ | [`Services/Scrapers/ScraperLatam.cs`](src/RedCodeApi/Services/Scrapers/ScraperLatam.cs) |
| Normalizador de dados (4 etapas) | ✅ | [`Services/Scrapers/NormalizadorDados.cs`](src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs) |
| Scraping integrado ao endpoint | ✅ | [`Program.cs:307-381`](src/RedCodeApi/Program.cs#307) |
| Cache em memória (IMemoryCache) | ✅ | [`Services/CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) |
| Fallback mock quando scraping falha | ✅ | [`Program.cs`](src/RedCodeApi/Program.cs) |

---

## 5. Fase 3 — Expansão do Scraping (Concluída)

**Objetivo**: Adicionar mais fontes de scraping, cache Redis, histórico de preços e jobs agendados.

### Arquitetura Expandida

```
                    ┌─────────────────────────┐
                    │   Redis Cache (L2)      │
                    │   TTL: 30 min           │
                    └────────────┬────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │  Memory Cache (L1)      │
                    │  Sliding: 10 min        │
                    └────────────┬────────────┘
                                 │
              ┌──────────────────┴──────────────────┐
              │             Scrapers                 │
              │  ┌──────┐ ┌──────┐ ┌──────┐ ┌────┐ │
              │  │LATAM │ │ GOL  │ │ Azul │ │Dec │ │
              │  │(Html)│ │(Html)│ │(Html)│ │(Pup)│ │
              │  └──────┘ └──────┘ └──────┘ └────┘ │
              └──────────────────┬──────────────────┘
                                 │
                    ┌────────────▼────────────┐
                    │     Hangfire Jobs        │
                    │  ┌──────────────────┐   │
                    │  │ Cache Warming    │   │
                    │  │ (a cada 6h)      │   │
                    │  └──────────────────┘   │
                    └─────────────────────────┘
```

### Resultados

| Item | Status | Referência |
|------|--------|------------|
| Scraper GOL | ✅ | [`Services/Scrapers/ScraperGol.cs`](src/RedCodeApi/Services/Scrapers/ScraperGol.cs) |
| Scraper Azul | ✅ | [`Services/Scrapers/ScraperAzul.cs`](src/RedCodeApi/Services/Scrapers/ScraperAzul.cs) |
| Scraper Decolar (PuppeteerSharp) | ✅ | [`Services/Scrapers/ScraperDecolar.cs`](src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs) |
| Cache Redis (IDistributedCache) | ✅ | [`Services/CacheService.cs`](src/RedCodeApi/Services/CacheService.cs) |
| Histórico de preços | ✅ | [`Program.cs:385-397`](src/RedCodeApi/Program.cs#385) |
| Job de cache warming (Hangfire) | ✅ | [`Services/ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) |
| Dashboard Hangfire | ✅ | `/hangfire` |

---

## 6. Fase 4 — Alertas e Experiência (Concluída)

**Objetivo**: Sistema de alertas de preço e refinamentos de UX.

### Fluxo de Alertas

```
Usuário                     Frontend                    API                       Banco/Hangfire
   │                           │                        │                            │
   │  Cria alerta              │                        │                            │
   │  (email, rota, preço)     │                        │                            │
   │──────────────────────────►│  POST /api/alertas     │                            │
   │                           │───────────────────────►│   INSERT AlertasPreco      │
   │                           │                        │───────────────────────────►│
   │                           │◄──── 201 Created ──────┤                            │
   │◄──── "Alerta criado!" ────┤                        │                            │
   │                           │                        │                            │
   │                           │                        │   Job a cada 2h:           │
   │                           │                        │   VerificarAlertas()        │
   │                           │                        │───────────────────────────►│
   │                           │                        │   SELECT * FROM Alertas    │
   │                           │                        │◄───────────────────────────┤
   │                           │                        │                            │
   │                           │                        │   Se preço ≤ alvo:         │
   │                           │                        │   UPDATE Ativo = 0         │
```

### Resultados

| Item | Endpoint/Página | Status | Código |
|------|----------------|--------|--------|
| Criar alerta | `POST /api/alertas` | ✅ | [`Program.cs:401-421`](src/RedCodeApi/Program.cs#401) |
| Listar alertas | `GET /api/alertas/{email}` | ✅ | [`Program.cs:425-442`](src/RedCodeApi/Program.cs#425) |
| Job verificação de alertas | Hangfire (a cada 2h) | ✅ | [`Services/ScrapingScheduler.cs`](src/RedCodeApi/Services/ScrapingScheduler.cs) |
| Página de alertas | `/alertas` | ✅ | [`MeusAlertas.razor`](src/RedCodeFront/Pages/MeusAlertas.razor) |
| Filtros no frontend | companhia + paradas | ✅ | [`ResultadosBusca.razor`](src/RedCodeFront/Pages/ResultadosBusca.razor) |
| Ordenação | preço, duração, horário | ✅ | [`ResultadosBusca.razor`](src/RedCodeFront/Pages/ResultadosBusca.razor) |
| Componente de alerta | Shared | ✅ | [`Alerta.razor`](src/RedCodeFront/Shared/Alerta.razor) |

---

## 7. Fase 5 — Produção e Qualidade (Próxima)

**Objetivo**: Preparar o sistema para uso real com foco em qualidade, testes e documentação.

**Status**: 🏗️ **Em planejamento** — Próximo marco a ser executado

### 7.1 Limpeza do Código Legado

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Remover endpoints do RedCode | `POST /api/usuarios`, `POST /api/eventos`, `POST /api/cupons`, `POST /api/reservas` | 🔴 Alta | 1h |
| Remover páginas Blazor legadas | Eventos, Reservas, Cupons, Usuários | 🔴 Alta | 30min |
| Remover tabelas legadas do banco | `Usuarios`, `Eventos`, `Cupons`, `Reservas` | 🔴 Alta | 30min |
| Remover models e DTOs legados | Namespaces antigos ainda presentes | 🟡 Média | 30min |

### 7.2 Testes

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Testes unitários do Normalizador | Padronização, deduplicação, outliers, sort | 🔴 Alta | 2h |
| Testes de cache | CacheService (memória + Redis simulado) | 🔴 Alta | 2h |
| Testes de endpoints | GET/POST da API FlyCompare | 🔴 Alta | 3h |
| Testes de frontend | Blazor componentes (básico) | 🟡 Média | 3h |
| Mock de scrapers para testes | `IVooScraper` mock para testes controlados | 🟡 Média | 1h |

### 7.3 Melhorias de UX/UI

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Layout responsivo | Adaptar CSS para mobile | 🟡 Média | 4h |
| Loading states melhorados | Skeletons em vez de spinners | 🟢 Baixa | 2h |
| Mensagens de erro amigáveis | Traduzir erros técnicos para o usuário | 🟡 Média | 1h |
| Feedback visual para alertas | Toast de confirmação ao criar alerta | 🟢 Baixa | 1h |

### 7.4 Documentação

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Atualizar README.md | Novo propósito, instruções de setup, endpoints | 🔴 Alta | 1h |
| Comentários XML na API | Documentar endpoints e parâmetros | 🟡 Média | 2h |
| Guia de contribuição | Como adicionar novo scraper | 🟢 Baixa | 1h |

### 7.5 Resiliência e Observabilidade

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Health checks | Endpoint `/health` para monitoramento | 🟡 Média | 1h |
| Logging estruturado | Revisar logs para produção (Info/Warning/Error) | 🟡 Média | 2h |
| Rate limiting para scrapers | Evitar bloqueio por IP | 🟢 Baixa | 3h |
| Testes de integração periódicos | Verificar se scrapers ainda funcionam | 🟢 Baixa | 4h |

---

## 8. Fase 6 — Escala e Avançado (Futuro)

**Objetivo**: Evoluir o sistema para produção real com escalabilidade, autenticação e features avançadas.

**Status**: 🔭 **Visão de futuro** — Sem previsão definida

### 8.1 Autenticação e Usuários

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Autenticação JWT | Login/registro de usuários | 🔴 Alta | 8h |
| Alertas por usuário logado | Vincular alertas ao usuário em vez de e-mail | 🔴 Alta | 4h |
| Perfil de usuário | Histórico de buscas, alertas, preferências | 🟡 Média | 4h |

### 8.2 Scraping Avançado

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Scraping assíncrono com SignalR | Resultados parciais em tempo real | 🟡 Média | 8h |
| Proxy rotativo para scrapers | Evitar bloqueio por IP | 🟡 Média | 6h |
| Mais fontes de dados | Kayak, Skyscanner, 123Milhas | 🟢 Baixa | 6h/cada |
| APIs pagas como fallback | Amadeus, Google Flights API | 🟢 Baixa | 4h |

### 8.3 Infraestrutura e Deploy

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Deploy em cloud | Azure App Service / AWS Elastic Beanstalk | 🔴 Alta | 8h |
| SQL Server em produção | Migrar de SQLite para SQL Server | 🔴 Alta | 4h |
| Redis gerenciado | Azure Cache for Redis / AWS ElastiCache | 🟡 Média | 2h |
| Pipeline CI/CD | GitHub Actions / Azure DevOps | 🟡 Média | 6h |
| Containerização | Dockerfile + docker-compose para produção | 🟡 Média | 4h |

### 8.4 Features Avançadas

| Tarefa | Descrição | Prioridade | Esforço |
|--------|-----------|------------|---------|
| Gráfico de histórico de preços | Visualização da evolução de preços | 🟡 Média | 4h |
| Notificação por e-mail | Disparo real de e-mail quando alerta dispara | 🟡 Média | 4h |
| Cache warming preditivo | Baseado em histórico de buscas dos usuários | 🟢 Baixa | 6h |
| Modo escuro | Tema dark para o frontend | 🟢 Baixa | 3h |
| Internacionalização (i18n) | Suporte a múltiplos idiomas | 🟢 Baixa | 6h |

---

## 9. Dependências entre Fases

```
F0 ───► F1 ───► F2 ───► F3 ───► F4 ───► F5 ───► F6
 │                                             │
 │  (Setup)         (Scraping)      (Alertas)  │  (Qualidade)
 │  └ Models        └ IVooScraper   └ Alertas  │  └ Testes
 │  └ SQL           └ Latam         └ Hangfire │  └ Docs
 │  └ DTOs          └ Normalizador  └ Frontend │  └ Limpeza
 │                   └ Cache                    │
 │  (API)                                       │  (Escala)
 │  └ Endpoints                   (Expansão)    │  └ Auth
 │  └ Frontend                    └ Gol         │  └ Deploy
 │                                └ Azul        │  └ Avançado
 │                                └ Decolar     │
 │                                └ Redis       │
 │                                └ Histórico   │
```

### Regras de Progresso

1. **Não pular fases** — Cada fase depende da anterior
2. **Testes antes do código** — Implementar testes antes ou junto com o código de produção (F5)
3. **Feature flags** — Funcionalidades experimentais devem ser isoladas
4. **Commits frequentes** — Ao final de cada tarefa dentro de uma fase
5. **Documentação contínua** — Atualizar [`docs/visao.md`](visao.md) e [`docs/arquitetura.md`](arquitetura.md) conforme necessário

---

## 10. Critérios de Progresso

### Definition of Done (DoD) por Fase

| Fase | Critérios para Considerar Concluída |
|------|--------------------------------------|
| **F0** | ✅ Pastas criadas, SQL aplicável, models/DTOs compilando |
| **F1** | ✅ Endpoints retornando dados (banco + mock), frontend funcional |
| **F2** | ✅ Pelo menos 1 scraper funcional, normalização, cache em memória |
| **F3** | ✅ 4 scrapers funcionando, cache Redis, Hangfire, histórico |
| **F4** | ✅ Alertas criando/consultando, jobs agendados, filtros frontend |
| **F5** | ⬜ Código legado removido, testes passando, README atualizado |
| **F6** | ⬜ Deploy em cloud, autenticação, escalabilidade horizontal |

### Status Geral do Projeto

```
F0 ████████████████████████████████ 100%
F1 ████████████████████████████████ 100%
F2 ████████████████████████████████ 100%
F3 ████████████████████████████████ 100%
F4 ████████████████████████████████ 100%
F5 ████░░░░░░░░░░░░░░░░░░░░░░░░░░░  15%
F6 ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   0%
```

---

## Apêndice A: Histórico de Versões

| Versão | Data | Mudanças | Autor |
|--------|------|----------|-------|
| 1.0 | 2026-05-21 | Criação do roadmap baseado em [`docs/visao.md`](visao.md) e [`docs/arquitetura.md`](arquitetura.md) | Red-code |

## Apêndice B: Referências

| Documento | Descrição | Link |
|-----------|-----------|------|
| **Visão do Sistema** | Documento de visão geral | [`docs/visao.md`](visao.md) |
| **Arquitetura** | Documento de arquitetura detalhada | [`docs/arquitetura.md`](arquitetura.md) |
| **Roadmap Técnico (SPECs)** | Roadmap detalhado com 33 SPECs de implementação | [`docs/pivotagem/ROADMAP.md`](pivotagem/ROADMAP.md) |
| **Plano de Pivotagem** | Migração RedCode → FlyCompare | [`docs/pivotagem/PIVOTAGEM.md`](pivotagem/PIVOTAGEM.md) |
| **ADR-001** | Architecture Decision Record | [`docs/pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md`](pivotagem/ADR-001-arquitetura-metabuscador-passagens-aereas.md) |
| **Requisitos FlyCompare** | Histórias de usuário e BDD | [`docs/pivotagem/REQUISITOS-FLYCOMPARE.md`](pivotagem/REQUISITOS-FLYCOMPARE.md) |

---

> **Documento gerado em:** 21 de maio de 2026  
> **Baseado em:** [`docs/visao.md`](visao.md) e [`docs/arquitetura.md`](arquitetura.md)  
> **Versão do projeto:** FlyCompare (pós-pivot RedCode) — **Fase 5 em planejamento**
