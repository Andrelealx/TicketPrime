# Correção AV2 — Red-code (FlyCompare)

**Grupo:** André, João Lucas, Miguel, Pedro, Vinicius

| # | Item de Avaliação | Nota | Justificativa |
|---|-------------------|:----:|---------------|
| 01 | Padrão AAA nos Testes | 0,5 | Vários métodos com `// Arrange`, `// Act`, `// Assert` em `UnitTest1.cs` e `IntegrationTests.cs`; 27 testes no total |
| 02 | Nomenclatura e Independência | 0,5 | Padrão `Metodo_Cenario_ResultadoEsperado` (ex: `Normalizar_Duplicatas_DeveManterApenasOMaisBarato`); zero condicionais |
| 03 | Padrões Arquiteturais | 0,5 | 3 cenários (Strategy, Cache-Aside, Pipes and Filters) com `Positivo:`/`Negativo:` |
| 04 | Violações Arquiteturais | 0,5 | 5 violações reais com `**Problema:**`, `**Evidência:**` (trechos de código), `**Impacto:**`, `**Ação Recomendada:**` |
| 05 | ADR | 0,0 | 5 ADRs em `/docs/adr/` com Contexto, Decisão, Consequências, Status, Prós/Contras — porém a pasta deveria chamar-se `/docs/adrs/` (plural) conforme exigido |
| 06 | Dívida Técnica | 0,5 | 8 dívidas com colunas: ID, Descrição Técnica, Freq. Alteração, Risco, Esforço, Decisão |
| 07 | Priorização Dívida | 0,5 | P1 (DT-03 acoplamento SQLite, DT-08 testes sem AAA), P2 (4 dívidas), P3 (DT-06 duplicação modelo, DT-07 modelos aninhados) |
| 08 | Classificação Manutenção | 0,5 | 12 tickets: 3 Corretiva, 2 Adaptativa, 3 Perfectiva, 4 Preventiva (Swanson) |
| 09 | Pipeline de Liberação | 0,5 | 4 passos: Análise de Impacto, Teste Cirúrgico, Feature Toggle, Estratégia de Release (com rollback documentado) |
| 10 | Plano de Iteração | 0,5 | Objetivo, Escopo, Entregáveis (15 itens), Risco Principal, DoD (5 critérios) |
| 11 | Quadro Kanban e WIP | 0,5 | 4 colunas (Backlog, Em Desenvolvimento, Code Review, Concluído); WIP = 3 ≤ 5 integrantes |
| 12 | Matriz de Riscos | 0,5 | 6 riscos com colunas: Risco, Probabilidade, Impacto, Estratégia, Ação Planejada |
| 13 | Gatilhos de Risco | 0,5 | Todos os 6 gatilhos com ≥20 caracteres descrevendo evento observável concreto |
| 14 | Métrica DORA | 0,5 | "Deployment Frequency" com 7 campos completos (Nome, O que Mede, Fórmula, Fonte, Frequência, Limites, Ação se Violado) |
| 15 | Métrica de Qualidade | 0,5 | "Test Coverage" com 7 campos completos (+ "Test Success Rate" como bônus) |
| 16 | SLO | 0,5 | SLI definido, Fórmula de Coleta, Fonte (ILogger), Janela (7 dias), Alvo (99,5%) para `GET /api/voos/busca` |
| 17 | Error Budget Policy | 0,5 | 3 níveis graduados: N1 (verde, normal), N2 (amarelo, novas features bloqueadas), N3 (vermelho, **"Feature Freeze total"** + **"Zero novas funcionalidades"** + **"Congelamento"**) |
| 18 | Segurança SSDF | 0,5 | Nenhuma credencial hardcoded nos 36 arquivos `.cs`; connection string via `builder.Configuration` |
| 19 | Threat Model e Gates | 0,5 | Ativos Protegidos (5), Vetor de Ataque (SQL Injection), Falha Arquitetural, Mitigação (5 controles) + 3 Gates com checklists detalhados |
| 20 | Topologia Times e DoD | 0,5 | 4 tipos Team Topologies (Stream-Aligned, Platform, Enabling, Complicated-Subsystem) + diagrama de interação + `release_checklist_final.md` com 7 `[x]` |

**Nota Final: 9,5 / 10,0**

---

**Observações:**
- Trabalho de altíssima qualidade. A documentação é contextualizada ao projeto real (FlyCompare, metabuscador de passagens aéreas), não genérica.
- Única falha: a pasta de ADRs chama-se `docs/adr/` (singular) em vez de `docs/adrs/` (plural) como exigido pela rubrica. O conteúdo dos 5 ADRs é excelente.
- Destaque para o ADR-001 (8 páginas) documentando a pivotagem do sistema de eventos para metabuscador de passagens aéreas com análise detalhada de trade-offs.
- O release_checklist cobre 54 itens verificáveis em 7 categorias — muito além do mínimo exigido.
- A equipe documentou honestamente suas próprias fragilidades (ex: DT-08 admite que nem todos os testes seguem AAA, apesar de na prática seguirem).
