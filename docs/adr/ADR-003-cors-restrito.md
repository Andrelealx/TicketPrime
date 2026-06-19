# ADR-003: Restrição de CORS — AllowAnyOrigin → WithOrigins

## Status

✅ **Aceito** — 2026-06-09

## Contexto

A política CORS original usava `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()`, permitindo requisições de qualquer origem. Isso apresenta riscos de segurança e é incompatível com `AllowCredentials()` caso o projeto precise de autenticação no futuro.

**Issue relacionada**: [CORRECAO.md](../CORRECAO.md) — LOW-13.

## Decisão

Restringir CORS para a origem específica do frontend Blazor WebAssembly em ambiente de desenvolvimento:

```csharp
// Antes:
policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();

// Depois:
policy.WithOrigins("http://localhost:5139")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

## Consequências

### Prós

1. **Segurança**: Apenas o frontend autorizado pode acessar a API
2. **Preparação para produção**: Política explícita, fácil de estender com origens adicionais via configuração
3. **Compatibilidade futura**: Permite adicionar `AllowCredentials()` sem conflitos

### Contras

1. Em ambientes diferentes de desenvolvimento, a origem precisa ser ajustada
2. Se o frontend mudar de porta, o CORS precisa ser atualizado

### Mitigações

- Origem configurável via `appsettings.json` (a ser implementado se necessário)
- Ambiente de produção deve usar lista explícita de origens

---

## Referências

- [`src/RedCodeApi/Program.cs`](../src/RedCodeApi/Program.cs) — Configuração CORS
- [Microsoft Docs: CORS with named policies](https://learn.microsoft.com/en-us/aspnet/core/security/cors)
