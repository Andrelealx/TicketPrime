# ADR-002: Refatoração do Program.cs — Separação de Responsabilidades

## Status

✅ **Aceito** — 2026-06-09

## Contexto

O arquivo `src/RedCodeApi/Program.cs` acumulava **577 linhas** com múltiplas responsabilidades em um único arquivo monolítico:

- Registro de serviços no contêiner DI (CORS, cache, scrapers, Hangfire)
- Inicialização do banco de dados (CREATE TABLE, seeds)
- 8 endpoints da API (aeroportos, companhias, rotas, voos, alertas, preços)
- Método auxiliar `GerarMockVoos()`

Isso violava o princípio de responsabilidade única (SRP) e dificultava manutenção, testes e onboarding de novos desenvolvedores.

## Decisão

Separar `Program.cs` em arquivos especializados, cada um com uma única responsabilidade:

| Arquivo | Responsabilidade |
|---------|-----------------|
| `Program.cs` | Configuração mínima: builder, DI, middleware pipeline |
| `Endpoints/AeroportosEndpoints.cs` | `GET /api/aeroportos`, `GET /api/aeroportos/busca` |
| `Endpoints/CompanhiasEndpoints.cs` | `GET /api/companhias` |
| `Endpoints/RotasEndpoints.cs` | `GET /api/rotas/populares` |
| `Endpoints/VoosEndpoints.cs` | `GET /api/voos/busca`, `GET /api/voos/precos/{vooId}` |
| `Endpoints/AlertasEndpoints.cs` | `POST /api/alertas`, `GET /api/alertas/{email}` |
| `Data/DbInitializer.cs` | Criação de tabelas e seed data |
| `Data/MockVoosGenerator.cs` | Geração de dados mockados para fallback |

**Padrão usado**: Extension methods sobre `WebApplication` para mapear endpoints, permitindo que `Program.cs` apenas chame métodos de alto nível.

```csharp
// Program.cs (após refatoração)
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureServices();
var app = builder.Build();
app.ConfigureMiddleware();
DbInitializer.Initialize(app);
app.MapFlyCompareEndpoints();
app.Run();
```

## Consequências

### Prós

1. **Legibilidade**: `Program.cs` reduzido de 577 para ~50 linhas — visão clara do pipeline
2. **Testabilidade**: Endpoints isolados podem ser testados mais facilmente
3. **Manutenção**: Alterar um endpoint não requer navegar por centenas de linhas
4. **Onboarding**: Novos desenvolvedores entendem a estrutura em minutos
5. **Navegação**: Arquivos pequenos com nome claro → IDE e grep mais eficientes

### Contras

1. Mais arquivos no projeto (aumenta de 1 para 8 arquivos na API)
2. Requer disciplina para manter a separação ao adicionar novos endpoints
3. Métodos auxiliares antes internos agora são `internal` ou `public`

### Mitigações

- Estrutura de pastas clara: `Endpoints/`, `Data/`
- Convenção de nomenclatura consistente: `XxxEndpoints.cs`, `MapXxxEndpoints()`
- Cada arquivo de endpoint é autocontido (não depende de outros endpoints)

---

## Referências

- [`src/RedCodeApi/Program.cs`](../src/RedCodeApi/Program.cs) — Arquivo refatorado
- [`src/RedCodeApi/Endpoints/`](../src/RedCodeApi/Endpoints/) — Endpoints extraídos
- [`src/RedCodeApi/Data/`](../src/RedCodeApi/Data/) — Inicialização do banco
