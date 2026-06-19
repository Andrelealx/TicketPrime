# 📋 Estado do Projeto — FlyCompare

> **Última sessão**: 2026-06-18
> **Build**: ✅ 0 erros, 0 warnings
> **Testes**: ✅ 27/27 aprovados (21 unitários + 6 integração)
> **SPECs**: 32/33 implementadas (97%) — SPEC-033 parcial (~60%)
> **CORRECAO.md**: 19 correções aplicadas, 10 LOW issues pendentes

---

## 1. O que é o projeto

**FlyCompare** — Metabuscador de passagens aéreas. Pesquisa preços em múltiplas fontes (LATAM, GOL, Azul, Decolar via scraping) e apresenta resultados consolidados.

- **Backend**: .NET 10 Minimal API + Dapper + SQLite
- **Frontend**: Blazor WebAssembly
- **Jobs**: Hangfire (scraping a cada 6h, alertas a cada 2h)
- **Scraping**: HtmlAgilityPack + PuppeteerSharp

---

## 2. Tudo que foi feito nesta sessão

### 2.1 Renomeação completa (TicketPrime → RedCode)
- Diretórios: `TicketPrimeApi/` → `RedCodeApi/`, `TicketPrimeFront/` → `RedCodeFront/`
- Todos os namespaces, using statements, project references, .sln
- CSS, SQL scripts, docs, package.json, scripts
- **Nenhum** `TicketPrime` restante no projeto

### 2.2 Achatamento da estrutura
- Antes: `Red-code-master/Red-code-master/src/...` (aninhamento duplo)
- Depois: `Red-code-master/src/...` (estrutura plana)
- Atualizados caminhos no `.sln` (removido prefixo `Red-code-master\`)

### 2.3 Limpeza de testes (71 → 21 → 27)
- Removidos 25 testes de `CalculoCupomTests` (sistema legado de eventos/cupons)
- Removidos 24 testes de `FormatarNomeCompanhia` / `FormatarTipoTarifa` (falsos positivos — duplicavam implementação)
- Removido 1 teste trivial (`ResultadoBusca_PrecoTotal` — testava aritmética)
- Mantidos 21 testes que testam código real (NormalizadorDados + IATA + DTOs)
- Adicionados 6 testes de integração com `WebApplicationFactory`

### 2.4 Remoção de código morto
- `src/RedCodeFront/Models/Models.cs` — Classes `Usuario`, `Evento`, `Cupom`, `ReservaReq`, `ReservaConsulta`
- `docs/requisitos.md` — Documentava sistema legado
- `db/script.sql` — Tabelas SQL legadas

### 2.5 Refatoração do Program.cs (ADR-002)
- 577 linhas monolíticas → 93 linhas de pipeline limpo
- Extraídos para `Endpoints/`: Aeroportos, Companhias, Rotas, Voos, Alertas
- Extraídos para `Data/`: DbInitializer, MockVoosGenerator
- Helper DRY para configuração de HttpClient dos scrapers

### 2.6 Segurança (ADR-003)
- CORS: `AllowAnyOrigin()` → `WithOrigins("http://localhost:5139")`

### 2.7 Correção de bugs (ADR-004)
- **LOW-02**: Deduplicação agora usa chave composta `CodigoVoo|Companhia`
- **LOW-13**: CORS corrigido (ver 2.6)
- 13 LOW issues restantes documentadas como dívida técnica

### 2.8 Testes de integração (ADR-005)
- `tests/IntegrationTests.cs` — 6 testes com `WebApplicationFactory`
- Testa endpoints reais: aeroportos, busca, companhias, rotas, validações

### 2.9 Documentação atualizada
- `docs/adr/` — 5 ADRs documentando cada decisão
- `docs/SPECS-FLYCOMPARE.md` — 32/33 SPECs (97%), F5 100%, F6 75%
- `docs/pivotagem/PIVOTAGEM.md` — DoD 11/11 concluído
- `CORRECAO.md` — 19 correções aplicadas, 10 LOW pendentes
- `docs/ESTADO-DO-PROJETO.md` — Este documento

### 2.10 Revisão adicional (2026-06-18)
- **CORRECAO.md** atualizado: paths corrigidos para estrutura refatorada, issues obsoletas removidas (LOW-01, LOW-03, LOW-05, LOW-07, LOW-10, LOW-11), novas issues documentadas
- **SPECS-FLYCOMPARE.md** atualizado: SPEC-033 marcada como parcial (~60%), job IDs corrigidos
- **README.md** atualizado: documentação reflete SQLite (não SQL Server)
- **Alerta.razor** corrigido: switch expression com suporte a "sucesso", "erro", "aviso", "info" + XML docs
- **ESTADO-DO-PROJETO.md** atualizado: data e estatísticas atualizadas

---

## 3. Estrutura atual do projeto

```
Red-code-master/
├── Red-code-master.sln
├── .gitignore
├── README.md
├── CORRECAO.md
├── claude.md
├── package.json
├── setup-local.ps1
├── teste.http
├── db/
│   ├── script-flycompare.sql          # Tabelas FlyCompare
│   └── cleanup-legado.sql             # Referência histórica
├── docs/
│   ├── adr/                           # 5 ADRs
│   │   ├── ADR-001-arquitetura-*.md
│   │   ├── ADR-002-refatoracao-*.md
│   │   ├── ADR-003-cors-restrito.md
│   │   ├── ADR-004-low-issues.md
│   │   └── ADR-005-testes-integracao.md
│   ├── pivotagem/
│   │   ├── PIVOTAGEM.md
│   │   ├── ROADMAP.md
│   │   └── REQUISITOS-FLYCOMPARE.md
│   ├── SPECS-FLYCOMPARE.md
│   ├── arquitetura.md
│   ├── visao.md
│   ├── roadmap.md
│   └── ESTADO-DO-PROJETO.md           # ← este arquivo
├── scripts/
│   ├── dev-all.mjs
│   └── postinstall.mjs
├── src/
│   ├── RedCodeApi/
│   │   ├── Program.cs                 # 93 linhas — pipeline limpo
│   │   ├── RedCodeApi.csproj
│   │   ├── Endpoints/
│   │   │   ├── AeroportosEndpoints.cs
│   │   │   ├── AlertasEndpoints.cs
│   │   │   ├── CompanhiasEndpoints.cs
│   │   │   ├── RotasEndpoints.cs
│   │   │   └── VoosEndpoints.cs
│   │   ├── Data/
│   │   │   ├── DbInitializer.cs
│   │   │   └── MockVoosGenerator.cs
│   │   ├── Models/FlyCompare/
│   │   ├── Dtos/FlyCompare/
│   │   └── Services/
│   │       ├── CacheService.cs
│   │       ├── ScrapingScheduler.cs
│   │       └── Scrapers/
│   │           ├── IVooScraper.cs
│   │           ├── NormalizadorDados.cs   # LOW-02 corrigido
│   │           ├── ScraperAzul.cs
│   │           ├── ScraperDecolar.cs
│   │           ├── ScraperGol.cs
│   │           └── ScraperLatam.cs
│   └── RedCodeFront/
│       ├── Pages/
│       │   ├── Index.razor
│       │   ├── BuscarVoos.razor
│       │   ├── ResultadosBusca.razor
│       │   └── MeusAlertas.razor
│       ├── Shared/
│       │   ├── MainLayout.razor
│       │   └── Alerta.razor
│       └── wwwroot/
└── tests/
    ├── RedCodeTests.csproj
    ├── UnitTest1.cs                   # 21 testes unitários
    └── IntegrationTests.cs            # 6 testes de integração
```

---

## 4. Como rodar

```powershell
# Setup completo (Windows)
npm install
npm run dev

# Ou manualmente:
dotnet restore src/RedCodeApi/RedCodeApi.csproj
dotnet restore src/RedCodeFront/RedCodeFront.csproj
dotnet run --project src/RedCodeApi/RedCodeApi.csproj     # API: localhost:5246
dotnet run --project src/RedCodeFront/RedCodeFront.csproj # Front: localhost:5139

# Testes:
dotnet test tests/RedCodeTests.csproj   # 27/27 aprovados
```

---

## 5. Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/aeroportos` | Listar aeroportos |
| GET | `/api/aeroportos/busca?q=` | Autocomplete (min 2 chars) |
| GET | `/api/companhias` | Listar companhias aéreas |
| GET | `/api/rotas/populares` | 22 rotas populares |
| GET | `/api/voos/busca?origem=&destino=&dataPartida=` | Buscar voos (cache → scrapers → mock → normalizador) |
| GET | `/api/voos/precos/{vooId}` | Histórico de preços |
| POST | `/api/alertas` | Criar alerta de preço |
| GET | `/api/alertas/{email}` | Listar alertas por email |

---

## 6. Pendências (para futuras sessões)

### SPEC-033 (Layout final) — parcialmente implementada (~60%)
- Finalizar CSS tema FlyCompare
- ✅ CSS com tema FlyCompare e prefixo `fc-`
- ✅ MainLayout.razor com sidebar, logo e navegação
- ✅ Index.razor com hero, stats e cards
- ❌ Responsividade mobile
- Breadcrumbs e loading states

### LOW Issues restantes (10)
- Documentadas no `CORRECAO.md`
- Maioria é baixo impacto ou dívida técnica consciente

### Melhorias futuras sugeridas
- Adicionar `appsettings.json` para origem CORS configurável
- Implementar envio real de email nos alertas (SMTP)
- Health check endpoint (`/health`)
- Rate limiting nos scrapers
- Unificar `ResultadoBusca` entre API e Frontend

---

> **Para retomar**: leia este documento, os ADRs em `docs/adr/`, e execute `dotnet build && dotnet test` para confirmar o estado.
