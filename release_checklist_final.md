# Release Checklist Final — FlyCompare v2.0.0

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0
> **Status:** ✅ Todos os itens verificados

---

## [x] Fundamentos

- [x] Estrutura de pastas correta: `/docs`, `/db`, `/src`, `/tests`
- [x] `README.md` executável com comandos `npm run dev`
- [x] Script SQL em `/db` com `CREATE TABLE` para tabelas FlyCompare
- [x] API Minimal em `/src` com endpoints mapeados
- [x] Dapper com parâmetros `@Param` — sem concatenação SQL
- [x] Projeto de testes em `/tests` com xUnit
- [x] Todos os testes possuem `Assert` válido
- [x] Build: 0 erros, 0 warnings
- [x] Testes: 27/27 aprovados

---

## [x] Produto Mínimo

- [x] `GET /api/aeroportos` — Listar aeroportos
- [x] `GET /api/aeroportos/busca?q=` — Autocomplete
- [x] `GET /api/companhias` — Listar companhias
- [x] `GET /api/rotas/populares` — Rotas populares
- [x] `GET /api/voos/busca?origem=&destino=&dataPartida=` — Buscar voos
- [x] `GET /api/voos/precos/{vooId}` — Histórico de preços
- [x] `POST /api/alertas` — Criar alerta
- [x] `GET /api/alertas/{email}` — Listar alertas
- [x] `POST /api/voos/analise` — Motor de regras + Score
- [x] Frontend Blazor WASM com 4 páginas (Home, Buscar, Resultados, Alertas)

---

## [x] Evidência de Qualidade

- [x] 21 testes unitários (NormalizadorDados, IATA, DTOs)
- [x] 6 testes de integração (`WebApplicationFactory`)
- [x] Testes com nomes no padrão `Metodo_Cenario_ResultadoEsperado`
- [x] Pelo menos 3 testes com comentários AAA (Arrange, Act, Assert)
- [x] CORS restrito a `http://localhost:5139`
- [x] Validação de input em todos os endpoints (fail-fast com 400)
- [x] Tratamento de erros com try-catch em scrapers e jobs

---

## [x] Decisões Documentadas

- [x] 5 ADRs em `/docs/adr/` com `# Contexto`, `# Decisão`, `# Consequências`
- [x] ADRs com `Status:` (Aceito/Proposto) e `Prós/Contras`
- [x] `/docs/analise_arquitetura.md` — 3 cenários + 5 violações
- [x] `/docs/registro_divida_tecnica.md` — 8 dívidas com Prioridade 1, 2 e 3
- [x] `/docs/fluxo_manutencao.md` — 12 tickets classificados (Swanson)
- [x] `/docs/plano_iteracao.md` — Objetivo, escopo, entregáveis, WIP, DoD
- [x] `/docs/operacao.md` — 6 riscos, métricas DORA, SLO 99.5%, Error Budget
- [x] `/docs/seguranca_ciclo.md` — Threat model + 3 gates de segurança
- [x] `/docs/topologia_times.md` — Stream-aligned, Platform, Enabling, Complicated-Subsystem

---

## [x] Evidência de Requisitos

- [x] Histórias de usuário no formato `Como... Quero... Para...`
- [x] Critérios BDD no formato `Dado... Quando... Então...`
- [x] Regras R1-R4 implementadas nos endpoints
- [x] Endpoint com JOIN (INNER JOIN entre Rotas e Aeroportos)
- [x] Pelo menos 3 validações antes de INSERT/UPDATE
- [x] 2 novos endpoints/features: SPEC-034 (Motor de Regras) e LOW-01 (Email)

---

## [x] Governança

- [x] Pipeline de liberação segura documentado
- [x] Análise de impacto antes de cada deploy
- [x] Feature toggles para funcionalidades experimentais
- [x] Estratégia de release e rollback
- [x] Matriz de riscos com gatilhos observáveis
- [x] Métricas operacionais com fórmula e ação se violado

---

## [x] Segurança

- [x] Nenhuma credencial hardcoded em arquivos `.cs` (SSDF)
- [x] Connection string via `builder.Configuration`
- [x] Senha SMTP via configuração (não hardcoded)
- [x] Queries Dapper com `@Param` — zero SQL Injection
- [x] CORS com origens específicas
- [x] Validação de input em todos os endpoints
- [x] Threat model documentado
- [x] 3 gates de segurança (Dev, Build, Pre-Deploy)

---

## Resumo

| Categoria | Status |
|-----------|--------|
| Fundamentos | ✅ 9/9 |
| Produto Mínimo | ✅ 10/10 |
| Evidência de Qualidade | ✅ 6/6 |
| Decisões Documentadas | ✅ 9/9 |
| Evidência de Requisitos | ✅ 6/6 |
| Governança | ✅ 6/6 |
| Segurança | ✅ 8/8 |
| **Total** | **✅ 54/54** |
