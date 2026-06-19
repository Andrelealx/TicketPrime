# Segurança no Ciclo de Desenvolvimento — FlyCompare

> **Projeto:** FlyCompare — Metabuscador de Passagens Aéreas
> **Data:** 2026-06-18
> **Versão:** v2.0.0

---

## Threat Model

### Ativos Protegidos

| Ativo | Descrição | Criticidade |
|-------|-----------|-------------|
| Dados de usuários (emails) | Emails cadastrados para alertas de preço | Média |
| Banco de dados SQLite | Arquivo `redcode.db` com aeroportos, rotas, preços e alertas | Alta |
| API endpoints | Endpoints REST em `localhost:5246` | Alta |
| Credenciais SMTP | Configuração de email para notificações | Alta |
| Connection string | String de conexão com o banco de dados | Crítica |

### Vetor de Ataque Provável

**Ataque:** Injeção de SQL via parâmetros de query string nos endpoints de busca.

**Cenário:** Um atacante envia requisição maliciosa para `/api/voos/busca?origem=GRU';DROP TABLE Voos;--&destino=REC&dataPartida=2026-12-01`

**Superfície de ataque:** Todos os endpoints GET com parâmetros de query string: `/api/voos/busca`, `/api/aeroportos/busca`, `/api/alertas/{email}`.

### Falha Arquitetural Potencial

**Falha:** Se um desenvolvedor futuramente usar concatenação de strings em vez de parâmetros Dapper (`@Param`), o sistema fica vulnerável a SQL Injection.

**Evidência atual:** Todas as queries usam Dapper com parâmetros (`@Origem`, `@Destino`, etc.) — ✅ seguro atualmente.

**Risco residual:** Desenvolvedor novo no time pode não conhecer a política e usar interpolação (`$"WHERE Cpf = {cpf}"`).

### Controle de Engenharia (Mitigação)

1. **Revisão de código obrigatória:** Toda PR deve ser revisada por pelo menos 2 desenvolvedores antes do merge.
2. **Linter/Análise estática:** Adicionar regra no `.editorconfig` proibindo concatenação em queries.
3. **Testes de segurança:** Adicionar teste de integração que tenta SQL Injection e verifica resposta 400.
4. **Treinamento:** Todo novo desenvolvedor recebe onboarding sobre segurança com Dapper.
5. **OWASP Top 10:** Revisar código contra OWASP Top 10 a cada sprint.

---

## Gates de Segurança

### Gate 1 — Desenvolvimento (Antes do Commit)

- [ ] Nenhuma credencial hardcoded nos arquivos `.cs`
- [ ] Connection string lida de `appsettings.json` ou variável de ambiente
- [ ] Senha SMTP lida de configuração (não hardcoded)
- [ ] Queries SQL usam exclusivamente parâmetros Dapper (`@Param`)
- [ ] CORS configurado com origens específicas (não `AllowAnyOrigin`)
- [ ] Input validado antes de processar (IATA 3 caracteres, email com `@`, data não passada)

### Gate 2 — Build/CI (Antes do Merge)

- [ ] `dotnet build` passa com 0 erros e 0 warnings
- [ ] `dotnet test` — 27/27 testes passando
- [ ] Nenhum arquivo `.cs` contém `Password=`, `Pwd=`, `User Id=` com valores literais
- [ ] Verificação automatizada: `grep -r "Password=\|User Id=" src/` retorna vazio

### Gate 3 — Pre-Deploy (Antes de Produção)

- [ ] SLO verificado: error budget disponível antes do deploy
- [ ] Smoke test: 1 requisição bem-sucedida em cada endpoint crítico
- [ ] Rollback plan documentado: `git revert <commit>` pronto para executar
- [ ] Logs de segurança revisados: nenhum pattern suspeito nas últimas 24h
- [ ] Dados sensíveis mascarados nos logs (emails parcialmente ofuscados em produção)
- [ ] Feature flags configuradas: features experimentais desabilitadas por padrão

---

## Conformidade SSDF (Secure Software Development Framework)

| Prática SSDF | Implementação | Status |
|-------------|---------------|--------|
| Preparar a organização | Time treinado em OWASP Top 10 e SQL Injection | ✅ |
| Proteger o software | Parâmetros Dapper, sem concatenação SQL | ✅ |
| Produzir software seguro | CORS restrito, validação de input em todos endpoints | ✅ |
| Responder a vulnerabilidades | Processo documentado de correção (CORRECAO.md) | ✅ |

---

## Checklist de Segurança

- [x] Nenhuma credencial hardcoded em `.cs` (SSDF)
- [x] CORS restrito a `http://localhost:5139` (não `AllowAnyOrigin`)
- [x] Todas queries Dapper usam `@Param` (sem concatenação/interpolação)
- [x] Validação de input em todos os endpoints (IATA, email, data)
- [x] Connection string via `builder.Configuration` (não hardcoded)
- [x] Senha SMTP via `appsettings.json` (não hardcoded)
- [x] Logs não expõem dados sensíveis (emails logados apenas em ambiente dev)
- [x] HTTPS recomendado para produção (README documenta)
