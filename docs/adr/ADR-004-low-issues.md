# ADR-004: Correção de LOW Issues — NormalizadorDados + Segurança

## Status

✅ **Aceito** — 2026-06-09

## Contexto

O documento `CORRECAO.md` identificou 15 issues de baixa prioridade pendentes. As mais impactantes foram corrigidas:

### LOW-02: Deduplicação usava apenas `CodigoVoo` como chave
Companhias diferentes podem ter códigos de voo iguais (ex: LATAM e GOL podem usar "LA1234" e "G31234", mas em teoria poderiam colidir). A dedup removia resultados válidos de companhias distintas.

### LOW-13: CORS AllowAnyOrigin (já corrigido — ADR-003)

## Decisão

### 1. NormalizadorDados — Chave de deduplicação composta

```csharp
// Antes: apenas CodigoVoo
var chave = voo.CodigoVoo;

// Depois: CodigoVoo + Companhia
var chave = $"{voo.CodigoVoo}|{voo.Companhia}";
```

Isso garante que voos com mesmo código mas de companhias diferentes sejam tratados como entradas distintas.

### 2. LOW Issues restantes

| ID | Issue | Decisão |
|----|-------|---------|
| LOW-01/11 | Senha hardcoded | Manter para dev local (SQLite não usa senha). Documentado como dívida técnica. |
| LOW-03 | Mock `GetHashCode()` não determinístico | Baixo impacto — mock é fallback. Sem alteração. |
| LOW-04 | Scrapers Scoped com Hangfire | Comportamento esperado. Hangfire gerencia seus próprios scopes. |
| LOW-05 | Scrapers com regex frágil | Sites mudam constantemente. Mock fallback cobre falhas. Sem alteração. |
| LOW-06 | Alerta sem envio de email | Adicionado log explícito `[ALERTA DISPARADO]` para clareza. Email real requer SMTP config. |
| LOW-07 | `BuscaRequest.DataVolta` não utilizado | Campo mantido para compatibilidade futura com busca ida+volta. |
| LOW-08 | `Alerta.razor` com string hardcoded | Baixo impacto — componente simples. Sem alteração. |
| LOW-09 | Duas definições de `ResultadoBusca` | API e Frontend têm necessidades diferentes. Arquitetura intencional. |
| LOW-10 | ScrapingScheduler sem CancellationToken | Baixo impacto — jobs Hangfire raramente são cancelados. |
| LOW-14 | ScraperDecolar sem health check | Puppeteer gerencia seu próprio ciclo de vida. |
| LOW-15 | Navegação antecipada em BuscarVoos | Comportamento intencional para UX fluida. |
| LOW-16 | Campo dataVolta sem utilidade | Relacionado a LOW-07. |

## Consequências

### Prós

1. Deduplicação mais precisa — não perde resultados de companhias diferentes
2. Logs de alerta mais claros para debugging
3. Documentação de dívida técnica explícita para itens não corrigidos

### Contras

1. Chave de dedup ligeiramente mais longa (string composta vs string simples)

---

## Referências

- [`src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs`](../src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs)
- [`CORRECAO.md`](../CORRECAO.md) — Lista completa de issues
