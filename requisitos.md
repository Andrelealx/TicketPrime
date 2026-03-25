# 📋 Requisitos — Sistema TicketPrime

> Disciplina: Engenharia de Software | Prof. Dr. André Campos

---

## 📖 Histórias de Usuário

| ID | Ator | Como | Quero | Para |
|---|---|---|---|---|
| HU-01 | Cliente | Como Cliente | Quero informar meu CPF, nome e e-mail | Para realizar meu cadastro |
| HU-02 | Sistema | Como Sistema | Quero validar se o CPF é único | Para evitar cadastros duplicados |
| HU-03 | Admin | Como Admin | Quero cadastrar novos eventos | Para disponibilizá-los ao público |
| HU-04 | Admin | Como Admin | Quero definir a capacidade máxima | Para controlar a lotação |
| HU-05 | Cliente | Como Cliente | Quero selecionar um evento | Para iniciar a reserva |
| HU-06 | Cliente | Como Cliente | Quero aplicar um cupom | Para obter desconto |
| HU-07 | Sistema | Como Sistema | Quero validar se o cupom existe | Para evitar códigos falsos |
| HU-08 | Gestor | Como Gestor | Quero validar o valor mínimo do cupom | Para proteger a margem |
| HU-09 | Gestor | Como Gestor | Quero impedir valor final negativo | Para evitar prejuízos |
| HU-10 | Sistema | Como Sistema | Quero calcular o valor pago automaticamente | Para garantir precisão |
| HU-11 | Cliente | Como Cliente | Quero realizar a reserva vinculando CPF ao evento | Para garantir a entrada |
| HU-12 | Sistema | Como Sistema | Quero bloquear reservas acima da capacidade | Para impedir overbooking |
| HU-13 | Gestor | Como Gestor | Quero impedir compra múltipla por CPF | Para bloquear cambistas |
| HU-14 | Sistema | Como Sistema | Quero permitir reservas sem cupom | Para não travar vendas comuns |
| HU-15 | Auditor | Como Auditor | Quero ID único autoincrementado | Para rastrear cada venda |
| HU-16 | TI | Como TI | Quero usar Dapper com @ | Para proteger contra SQL Injection |
| HU-17 | TI | Como TI | Quero usar Chaves Estrangeiras (FK) | Para manter a integridade dos dados |
| HU-18 | Dev | Como Dev | Quero usar Minimal API | Para ter um backend performático |
| HU-19 | Dev | Como Dev | Quero criar script .sql manual | Para controle total do banco |
| HU-20 | Professor | Como Professor | Quero entrega via Git | Para auditar o histórico de commits |
| HU-21 | Sistema | Como Sistema | Quero respeitar o Case Sensitivity | Para passar na análise estática |
| HU-22 | Auditor | Como Auditor | Quero documentos na /docs | Para validar a engenharia |
| HU-23 | Auditor | Como Auditor | Quero scripts na /db | Para verificar o banco |
| HU-24 | Auditor | Como Auditor | Quero código na /src | Para analisar a implementação |
| HU-25 | Auditor | Como Auditor | Quero testes na /tests | Para validar a confiabilidade |
| HU-26 | Dev | Como Dev | Quero evitar Entity Framework | Para cumprir a restrição técnica |
| HU-27 | Dev | Como Dev | Quero evitar bancos em memória | Para garantir persistência real |
| HU-28 | Dev | Como Dev | Quero um README executável | Para rodar o projeto sem fricção |
| HU-29 | Dev | Como Dev | Quero usar xUnit | Para validar regras de negócio implacáveis |
| HU-30 | Sistema | Como Sistema | Quero impedir datas passadas | Para evitar erros operacionais |
| HU-31 | Financ. | Como Financ. | Quero gravar o valor final pago | Para auditoria contábil |
| HU-32 | Sistema | Como Sistema | Quero validar o formato do e-mail | Para manter dados de contato úteis |
| HU-33 | Eng. | Como Eng. | Quero entregar sem arquivos ZIP | Para não receber nota zero |
| HU-34 | Sistema | Como Sistema | Quero CPF com 11 caracteres | Para padronizar identificação |
| HU-35 | Admin | Como Admin | Quero ver a porcentagem do cupom | Para gerenciar campanhas |
| HU-36 | Dev | Como Dev | Quero rodar dotnet run | Para iniciar a API localmente |
## 🧩 DoD Consolidado de Execução

Baseado em todos os critérios deste documento, o item só é considerado "Done" quando implementação, validação BDD, testes e evidência estiverem concluídos.

### 1) DoD Funcional (Histórias de Usuário)

- [ ] HU-01: `POST /api/eventos` cria evento com `nome`, `data`, `capacidade`, `precoPadrao` e retorna `HTTP 201`.
- [ ] HU-02: `POST /api/cupons` cria cupom com `codigo`, `porcentagem`, `valorMinimo` e retorna `HTTP 201`.
- [ ] HU-03: `POST /api/usuarios` cria usuário com `cpf`, `nome`, `email`; CPF duplicado retorna `HTTP 400`.
- [ ] HU-04: `POST /api/reservas` cria reserva para CPF cadastrado e evento com vaga; sem cupom usa preço cheio; com cupom válido aplica desconto quando regra de valor mínimo for atendida.
- [ ] HU-05: `GET /api/reservas/{cpf}` retorna reservas do CPF com `NomeEvento` (via JOIN) e `ValorFinalPago`; sem reservas retorna `HTTP 200` com lista vazia.
- [ ] HU-06: Reserva bloqueada quando capacidade do evento foi atingida (`HTTP 400`).
- [ ] HU-07: Reserva bloqueada quando CPF já possui 2 reservas para o mesmo evento (`HTTP 400`).

### 2) DoD de Qualidade (BDD e Validações)

- [ ] Requisitos registrados no formato de histórias: `Como`, `Quero`, `Para`.
- [ ] Critérios BDD registrados no formato: `Dado`, `Quando`, `Então`.
- [ ] Todos os cenários de erro usam fail-fast (`BadRequest` ou `NotFound`) com mensagem clara.
- [ ] Regras R1, R2, R3 e R4 cobertas por testes automatizados.

### 3) DoD Técnico - AV1 (Fundação)

- [ ] `README.md` na raiz com instruções de execução em bloco de código.
- [ ] Script SQL em `/db` com `CREATE TABLE` para `Usuarios`, `Eventos`, `Cupons`, `Reservas`, incluindo FKs.
- [ ] API minimal em `/src` com mapeamento de endpoints obrigatórios da AV1 (`MapGet` e `MapPost`).
- [ ] Todas as queries Dapper usam parâmetros (`@Param`), sem concatenação ou interpolação de SQL.
- [ ] Projeto de testes em `/tests` com pelo menos um teste `[Fact]` ou `[Theory]`.
- [ ] Todo teste possui `Assert` válido.

### 4) DoD Técnico - AV2 (Arquitetura e Operação)

- [ ] ADR em `/docs` com seções: `## Contexto`, `## Decisão`, `## Consequências`.
- [ ] Em `## Consequências`, existem `Prós` e `Contras` explícitos.
- [ ] `/docs/operacao.md` contém matriz de riscos com colunas: `Risco`, `Probabilidade`, `Impacto`, `Ação`, `Gatilho`.
- [ ] `/docs/operacao.md` define métrica com: `Fórmula`, `Fonte de Dados`, `Frequência`, `Ação se Violado`.
- [ ] Documento explicita `SLO` com porcentagem e janela de tempo.
- [ ] Documento explicita `Error Budget Policy`.
- [ ] Nenhum arquivo `.cs` contém credenciais em texto plano (`Password=`, `User Id=`).
- [ ] `release_checklist_final.md` existe na raiz com todos os itens finais marcados.

### 5) DoD de Regras de Negócio (Endpoints)

- [ ] R1 Integridade: em `POST /api/reservas`, valida existência de `UsuarioCpf` e `EventoId` antes do insert.
- [ ] R2 Limite por CPF: bloqueia terceira reserva do mesmo CPF no mesmo evento.
- [ ] R3 Estoque: bloqueia reserva quando total de reservas >= `CapacidadeTotal`.
- [ ] R4 Motor de cupons: aplica desconto somente se `PrecoPadrao >= ValorMinimoRegra`.
- [ ] Consulta com JOIN: `GET /api/reservas/{cpf}` retorna nome do evento (não apenas ID).

### 6) DoD de Entrega (Regras Gerais)

- [ ] Estrutura de pastas na raiz: `/docs`, `/db`, `/src`, `/tests`.
- [ ] Entrega feita por URL de repositório GitHub ou GitLab.
- [ ] Grupo respeita tamanho entre 5 e 6 alunos.
- [ ] Persistência com Dapper (sem Entity Framework e sem banco em memória).
- [ ] Nomes de pastas, tabelas, colunas e rotas seguem exatamente o especificado nos requisitos.

### 7) Condição de Conclusão

Um item (história, regra, documento ou endpoint) só é considerado "Done" quando:

- [ ] Implementação concluída.
- [ ] Critério BDD correspondente atendido.
- [ ] Teste automatizado passando.
- [ ] Evidência registrada (código, teste, documento ou resposta de endpoint).

O projeto só é considerado "Done" quando todos os itens deste documento estiverem marcados.

---

## ✅ Critérios de Aceitação (BDD)

### HU-01 — Cadastro de Evento

```
Dado que o organizador preenche todos os campos obrigatórios (nome, data, capacidade, preço)
Quando ele envia a requisição POST /api/eventos
Então o sistema deve retornar HTTP 201 e o evento deve constar na listagem
```

### HU-02 — Cadastro de Cupom

```
Dado que o administrador informa código, porcentagem e valorMínimo do cupom
Quando a requisição POST /api/cupons é enviada
Então o sistema retorna HTTP 201 e o cupom fica disponível para uso nas reservas
```

### HU-03 — Cadastro de Usuário

```
Dado que o comprador informa CPF, nome e e-mail válidos
Quando ele envia POST /api/usuarios
Então o sistema retorna HTTP 201 e o cadastro é criado

Dado que o CPF informado já existe no banco de dados
Quando a requisição é enviada
Então o sistema retorna HTTP 400 com mensagem de CPF duplicado
```

### HU-04 — Realizar Reserva

```
Dado que o comprador tem CPF cadastrado e o evento possui vagas disponíveis
Quando ele envia POST /api/reservas sem cupom
Então o sistema registra a reserva com o PreçoPadrão do evento e retorna HTTP 201

Dado que o comprador informa um cupom válido e o preço do evento >= ValorMínimoRegra
Quando a reserva é processada
Então o ValorFinalPago deve ser o PreçoPadrão com o desconto aplicado
```

### HU-05 — Consultar Reservas

```
Dado que o comprador possui ao menos uma reserva registrada
Quando ele envia GET /api/reservas/{cpf}
Então o sistema retorna a lista de reservas com o Nome do Evento (via JOIN) e o ValorFinalPago

Dado que o CPF não possui reservas
Quando a requisição é enviada
Então o sistema retorna HTTP 200 com lista vazia
```

### HU-06 — Controle de Capacidade (R3)

```
Dado que o evento já tem reservas igual à sua CapacidadeTotal
Quando um comprador tenta realizar uma nova reserva para esse evento
Então o sistema retorna HTTP 400 com mensagem de esgotamento de capacidade
```

### HU-07 — Limite por CPF (R2)

```
Dado que um CPF já possui 2 reservas para o mesmo EventoId
Quando uma terceira reserva é tentada para o mesmo evento e CPF
Então o sistema retorna HTTP 400 com mensagem de limite por CPF atingido
```

---

## 🏁 Definition of Done (DoD) — Checklist QA

### AV1 — Fundação e Cadastros Básicos

| # | Critério | Descrição / Evidência Esperada | Status |
|---|----------|-------------------------------|--------|
| 1 | **Histórias de Usuário** | Mínimo 3 HUs no formato exato: `Como [ator], Quero [ação], Para [motivo]` no arquivo `/docs/requisitos.md` | ⬜ Pendente |
| 2 | **Critérios BDD** | Ao menos 1 HU com critérios no formato: `Dado que... Quando... Então...` no `/docs/requisitos.md` | ⬜ Pendente |
| 3 | **README Executável** | Arquivo `README.md` na raiz com comandos de terminal (ex: `dotnet run`) em blocos de código Markdown | ⬜ Pendente |
| 4 | **Script do Banco** | Arquivo `.sql` em `/db` com `CREATE TABLE` das 4 tabelas (Usuarios, Eventos, Cupons, Reservas) com FKs corretas | ⬜ Pendente |
| 5 | **Contrato da API** | Minimal API mapeando os 4 endpoints da AV1 com `app.MapGet` e `app.MapPost` em `/src` | ⬜ Pendente |
| 6 | **Fail-Fast (Validação)** | Endpoints retornam `Results.BadRequest` ou `Results.NotFound` em casos de erro (ex: CPF duplicado) | ⬜ Pendente |
| 7 | **Segurança no Dapper** | Todas as queries usam parâmetros `@NomeParam` (ex: `WHERE Cpf = @Cpf`) — nenhuma exceção | ⬜ Pendente |
| 8 | **Zero SQL Injection** | Proibido concatenação (`+`) ou interpolação (`$"{}"`) em queries SQL — falha aqui **ZERA** o item | ⬜ Pendente |
| 9 | **Infraestrutura de Testes** | Projeto xUnit em `/tests` com ao menos 1 método anotado com `[Fact]` ou `[Theory]` | ⬜ Pendente |
| 10 | **Testes com Oráculo** | Todo método de teste possui cláusula `Assert.` — testes sem Assert são inválidos e **zeram** o item | ⬜ Pendente |

### AV2 — Coração do Sistema e Blindagem

| # | Critério | Descrição / Evidência Esperada | Status |
|---|----------|-------------------------------|--------|
| 11 | **ADR — Decisão Arquitetural** | Arquivo Markdown em `/docs` com títulos exatos: `## Contexto`, `## Decisão` e `## Consequências` | ⬜ Pendente |
| 12 | **Trade-offs no ADR** | Seção `## Consequências` contém lista com `Prós:` e `Contras:` da decisão técnica explicitados | ⬜ Pendente |
| 13 | **Matriz de Riscos** | Tabela Markdown em `/docs/operacao.md` com colunas: `Risco`, `Probabilidade`, `Impacto`, `Ação` | ⬜ Pendente |
| 14 | **Gatilhos de Risco** | Matriz de riscos possui coluna extra `Gatilho` com o evento que dispara cada ação de mitigação | ⬜ Pendente |
| 15 | **Métricas Operacionais** | Em `/docs/operacao.md`, métrica definida com campos exatos: `Fórmula:`, `Fonte de Dados:` e `Frequência:` | ⬜ Pendente |
| 16 | **Ação da Métrica** | Campo exato `Ação se Violado:` presente na métrica, informando o que o time deve fazer | ⬜ Pendente |
| 17 | **SLO (Objetivo de Serviço)** | Documento contém o termo `SLO:` seguido de uma porcentagem (`%`) e uma janela de tempo (`dias/horas`) | ⬜ Pendente |
| 18 | **Error Budget Policy** | Documento `Error Budget Policy:` descreve o que o time deve fazer quando o orçamento de falhas se esgota | ⬜ Pendente |
| 19 | **Segurança SSDF** | Nenhum arquivo `.cs` contém `Password=` ou `User Id=` em texto plano — usar variáveis de ambiente/secrets | ⬜ Pendente |
| 20 | **Checklist Final (DoD)** | Arquivo `release_checklist_final.md` na raiz com todas as caixas marcadas como `[x]` | ⬜ Pendente |

### AV2 — Regras de Negócio (Endpoints)

| # | Critério | Descrição / Evidência Esperada | Status |
|---|----------|-------------------------------|--------|
| 21 | **Regra R1 — Integridade** | `POST /api/reservas` valida se `UsuarioCpf` e `EventoId` existem antes do INSERT; retorna 400 se não existir | ⬜ Pendente |
| 22 | **Regra R2 — Limite por CPF** | `POST /api/reservas` bloqueia com 400 quando o mesmo CPF já tem 2 reservas para o mesmo `EventoId` | ⬜ Pendente |
| 23 | **Regra R3 — Estoque** | `POST /api/reservas` bloqueia com 400 quando reservas existentes >= `CapacidadeTotal` do evento | ⬜ Pendente |
| 24 | **Regra R4 — Motor de Cupons** | Desconto só é aplicado quando `PrecoPadrao >= ValorMinimoRegra`; caso contrário, preço cheio é cobrado | ⬜ Pendente |
| 25 | **GET Reservas com JOIN** | `GET /api/reservas/{cpf}` usa `INNER JOIN` no Dapper para retornar o Nome do Evento (não apenas o ID) | ⬜ Pendente |

### Regras Gerais (Semestre Todo)

| # | Critério | Descrição / Evidência Esperada | Status |
|---|----------|-------------------------------|--------|
| 26 | **Estrutura de Pastas** | Repositório possui exatamente `/docs`, `/db`, `/src` e `/tests` na raiz (case sensitive) | ⬜ Pendente |
| 27 | **Entrega via Git** | Entrega feita exclusivamente via URL de repositório GitHub ou GitLab (zip/rar/Drive = nota **ZERO**) | ⬜ Pendente |
| 28 | **Tamanho do Grupo** | Grupo com mínimo 5 e máximo 6 alunos — sem exceções | ⬜ Pendente |
| 29 | **Banco com Dapper** | Entity Framework (Code-First) e bancos em memória são expressamente proibidos em todo o projeto | ⬜ Pendente |
| 30 | **Nomes Imutáveis** | Nomes de pastas, tabelas, colunas e rotas HTTP são exatamente os definidos no documento — sem alteração | ⬜ Pendente |

---

> **Legenda de Status:** ⬜ Pendente &nbsp;|&nbsp; ✅ Aprovado &nbsp;|&nbsp; ❌ Reprovado &nbsp;|&nbsp; 🔄 Em Revisão
