# Plano de Iteração — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Sprint:** AV2 — Qualidade, Arquitetura e Operação
> **Data:** 2026-06-18
> **Duração:** 2 semanas

---

## Objetivo da Iteração

**Evoluir o FlyCompare com foco em engenharia de software: qualidade, arquitetura, documentação, segurança e operação.** Consolidar o sistema como um produto de software bem projetado, não apenas implementado.

---

## Escopo (Backlog Selecionado)

| ID | Tipo | Descrição | Prioridade |
|----|------|-----------|------------|
| LOW-01 | Feature | Implementar envio de email SMTP para notificação de alertas | Alta |
| LOW-02 | Fix | Health check no browser Puppeteer do ScraperDecolar | Alta |
| LOW-03 | Fix | Unificar campos entre ResultadoBusca API e Frontend | Média |
| LOW-04 | Fix | Corrigir navegação antecipada em BuscarVoos | Média |
| LOW-05 | Fix | Remover campo dataVolta sem utilidade | Baixa |
| LOW-06 | Fix | Substituir fire-and-forget Task.Run em VoosEndpoints | Alta |
| LOW-08 | Feature | Completar SPEC-033 — Layout final (breadcrumbs, footer, responsividade) | Média |
| AV2-01 | Doc | Criar /docs/analise_arquitetura.md | Alta |
| AV2-02 | Doc | Criar /docs/registro_divida_tecnica.md | Alta |
| AV2-03 | Doc | Criar /docs/fluxo_manutencao.md | Alta |
| AV2-04 | Doc | Criar /docs/plano_iteracao.md | Alta |
| AV2-05 | Doc | Criar /docs/operacao.md (riscos, métricas, SLO) | Alta |
| AV2-06 | Doc | Criar /docs/seguranca_ciclo.md | Alta |
| AV2-07 | Doc | Criar /docs/topologia_times.md | Média |
| AV2-08 | Doc | Criar release_checklist_final.md | Alta |
| AV2-09 | Test | Atualizar testes com padrão AAA e nomes padronizados | Média |

---

## Entregáveis (Evidências)

| Entregável | Tipo | Evidência |
|-----------|------|-----------|
| EmailService.cs | Código | `src/RedCodeApi/Services/EmailService.cs` |
| ScraperDecolar health check | Código | `src/RedCodeApi/Services/Scrapers/ScraperDecolar.cs` |
| ResultadoBusca unificado | Código | `src/RedCodeFront/Models/FlyCompare/ResultadoBusca.cs` |
| BuscarVoos corrigido | Código | `src/RedCodeFront/Pages/BuscarVoos.razor` |
| VoosEndpoints refatorado | Código | `src/RedCodeApi/Endpoints/VoosEndpoints.cs` |
| MainLayout + CSS | Código | `src/RedCodeFront/Shared/MainLayout.razor`, `wwwroot/css/app.css` |
| Análise arquitetural | Documento | `docs/analise_arquitetura.md` |
| Dívida técnica | Documento | `docs/registro_divida_tecnica.md` |
| Fluxo de manutenção | Documento | `docs/fluxo_manutencao.md` |
| Plano de iteração | Documento | `docs/plano_iteracao.md` |
| Operação (riscos, métricas, SLO) | Documento | `docs/operacao.md` |
| Segurança | Documento | `docs/seguranca_ciclo.md` |
| Topologia de times | Documento | `docs/topologia_times.md` |
| Release checklist | Documento | `release_checklist_final.md` |
| Testes AAA | Código | `tests/UnitTest1.cs`, `tests/IntegrationTests.cs` |

---

## Risco Principal do Ciclo

**Risco:** Quebra de compatibilidade nos testes após refatoração do `VoosEndpoints` (extração do método `PersistirHistoricoPrecosAsync`).

**Mitigação:** Executar `dotnet test` após cada alteração. Rollback via `git revert` se necessário. A refatoração é interna — a API pública dos endpoints não muda.

---

## Definição de Pronto (DoD)

Uma tarefa é considerada **Done** quando:

1. ✅ Código compila sem erros e warnings (`dotnet build`)
2. ✅ Todos os 27 testes passam (`dotnet test` — 21 unitários + 6 integração)
3. ✅ Documentação atualizada com o que foi feito
4. ✅ Código revisado (self-review com checklist CORRECAO.md)
5. ✅ Evidência registrada (arquivo, commit, ou resposta de endpoint)

---

## Quadro Visual e WIP

| Backlog | Em Desenvolvimento | Code Review | Concluído |
|---------|-------------------|-------------|-----------|
| LOW-07 | | | LOW-01 ✅ |
| LOW-09 | | | LOW-02 ✅ |
| LOW-10 | | | LOW-03 ✅ |
| AV2-07 | | | LOW-04 ✅ |
| AV2-09 | | | LOW-05 ✅ |
| | | | LOW-06 ✅ |
| | | | LOW-08 ✅ |
| | | | AV2-01 ✅ |
| | | | AV2-02 ✅ |
| | | | AV2-03 ✅ |
| | | | AV2-04 ✅ |

**WIP máximo: 3 tarefas** (grupo de 5 integrantes)

---

## Métricas da Iteração

| Métrica | Valor |
|---------|-------|
| Total de tarefas | 17 |
| Concluídas | 11 |
| Em andamento | 0 |
| Pendentes | 6 |
| Taxa de conclusão | 65% |
| Build | ✅ 0 erros, 0 warnings |
| Testes | ✅ 27/27 |
