# Topologia de Times — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

---

## Mapeamento Team Topologies

O projeto FlyCompare, como sistema único com escopo bem definido, se beneficia de uma estrutura de times simplificada baseada nos 4 tipos fundamentais de Team Topologies.

### 1. Stream-Aligned Team (Time Alinhado ao Fluxo)

**Responsabilidade:** Time principal de desenvolvimento do produto FlyCompare.

**Escopo:**
- Desenvolvimento full-stack: API (.NET Minimal API) + Frontend (Blazor WASM)
- Implementação de scrapers (LATAM, GOL, Azul, Decolar)
- Criação e manutenção de endpoints REST
- Desenvolvimento de páginas Blazor
- Testes unitários e de integração

**Membros:** Todos os 5 integrantes do grupo (André, João Lucas, Miguel, Pedro, Vinicius)

**Entregáveis:**
- `src/RedCodeApi/` — Backend
- `src/RedCodeFront/` — Frontend
- `tests/` — Testes automatizados

---

### 2. Platform Team (Time de Plataforma)

**Responsabilidade:** Fornecer infraestrutura e ferramentas para o time de desenvolvimento.

**Escopo:**
- Manutenção do script `setup-local.ps1` (instalação de dependências)
- Configuração do `package.json` com scripts NPM (`npm run dev`, `npm run test`)
- Gerenciamento do Hangfire (jobs de scraping e alertas)
- Configuração do Redis (cache distribuído, opcional)
- Pipeline de CI/CD (GitHub Actions, se configurado)

**Serviços internos:**
- `CacheService` — Cache de duas camadas (memória + Redis)
- `ScrapingScheduler` — Jobs recorrentes do Hangfire
- `EmailService` — Envio de notificações SMTP

**Membros:** Time inteiro atua como Platform quando necessário (modelo colaborativo)

---

### 3. Enabling Team (Time de Habilitação)

**Responsabilidade:** Ajudar o time principal a superar obstáculos técnicos e adquirir novas competências.

**Escopo:**
- Pesquisa e treinamento em PuppeteerSharp para scraping headless
- Configuração de autenticação SMTP para envio de emails
- Mentoria em padrões arquiteturais (Strategy, Cache-Aside, Pipes and Filters)
- Revisão de código focada em segurança (SSDF, SQL Injection)
- Documentação de ADRs (Architecture Decision Records)

**Entregáveis:**
- `docs/adr/` — 5 ADRs documentando decisões arquiteturais
- `docs/analise_arquitetura.md` — Análise de padrões e violações
- `docs/seguranca_ciclo.md` — Threat model e gates de segurança

---

### 4. Complicated-Subsystem Team (Time de Subsistema Complexo)

**Responsabilidade:** Gerenciar componentes que exigem conhecimento especializado.

**Escopo:**
- **ScraperDecolar** — Requer conhecimento de PuppeteerSharp e Chromium headless
- **NormalizadorDados** — Pipeline de 4 etapas com estatística (outliers via 3σ)
- **AnalisadorPrecosService** — Motor de regras + score (SPEC-034)
- **Hangfire Scheduler** — Jobs recorrentes com CRON expressions

**Abordagem:** O time principal assume responsabilidade por estes subsistemas com suporte do Enabling Team. Não há time dedicado — o conhecimento é compartilhado via documentação e pair programming.

---

## Estrutura de Interação

```
┌─────────────────────────────────────────────────────────┐
│                   Stream-Aligned Team                    │
│         (André, João Lucas, Miguel, Pedro, Vinicius)     │
│                                                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐ │
│  │ Backend  │  │ Frontend │  │ Scrapers │  │  Testes  │ │
│  │ .NET 10  │  │  Blazor  │  │HtmlAgility│  │  xUnit  │ │
│  └──────────┘  └──────────┘  └──────────┘  └─────────┘ │
│                                                          │
└──────────────────────┬───────────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        │              │              │
        ▼              ▼              ▼
┌──────────────┐ ┌─────────────┐ ┌────────────────────┐
│   Platform   │ │   Enabling   │ │Complicated-Subsystem│
│   (Scripts,  │ │  (ADRs,      │ │  (Puppeteer,        │
│   Hangfire,  │ │  Segurança,  │ │   Normalizador,     │
│   Cache)     │ │  Treinamento)│ │   Analisador)       │
└──────────────┘ └─────────────┘ └────────────────────┘
```

---

## Modo de Operação

- **Stream-Aligned** é o modo primário — 100% do tempo
- **Platform** é acionado quando scripts/infra precisam de manutenção
- **Enabling** é acionado quando surge nova tecnologia ou problema complexo
- **Complicated-Subsystem** é documentado mas operado pelo time principal

---

## Comunicação

| Canal | Frequência | Participantes |
|-------|-----------|---------------|
| Daily (assíncrono) | Diário | Time inteiro |
| Code Review | A cada PR | Mínimo 2 revisores |
| Planning | Início da sprint | Time inteiro |
| Retrospective | Final da sprint | Time inteiro |
