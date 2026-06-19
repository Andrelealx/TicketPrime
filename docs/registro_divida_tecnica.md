# Registro de Dívida Técnica — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

## Dívidas Técnicas Identificadas

| ID | Descrição Técnica | Freq. Alteração | Risco | Esforço | Decisão |
|----|-------------------|----------------|-------|---------|---------|
| DT-01 | `NormalizadorDados` concentra 4 responsabilidades (padronização, dedup, outliers, ordenação) em uma única classe. Se uma etapa crescer, toda a classe precisa ser modificada. | Baixo | Médio | Médio | Prioridade 2 (Próxima Sprint) |
| DT-02 | `Program.cs` ainda contém método local `ConfigureScraperHttpClient<T>()` e configuração Hangfire inline. Deveria ser refatorado para extension methods. | Baixo | Baixo | Baixo | Prioridade 2 (Próxima Sprint) |
| DT-03 | Conexão com banco usa `new SqliteConnection(connStr)` diretamente em `ScrapingScheduler`, `VoosEndpoints` e outros. Sem abstração, trocar para SQL Server exige alterar múltiplas classes. | Médio | Alto | Médio | Prioridade 1 (Imediato) |
| DT-04 | `ScraperDecolar` mantém `static IBrowser` compartilhado sem `IDisposable`. Processo Chromium pode ficar órfão no shutdown da aplicação. Health check implementado (LOW-02) mas dispose ainda não existe. | Baixo | Médio | Baixo | Prioridade 2 (Próxima Sprint) |
| DT-05 | Connection string injetada como `string` primitivo — ambiguidade de DI se outro `string` for registrado. Deveria ser wrapper tipado (`ConnectionString`) ou `IOptions<T>`. | Baixo | Baixo | Baixo | Prioridade 2 (Próxima Sprint) |
| DT-06 | Modelo `ResultadoBusca` duplicado entre API (`RedCodeApi.Dtos.FlyCompare`) e Frontend (`RedCodeFront.Models.FlyCompare`). Manutenção duplicada — qualquer novo campo precisa ser adicionado em dois lugares. (LOW-03 parcialmente corrigido — campos sincronizados, mas duplicação permanece.) | Alto | Médio | Alto | Prioridade 3 (Aceitar/Ignorar) |
| DT-07 | `BuscarVoos.razor` e `MeusAlertas.razor` definem modelos aninhados (`RotaFront`, `AlertaResponse`, `AlertaRequestFront`) como classes privadas dentro do code-behind. Duplicação de modelos entre páginas e com a API. | Médio | Baixo | Médio | Prioridade 3 (Aceitar/Ignorar) |
| DT-08 | Testes de unidade não seguem padrão AAA com comentários explícitos. Nomes de teste não usam sufixo `_Quando_Cenario_ResultadoEsperado` consistente (alguns usam, outros não). | Alto | Baixo | Baixo | Prioridade 1 (Imediato) |

## Legenda

### Freq. Alteração
- **Alto:** Modificado frequentemente (>1x por sprint)
- **Médio:** Modificado ocasionalmente (1x a cada 2-3 sprints)
- **Baixo:** Raramente modificado

### Risco
- **Alto:** Pode causar bugs em produção ou perda de dados
- **Médio:** Pode causar bugs em desenvolvimento ou dificultar manutenção
- **Baixo:** Impacto limitado, melhoria de qualidade

### Esforço
- **Alto:** Requer refatoração significativa (>2 dias)
- **Médio:** Requer refatoração moderada (4-16 horas)
- **Baixo:** Pode ser resolvido em <4 horas

### Decisão
- **Prioridade 1 (Imediato):** Deve ser resolvido na sprint atual
- **Prioridade 2 (Próxima Sprint):** Planejar para próxima sprint
- **Prioridade 3 (Aceitar/Ignorar):** Dívida consciente — baixo impacto, alto esforço
