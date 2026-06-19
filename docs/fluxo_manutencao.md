# Fluxo de Manutenção — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

---

## Classificação de Manutenção (Taxonomia Swanson)

| Ticket | Descrição | Classificação | Justificativa |
|--------|-----------|---------------|---------------|
| Ticket 1 | Corrigir `NullReferenceException` ao recarregar alertas após criação (CRIT-03) | **Corretiva** | Correção de bug que causava falha em runtime |
| Ticket 2 | Substituir `IMemoryCache` por `CacheService` no `ScrapingScheduler` (CRIT-01) | **Corretiva** | Bug onde Redis era ignorado, causando cache miss permanente |
| Ticket 3 | Adicionar `CancellationToken` ao endpoint de busca (CRIT-04) | **Corretiva** | Bug onde scrapers continuavam executando após cancelamento da requisição |
| Ticket 4 | Remover tabelas e endpoints legados do sistema de eventos/cupons (SPEC-028/029/030) | **Adaptativa** | Adaptação do sistema para novo domínio (de eventos para passagens aéreas) |
| Ticket 5 | Adicionar `PrecoSemTaxas` e `Taxas` ao modelo `ResultadoBusca` do Frontend (LOW-03) | **Adaptativa** | Adaptação do modelo para suportar novos campos do domínio |
| Ticket 6 | Adicionar envio de email SMTP para notificação de alertas (LOW-01) | **Perfectiva** | Melhoria de funcionalidade existente (antes só logava, agora notifica) |
| Ticket 7 | Implementar SPEC-034 — Motor de Regras + Score de recomendação de preços | **Perfectiva** | Nova funcionalidade que melhora a experiência do usuário |
| Ticket 8 | Completar SPEC-033 — Layout final com breadcrumbs, footer, responsividade | **Perfectiva** | Melhoria de UX/UI sem alterar funcionalidade core |
| Ticket 9 | Adicionar health check no browser Puppeteer do `ScraperDecolar` (LOW-02) | **Preventiva** | Previne falha catastrófica se o browser travar |
| Ticket 10 | Otimizar SQL do `VerificarAlertas()` — 3 subconsultas para 1 CTE (CRIT-05) | **Preventiva** | Previne degradação de performance com crescimento de dados |
| Ticket 11 | Substituir `dynamic` por DTO tipado `AlertaComPreco` (BUS-10) | **Preventiva** | Previne erros de runtime por falta de tipagem |
| Ticket 12 | Restringir CORS de `AllowAnyOrigin` para origem específica (LOW-13) | **Preventiva** | Previne vulnerabilidade de segurança |

---

## Pipeline de Liberação Segura

### 1. Análise de Impacto

Antes de qualquer liberação, a equipe avalia:
- **Escopo da mudança:** Quais arquivos, namespaces e endpoints são afetados?
- **Dependências:** Outros serviços ou componentes dependem do código alterado?
- **Risco de regressão:** A mudança pode quebrar funcionalidades existentes?
- **Dados:** A mudança afeta estrutura de banco ou migrações?

Ferramenta: `git diff --stat` + revisão manual dos arquivos alterados.

### 2. Teste como Instrumento Cirúrgico

- **Testes unitários:** Executar antes de cada commit (`dotnet test tests/RedCodeTests.csproj`)
- **Testes de integração:** Validar endpoints reais com `WebApplicationFactory`
- **Testes manuais:** Verificar fluxo completo no Blazor WASM (busca → resultados → alertas)
- **Evidência:** 27/27 testes passando = ✅ build aprovado

### 3. Feature Toggle

Funcionalidades experimentais ou de risco são protegidas por feature toggles:
- **Configuração:** `appsettings.json` com chaves booleanas
- **Exemplo:** `"Features:EmailNotificacao": true` — desativa envio de email em ambiente de desenvolvimento
- **Exemplo:** `"Redis:ConnectionString": null` — Redis é opcional; sistema funciona apenas com memory cache

### 4. Estratégia de Release e Regressão

- **Branch strategy:** `main` (produção) ← `develop` (integração) ← `feat/*` (features)
- **Rollback:** Reverter commit no GitHub + redeploy. SQLite facilita (arquivo único, sem migrações complexas)
- **Monitoramento:** Logs estruturados com níveis (Information, Warning, Error) via `ILogger<T>`
- **Rollback rápido:** `git revert <commit>` + `dotnet run`

---

## Resumo

| Métrica | Valor |
|---------|-------|
| Total de tickets classificados | 12 |
| Corretivas | 3 (25%) |
| Adaptativas | 2 (17%) |
| Perfectivas | 3 (25%) |
| Preventivas | 4 (33%) |

**Observação:** Alta proporção de manutenção preventiva (33%) indica maturidade do time em antecipar problemas antes que ocorram em produção.
