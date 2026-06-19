# CLAUDE.md — Regras do Projeto

## Princípio Fundamental: Zero Ambiguidade para Implementação

Toda especificação, tarefa ou instrução neste projeto **deve eliminar ambiguidades**. Decisões não explicitadas serão interpretadas como **proibidas**. O padrão é "não fazer" até que esteja claro o que deve ser feito.

---

## 1. Especificações

### 1.1 Estrutura Obrigatória de Qualquer Especificação
Antes de implementar qualquer funcionalidade, a especificação correspondente **deve** conter:

| Campo | Obrigatório | Descrição |
|-------|-------------|-----------|
| `Propósito` | Sim | Por que esta funcionalidade existe |
| `Escopo` | Sim | O que está dentro e **fora** do escopo |
| `Entradas` | Sim | Formatos, tipos, intervalos válidos, valores aceitos |
| `Saídas` | Sim | Formatos, tipos, estruturas de retorno |
| `Regras de Negócio` | Sim | Lista exaustiva de condições e validações |
| `Fluxo Principal` | Sim | Passo a passo sem lacunas |
| `Fluxos Alternativos` | Sim | Erro, exceção, edge case — todo caminho possível |
| `Critérios de Aceite` | Sim | condições mensuráveis para considerar "pronto" |

### 1.2 Proibido
- Termos vagos: "em breve", "futuramente", "talvez", "eventualmente"
- Suposições não documentadas
- Comportamento indefinido sem fallback explícito
- Dependências implícitas (toda dependência deve ser declarada)

---

## 2. Código

### 2.1 Estilo e Estrutura
- **Imutabilidade por padrão**: Preferir `readonly`, `const`, records imutáveis
- **Null safety explícito**: Todo parâmetro e retorno deve declarar se aceita `null` (uso de nullable annotations)
- **Validação na fronteira**: Validar entradas na camada mais externa (Controller/Endpoint) — camadas internas assumem dados válidos
- **Tratamento de erros**: Nunca engolir exceções. Toda exceção deve ser registrada (log) e tratada em nível adequado
- **Testes**: Toda função com lógica condicional (if/switch/loop) **deve** ter teste unitário cobrindo todos os branches

### 2.2 Nomenclatura
| Artefato | Convenção | Exemplo |
|----------|-----------|---------|
| Classes/Records | PascalCase | `ResultadoBusca` |
| Interfaces | IPascalCase | `IVooScraper` |
| Métodos | PascalCase | `BuscarVoosAsync` |
| Parâmetros | camelCase | `dataPartida` |
| Variáveis locais | camelCase | `resultado` |
| Constantes | SCREAMING_SNAKE_CASE | `TIMEOUT_PADRAO` |
| Arquivos | PascalCase (classe) / kebab-case (config) | `VooScraper.cs` / `appsettings.json` |

### 2.3 Documentação no Código
- Toda interface pública deve ter `/// <summary>` XML doc
- Toda classe com constructor injection deve ter os parâmetros documentados com `<param>`
- Comentários `// TODO:` são **proibidos em produção** — use issues/ADR em vez disso

### 2.4 Imports e Usings
- Usings devem ser explícitos (não implícitos)
- Ordem: System > NuGet > Projeto
- Remover usings não utilizados

---

## 3. Commits e Versionamento

### 3.1 Mensagens de Commit
Formato obrigatório:

```
<tipo>(<escopo>): <descrição imperativa>

- <detalhe 1>
- <detalhe 2>
```

Tipos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`

### 3.2 Branch Naming
`<tipo>/<descrição-curta>` — ex.: `feat/scraper-latam`, `fix/cache-timeout`

---

## 4. ADRs (Architecture Decision Records)

Toda decisão arquitetural **deve** ser registrada em [`docs/pivotagem/ADR-*.md`](docs/pivotagem/).

Template mínimo:
```markdown
# ADR-NNN: Título

## Status
[Proposto | Aceito | Rejeitado | Deprecado]

## Contexto
Problema que motivou a decisão.

## Decisão
O que foi decidido e por quê.

## Consequências
Impactos positivos e negativos.
```

---

## 5. Fluxo de Implementação (Zero Ambiguidade)

1. **Ler** a especificação completa (docs/requisitos, ADRs, SPECS)
2. **Identificar** ambiguidades — se houver, **perguntar** antes de implementar
3. **Declarar** dependências e assumptions por escrito
4. **Implementar** seguindo exatamente a especificação
5. **Testar** cobrindo todos os cenários (feliz, triste, exceção)
6. **Revisar** se o código reflete fielmente a especificação
7. **Entregar** apenas quando critérios de aceite forem 100% atendidos

### Regra de Ouro
> **Se não está escrito, não está especificado. Se não está especificado, não implemente.**

---

## 6. Stack do Projeto

- **Backend**: .NET 10, C#, Minimal APIs, Dapper, SQLite
- **Frontend**: .NET Blazor (WebAssembly), C#
- **Scraping**: Selenium WebDriver (headless)
- **Cache**: StackExchange.Redis (Redis) + IMemoryCache fallback
- **Background Jobs**: Hangfire
- **Testes**: xUnit
- **Infra**: Docker (opcional), IIS, Windows Server
