# Operação — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

---

## 1. Matriz de Riscos Operacionais

| Risco | Probabilidade | Impacto | Estratégia | Ação Planejada | Gatilho |
|-------|-------------|---------|------------|----------------|---------|
| Scrapers bloqueados por anti-bot das companhias aéreas | Alto | Alto | Mitigar | Implementar rate limiting (delay entre requisições), rotacionar User-Agents, usar cache agressivo (30min TTL) para reduzir chamadas | Taxa de erro dos scrapers > 70% por mais de 2 execuções consecutivas do Hangfire |
| Browser Puppeteer crash no ScraperDecolar | Médio | Alto | Mitigar | Health check a cada 5 minutos (LOW-02). Se browser falhar, reiniciar automaticamente. Se falhar 3x seguidas, desabilitar temporariamente scraper Decolar | `PagesAsync()` lança exceção durante health check |
| Banco SQLite corrompido por falta de espaço em disco | Baixo | Alto | Mitigar | Backup automático do arquivo `redcode.db` a cada 6 horas. Monitorar espaço em disco. Documentar procedimento de restore | Espaço em disco < 100MB OU exceção `SqliteException` com erro de I/O |
| Hangfire MemoryStorage perder jobs no restart da aplicação | Alto | Médio | Aceitar | Documentar que jobs são reiniciados no startup. Usar SQLite storage para produção futura (DT-03) | Log de startup mostra `RecurringJob.AddOrUpdate` sem erros |
| Cache inconsistente entre instâncias sem Redis | Médio | Baixo | Aceitar | Documentar que sem Redis cada instância tem cache independente. Primeira requisição sempre sofre cache miss em cold start | Usuário reporta resultados diferentes entre requisições consecutivas em menos de 30min |
| Timeout dos scrapers acumulando e bloqueando threads | Médio | Médio | Mitigar | Timeout global de 30s via `CancellationToken`. Cada scraper tem seu próprio `HttpClient` com timeout de 30s. Se 4 scrapers falharem, fallback para mock | Tempo de resposta do endpoint `/api/voos/busca` > 30 segundos |

---

## 2. Métrica de Fluxo (DORA)

### Deployment Frequency

| Campo | Valor |
|-------|-------|
| **Nome da Métrica:** | Deployment Frequency (Frequência de Deploy) |
| **O que Mede:** | Quantas vezes o código é implantado em produção por semana |
| **Fórmula:** | `COUNT(deploys) / 7 dias` |
| **Fonte de Dados:** | Log de `git push` para branch `main` + `dotnet run` no servidor |
| **Frequência de Coleta:** | Semanal (toda segunda-feira) |
| **Limites de Saúde:** | Elite: ≥7/semana; Alto: 1-6/semana; Médio: 1/mês; Baixo: <1/mês |
| **Ação se Violado:** | Se < 1 deploy por sprint (2 semanas), revisar pipeline de CI/CD e automatizar deploy com GitHub Actions |

### Lead Time for Changes

| Campo | Valor |
|-------|-------|
| **Nome da Métrica:** | Lead Time for Changes (Tempo até Produção) |
| **O que Mede:** | Tempo entre o commit e o deploy em produção |
| **Fórmula:** | `AVG(data_deploy - data_commit)` por sprint |
| **Fonte de Dados:** | `git log --format="%ci"` + data de `dotnet run` |
| **Frequência de Coleta:** | A cada deploy |
| **Limites de Saúde:** | Elite: <1 hora; Alto: 1 dia; Médio: 1 semana; Baixo: >1 semana |
| **Ação se Violado:** | Lead time > 1 dia = automatizar build/test/deploy. Lead time > 1 semana = reunião de post-mortem para identificar gargalo |

### Change Failure Rate

| Campo | Valor |
|-------|-------|
| **Nome da Métrica:** | Change Failure Rate (Taxa de Falha em Mudanças) |
| **O que Mede:** | Porcentagem de deploys que resultam em falha (rollback, hotfix, incidente) |
| **Fórmula:** | `COUNT(deploys_com_falha) / COUNT(total_deploys) * 100` |
| **Fonte de Dados:** | Log de erros em produção (`ILogger` nível Error) pós-deploy |
| **Frequência de Coleta:** | A cada sprint (2 semanas) |
| **Limites de Saúde:** | Elite: 0-15%; Alto: 16-30%; Médio: 31-45%; Baixo: >45% |
| **Ação se Violado:** | CFR > 30% = congelar deploys, revisar suite de testes, adicionar smoke tests antes do deploy |

---

## 3. Métrica de Qualidade

### Test Coverage (Cobertura de Testes)

| Campo | Valor |
|-------|-------|
| **Nome da Métrica:** | Test Coverage — Cobertura de Código por Testes |
| **O que Mede:** | Porcentagem de linhas de código cobertas por testes automatizados |
| **Fórmula:** | `(linhas_executadas_por_testes / linhas_totais) * 100` |
| **Fonte de Dados:** | `dotnet test --collect:"Code Coverage"` + ReportGenerator |
| **Frequência de Coleta:** | A cada sprint (quinzenal) |
| **Limites de Saúde:** | Bom: ≥70%; Aceitável: 50-69%; Crítico: <50% |
| **Ação se Violado:** | Cobertura < 50% = parar desenvolvimento de features, dedicar sprint a testes automatizados |

### Test Success Rate

| Campo | Valor |
|-------|-------|
| **Nome da Métrica:** | Test Success Rate (Taxa de Sucesso dos Testes) |
| **O que Mede:** | Porcentagem de execuções da suite de testes que passam completamente |
| **Fórmula:** | `COUNT(runs_com_0_falhas) / COUNT(total_runs) * 100` |
| **Fonte de Dados:** | Output do `dotnet test` (27/27 passando = 100%) |
| **Frequência de Coleta:** | A cada commit (automático via `npm run test`) |
| **Limites de Saúde:** | Saudável: 100%; Degradado: 90-99%; Crítico: <90% |
| **Ação se Violado:** | Qualquer falha em teste bloqueia merge. Se < 100%, não deployar até corrigir |

---

## 4. SLO — Service Level Objective

### Rota Crítica: `GET /api/voos/busca`

| Campo | Valor |
|-------|-------|
| **SLI (Indicador):** | Disponibilidade — porcentagem de requisições que retornam HTTP 2xx em menos de 30 segundos |
| **Fórmula de Coleta:** | `COUNT(respostas_2xx_em_<30s) / COUNT(total_requisicoes) * 100` |
| **Fonte do Dado:** | Middleware de logging (`ILogger` no endpoint de busca — logs Information com duração) |
| **Janela de Medição:** | 7 dias (rolling window) |
| **Alvo (SLO):** | **99.5%** das requisições devem ser bem-sucedidas em < 30s |
| **Janela:** | 7 dias |

---

## 5. Error Budget Policy

Com SLO de 99.5% em 7 dias, o **Error Budget** é de **0.5%** — aproximadamente **50 minutos de indisponibilidade** por semana.

### Nível 1 — Budget Verde (> 0.3% restante)
- **Ação:** Operação normal. Novas features podem ser implantadas.
- **Monitoramento:** Logs de erro e latência são coletados mas não geram alerta.

### Nível 2 — Budget Amarelo (0.1% a 0.3% restante)
- **Ação:** Alerta para o time de desenvolvimento.
- **Restrição:** Apenas correções de bugs e melhorias de confiabilidade são permitidas.
- **Novas features:** Bloqueadas até que o error budget volte ao Nível 1.
- **Post-mortem:** Agendar revisão das falhas para identificar causa raiz.

### Nível 3 — Budget Vermelho (< 0.1% restante / Budget esgotado)
- **Ação:** **Feature Freeze total.** Nenhuma nova funcionalidade é implantada.
- **Congelamento:** Todo o time foca exclusivamente em correções de confiabilidade.
- **Zero novas funcionalidades:** Até que o error budget seja restaurado (nova janela de 7 dias).
- **Escalação:** Notificar todos os stakeholders. Iniciar war room para debugging.
- **Retrospectiva:** Ao final da janela, realizar post-mortem completo com 5 Whys.

---

## Resumo de Métricas

| Métrica | Tipo | Alvo | Status |
|---------|------|------|--------|
| Deployment Frequency | DORA — Fluxo | ≥1/sprint | ✅ |
| Lead Time for Changes | DORA — Fluxo | <1 dia | ✅ |
| Change Failure Rate | DORA — Qualidade | <30% | ✅ |
| Test Coverage | Qualidade | ≥70% | ⚠️ ~60% |
| Test Success Rate | Qualidade | 100% | ✅ 27/27 |
| SLO — Disponibilidade | SLO | 99.5% (7d) | ✅ |
