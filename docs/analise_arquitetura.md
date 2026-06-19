# Análise de Padrões Arquiteturais — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

---

## 1. Análise de Cenários Arquiteturais

### Cenário 1: Sistema de Scraping Multi-Fonte (Strategy Pattern)

**Contexto:** O FlyCompare precisa buscar preços em múltiplas fontes (LATAM, GOL, Azul, Decolar) com diferentes estratégias de extração (HTTP/HTML vs Puppeteer/Headless Browser).

**Padrão Identificado:** Strategy Pattern

**Evidência no código:**
- Interface `IVooScraper` define o contrato comum (`Nome`, `Ordem`, `BuscarVoosAsync()`)
- Classes concretas: `ScraperLatam`, `ScraperGol`, `ScraperAzul`, `ScraperDecolar`
- Injeção via `IEnumerable<IVooScraper>` em `Program.cs` e `ScrapingScheduler.cs`
- Execução paralela com `Task.WhenAll()`
- Cada scraper encapsula sua própria lógica de parsing e URL

**Trade-off:**
- **Positivo:** Adicionar nova companhia aérea requer apenas uma nova classe implementando `IVooScraper` e registro no container DI. Código existente não é modificado (Open/Closed Principle).
- **Negativo:** Cada scraper tem seu próprio `HttpClient` (via `AddHttpClient<T>`) — se houver 20 scrapers, serão 20 conexões HTTP. Não há connection pooling compartilhado entre scrapers.

---

### Cenário 2: Cache em Duas Camadas (Decorator/Chain of Responsibility)

**Contexto:** O sistema precisa de cache rápido (memória) para baixa latência e cache distribuído (Redis) para compartilhamento entre instâncias.

**Padrão Identificado:** Cache-Aside com Fallback em Cascata (variação de Chain of Responsibility)

**Evidência no código:**
- `CacheService` com duas camadas: `IMemoryCache` (L1) + `IDistributedCache` (L2)
- Leitura em cascata: Redis → Memória → null
- Escrita em ambas as camadas simultaneamente
- TTL com sliding expiration na memória, absolute no Redis
- Fallback automático quando Redis não está configurado

**Trade-off:**
- **Positivo:** Degradação graciosa — se Redis falhar, o sistema continua funcionando apenas com cache em memória. Sem ponto único de falha.
- **Negativo:** Em cenário multi-instância sem Redis, cada instância tem seu próprio cache local, podendo servir dados inconsistentes. A primeira requisição em cada instância sempre sofre cache miss.

---

### Cenário 3: Pipeline de Processamento de Dados (Pipes and Filters)

**Contexto:** Os resultados brutos dos scrapers precisam ser processados em etapas sequenciais: padronização, deduplicação, remoção de outliers e ordenação.

**Padrão Identificado:** Pipes and Filters

**Evidência no código:**
- `NormalizadorDados.Normalizar()` com 4 etapas encadeadas:
  1. `PadronizarCampos()` — uppercase, formatação, sanitização
  2. `Deduplicar()` — agrupamento por chave composta, mantém mais barato
  3. `RemoverOutliers()` — método estatístico 3σ
  4. `OrdenarPorPreco()` — ordenação final
- Cada etapa é um método independente e testável
- Saída de uma etapa é entrada da próxima (composição funcional)

**Trade-off:**
- **Positivo:** Cada filtro é independente e testável isoladamente (7 testes unitários no `UnitTest1.cs`). Adicionar nova etapa de normalização não afeta as existentes.
- **Negativo:** O pipeline é síncrono e sequencial — não aproveita paralelismo entre etapas. Para volumes muito grandes de dados, cada etapa espera a anterior terminar completamente.

---

## 2. Violações Arquiteturais Identificadas

### Violação 1: God Class em Potencial — NormalizadorDados

**Problema:** O `NormalizadorDados` concentra 4 responsabilidades distintas (padronização, deduplicação, remoção de outliers, ordenação) em uma única classe. Se cada pipeline crescer em complexidade, a classe pode se tornar um "God Object".

**Evidência:** Arquivo `src/RedCodeApi/Services/Scrapers/NormalizadorDados.cs` contém os 4 métodos privados + método público `Normalizar()`. Todos residem na mesma classe com acoplamento interno.

**Impacto:** Dificuldade de testar etapas individuais sem mock. Se uma etapa falhar, o pipeline inteiro é afetado. Mudanças em uma etapa podem impactar outras.

**Ação Recomendada:** Extrair cada etapa para uma classe separada implementando uma interface `IEtapaNormalizacao`. Compor o pipeline via injeção de dependência (`IEnumerable<IEtapaNormalizacao>`).

---

### Violação 2: Violação do Princípio de Responsabilidade Única — Program.cs

**Problema:** O `Program.cs` (103 linhas) concentra configuração de DI, CORS, cache, scrapers, Hangfire, banco de dados e endpoints. Embora tenha sido reduzido de 577 para 103 linhas (ADR-002), ainda contém múltiplas responsabilidades.

**Evidência:** `Program.cs` configura simultaneamente CORS policy, HttpClient para scrapers, Hangfire jobs, connection string e middleware pipeline.

**Impacto:** Dificuldade de localizar configurações específicas. `ConfigureScraperHttpClient<T>()` é um método local (não extension method), dificultando reuso e teste.

**Ação Recomendada:** Extrair `ConfigureScraperHttpClient<T>()` para uma classe `ScraperConfigurationExtensions`. Mover configuração Hangfire para `HangfireConfigurationExtensions`. Usar extension methods em `IServiceCollection`.

---

### Violação 3: Acoplamento Direto com SQLite — ScrapingScheduler e VoosEndpoints

**Problema:** Múltiplas classes instanciam `SqliteConnection` diretamente com `new SqliteConnection(_connStr)`, criando acoplamento forte com SQLite.

**Evidência:** 
- `ScrapingScheduler.cs` linha 131: `using var db = new SqliteConnection(_connStr);`
- `VoosEndpoints.cs`: `using var db = new SqliteConnection(connectionString);`
- Se o banco mudar para SQL Server, todas essas classes precisam ser alteradas.

**Impacto:** Não é possível trocar o banco de dados sem modificar código de infraestrutura. Testes de unidade são difíceis porque não há abstração do banco.

**Ação Recomendada:** Criar uma factory `IDbConnectionFactory` que retorna `IDbConnection`. Registrar no DI. Injetar nos serviços em vez da connection string diretamente.

---

### Violação 4: Browser Global Compartilhado sem Gerenciamento de Ciclo de Vida — ScraperDecolar

**Problema:** O `ScraperDecolar` mantém um `static IBrowser` compartilhado via `SemaphoreSlim`, mas não há mecanismo de dispose quando a aplicação termina, nem renovação automática em caso de crash do browser.

**Evidência:** `ScraperDecolar.cs` linhas 13-14: `private static IBrowser? _browserCompartilhado;` com `SemaphoreSlim`. O browser nunca é fechado explicitamente no shutdown da aplicação.

**Impacto:** Vazamento de recursos (processo Chromium órfão). Se o browser travar, o `SemaphoreSlim` pode ficar bloqueado permanentemente.

**Ação Recomendada:** Implementar `IDisposable` ou `IAsyncDisposable` no `ScraperDecolar`. Registrar como Singleton e fazer o dispose no `IHostApplicationLifetime.ApplicationStopping`. (Parcialmente corrigido com health check LOW-02.)

---

### Violação 5: Injeção de Connection String como Singleton

**Problema:** A connection string é registrada como singleton (`builder.Services.AddSingleton(connStr)`) e injetada diretamente como `string` nos serviços. Isso é frágil — se outro `string` for registrado, há ambiguidade.

**Evidência:** `Program.cs` linha 74: `builder.Services.AddSingleton(connStr);` e construtores como `ScrapingScheduler(..., string connStr)`.

**Impacto:** Ambiguidade de injeção. Se um segundo `string` for registrado, o DI lança exceção. Dificulta identificar qual parâmetro do construtor é a connection string.

**Ação Recomendada:** Criar um wrapper `class ConnectionString { public string Value { get; } }` ou usar `IOptions<ConnectionStringsOptions>`. Isso elimina ambiguidade e permite validação na inicialização.

---

## Resumo

| # | Tipo | Padrão/Violação | Status |
|---|------|----------------|--------|
| 1 | Padrão | Strategy (Scrapers) | ✅ Implementado |
| 2 | Padrão | Cache-Aside (CacheService) | ✅ Implementado |
| 3 | Padrão | Pipes and Filters (Normalizador) | ✅ Implementado |
| 4 | Violação | God Class — NormalizadorDados | 📋 Documentada |
| 5 | Violação | SRP — Program.cs | 📋 Documentada |
| 6 | Violação | Acoplamento SQLite | 📋 Documentada |
| 7 | Violação | Browser sem lifecycle | 🔄 Parcialmente corrigido |
| 8 | Violação | Connection String ambígua | 📋 Documentada |
