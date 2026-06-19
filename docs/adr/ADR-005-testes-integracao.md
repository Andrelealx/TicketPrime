# ADR-005: Testes de Integração — WebApplicationFactory

## Status

✅ **Aceito** — 2026-06-09

## Contexto

O projeto tinha apenas testes unitários (21 testes no `NormalizadorDados` e validações). Faltavam testes de integração que verificassem os endpoints da API de ponta a ponta, conforme planejado na SPEC-032.4.

## Decisão

Adicionar testes de integração usando `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`), que permite testar a API completa em memória sem precisar iniciar um servidor real.

### Testes implementados

| Teste | Endpoint | Verifica |
|-------|----------|----------|
| `GET_Aeroportos_DeveRetornar200` | `/api/aeroportos` | 200 OK com lista |
| `GET_AeroportosBusca_DeveFiltrarPorTermo` | `/api/aeroportos/busca?q=GRU` | Filtro funciona |
| `GET_Companhias_DeveRetornar200` | `/api/companhias` | 200 OK com lista |
| `GET_RotasPopulares_DeveRetornar200` | `/api/rotas/populares` | 200 OK com rotas |
| `GET_BuscaVoos_ParametrosInvalidos_DeveRetornar400` | `/api/voos/busca` | Validação de entrada |
| `POST_Alertas_EmailInvalido_DeveRetornar400` | `/api/alertas` | Validação de email |

### Estratégia de teste

- Usa SQLite em memória (`Data Source=:memory:`) para isolar testes
- `WebApplicationFactory` com configuração customizada substitui o banco real
- Cada teste é independente e não depende de estado externo

## Consequências

### Prós

1. Validação real do pipeline HTTP (middleware, CORS, serialização)
2. Testes rápidos (banco em memória, sem rede)
3. Cobertura de endpoints críticos

### Contras

1. Não testa scrapers reais (requerem rede externa)
2. Configuração adicional no `.csproj` de testes (`Microsoft.AspNetCore.Mvc.Testing`)

---

## Referências

- [`tests/IntegrationTests.cs`](../tests/IntegrationTests.cs) — Testes implementados
- [`docs/SPECS-FLYCOMPARE.md`](../docs/SPECS-FLYCOMPARE.md) — SPEC-032.4
