# ADR-001: Arquitetura do FlyCompare — Metabuscador de Passagens Aéreas

## Status

✅ **Aceito** — 2026-05-14

## Contexto

O projeto **RedCode** (sistema de bilheteria de eventos) está sendo pivotado para **FlyCompare**, um metabuscador de passagens aéreas. A mudança de domínio exige decisões arquiteturais fundamentais que impactam toda a base de código.

### Fatores que influenciam esta decisão

1. **Domínio completamente novo**: O modelo de dados (voos, aeroportos, companhias, preços) substitui o modelo anterior (eventos, usuários, reservas, cupons)
2. **Fonte de dados externa**: Diferente do RedCode (dados inseridos manualmente no banco), o FlyCompare depende de dados coletados de sites de terceiros via web scraping
3. **Restrição técnica**: O projeto deve manter `.NET` e `Dapper` conforme especificação acadêmica (AV1/AV2)
4. **Tempo de resposta**: Usuários esperam resultados de busca em segundos, mas scraping pode levar de 5 a 30 segundos dependendo da fonte
5. **Custo**: APIs oficiais de passagens (Amadeus, Google Flights, Skyscanner) têm custos por requisição
6. **Manutenibilidade**: O código precisa ser claro o suficiente para que uma IA ou desenvolvedor possa dar continuidade sem documentação extensa
7. **Legalidade**: Web scraping de sites de companhias aéreas pode violar termos de serviço

## Decisão

Adotar uma **arquitetura híbrida de metabusca** com as seguintes características:

### 1. Estratégia de Coleta de Dados

| Fonte | Estratégia | Justificativa |
|---|---|---|
| **Companhias Aéreas (Latam, Gol, Azul)** | Web scraping com `HtmlAgilityPack` + `HttpClient` | APIs públicas geralmente não existem ou exigem parceria comercial |
| **OTAs (Decolar, Kayak)** | Browser automation com `PuppeteerSharp` ou `Playwright` | Sites com JavaScript pesado que exigem renderização |
| **Fallback** | APIs pagas (Amadeus, Skyscanner API) | Se scraping for inviabilizado legal ou tecnicamente |

### 2. Cache em Duas Camadas

```
┌─────────────────────────────────────────────────┐
│             Camada 1: Cache em Memória           │
│  (IMemoryCache) - TTL: 5 min - Resultado da     │
│  última busca do mesmo usuário                  │
├─────────────────────────────────────────────────┤
│             Camada 2: Cache Distribuído          │
│  (Redis) - TTL: 30 min - Resultados consolidados│
│  da mesma rota/data                             │
└─────────────────────────────────────────────────┘
```

### 3. Persistência com Dapper (mantido)

Manter **Dapper** como ORM, consistente com o projeto legado. As razões:

- Performance superior para consultas de leitura intensa (busca de voos)
- Controle total sobre SQL e otimizações
- Código legado já utiliza Dapper, reduzindo retrabalho
- Consultas parametrizadas com `@` protegem contra SQL injection

### 4. Scraping Síncrono (Fases 3-4) → Assíncrono (Fase 5+)

| Fase | Estratégia | Motivo |
|---|---|---|
| **Fase 3-4** | Scraping síncrono na request (com cache) | Simplicidade, prova de conceito rápida, 1-2 fontes |
| **Fase 5+** | Scraping assíncrono com Hangfire + SignalR | Experiência do usuário, escalabilidade, múltiplas fontes |

### 5. Frontend Blazor WASM (mantido)

Manter Blazor WebAssembly como frontend. Adaptar as páginas para o novo domínio:

- Página inicial vira busca de voos (em vez de listagem de eventos)
- Páginas legadas (Eventos, Reservas, Cupons, Usuários) são removidas gradualmente
- Componentes compartilhados (Alerta, MainLayout) são reaproveitados

### 6. Arquitetura de Scrapers com Strategy Pattern

```csharp
public interface IVooScraper {
    string Fonte { get; }
    Task<List<ResultadoBusca>> BuscarVoosAsync(
        string origem, string destino, DateTime dataPartida,
        CancellationToken ct = default
    );
}

// Registro dos scrapers via DI
builder.Services.AddScoped<IVooScraper, ScraperLatam>();
builder.Services.AddScoped<IVooScraper, ScraperGol>();
builder.Services.AddScoped<IVooScraper, ScraperAzul>();
// ...

// Uso no endpoint de busca
var scrapers = context.RequestServices.GetServices<IVooScraper>();
var tasks = scrapers.Select(s => s.BuscarVoosAsync(origem, destino, data));
var resultados = (await Task.WhenAll(tasks)).SelectMany(r => r);
```

## Consequências

### Prós

1. **Aproveitamento do código existente**: Dapper, SQL Server, Blazor WASM e estrutura de projetos são reutilizados
2. **Custo zero de dados**: Scraping não tem custo por requisição (vs. APIs pagas que cobram US$ 0.01-0.05 por chamada)
3. **Flexibilidade de fontes**: Novas companhias podem ser adicionadas criando uma nova classe que implementa `IVooScraper`
4. **Cache agressivo**: Reduz drasticamente o número de requisições de scraping (na prática, uma mesma rota/data é buscada dezenas de vezes por dia)
5. **Separação de concerns**: Scrapers são isolados da lógica de negócio e podem ser testados independentemente
6. **Evolução gradual**: Começa simples (síncrono, 1 scraper) e evolui conforme necessidade (assíncrono, N scrapers, Redis)

### Contras

1. **Manutenção dos scrapers**: Sites mudam de layout sem aviso, quebrando os parsers — requer monitoramento contínuo e alertas de falha
2. **Risco legal**: Web scraping pode violar termos de serviço. Alguns sites (ex: Decolar) bloqueiam agressivamente scrapers
3. **Tempo de resposta**: Mesmo com cache, a primeira busca de uma rota pode levar 10-20 segundos (pior experiência do usuário)
4. **Complexidade operacional**: Browser headless (Puppeteer) consome memória (~100-200 MB por instância) e requer configuração adicional
5. **Bloqueio por IP**: Sites podem bloquear o IP após múltiplas requisições — exige proxies rotativos em produção
6. **Dados não estruturados**: Cada fonte tem seu próprio formato de dados, exigindo normalizadores específicos que podem introduzir bugs

### Mitigações

| Risco | Mitigação |
|---|---|
| Site muda de layout | Testes de integração periódicos que disparam alerta se falharem |
| Bloqueio de IP | Rotação de user-agents, delays aleatórios, proxies (futuro) |
| Tempo de resposta lento | Cache agressivo (30 min Redis + 5 min memória), feedback visual de carregamento |
| Risco legal | Priorizar fontes que permitem scraping no robots.txt; considerar APIs pagas como fallback |

## Opções Consideradas

### Opção A: Apenas APIs Pagas (Amadeus, Google Flights, Skyscanner)

- **Prós**: Dados estruturados, confiáveis, sem risco legal
- **Contras**: Custo por requisição (~US$ 0.01-0.05), necessidade de cadastro comercial, limite de requisições
- **Decisão**: Descartado por ser inviável para projeto acadêmico sem orçamento

### Opção B: Apenas Scraping com Browser Automation (Playwright)

- **Prós**: Funciona em qualquer site, independente de tecnologia
- **Contras**: Mais lento que HTTP simples, maior consumo de recursos
- **Decisão**: Rejeitado como estratégia única — usar browser só onde necessário (OTAs), e HTTP simples onde possível (companhias)

### Opção C: Cache Apenas em Memória

- **Prós**: Zero dependência externa, simplicidade
- **Contras**: Cache é perdido ao reiniciar a API, não compartilhado entre instâncias
- **Decisão**: Rejeitado como solução definitiva — usar Redis para cache compartilhado (Fase 4)

---

## Referências

- [`docs/pivotagem/PIVOTAGEM.md`](PIVOTAGEM.md) — Plano completo de pivotagem
- [`src/RedCodeApi/Program.cs`](../src/RedCodeApi/Program.cs) — Código da API
- [`db/script.sql`](../db/script.sql) — Script legado do banco (removido)
