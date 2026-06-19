# ✈️ Requisitos — FlyCompare (Metabuscador de Passagens Aéreas)

> **Versão**: 1.0 | **Data**: 2026-05-14

---

## 📖 Histórias de Usuário

| ID | Ator | Como | Quero | Para |
|---|---|---|---|---|
| FC-01 | Viajante | Como viajante | Quero buscar passagens informando origem, destino e data | Para comparar preços de diferentes companhias |
| FC-02 | Viajante | Como viajante | Quero ver o preço total incluindo taxas | Para saber exatamente quanto vou pagar |
| FC-03 | Viajante | Como viajante | Quero filtrar por companhia, paradas e horário | Para encontrar o voo ideal |
| FC-04 | Viajante | Como viajante | Quero ordenar por preço, duração ou horário | Para escolher a melhor opção |
| FC-05 | Viajante | Como viajante | Quero criar alertas de preço para uma rota | Para ser notificado quando o preço baixar |
| FC-06 | Viajante | Como viajante | Quero ver o histórico de preços de um voo | Para saber se o preço atual é bom |
| FC-07 | Sistema | Como sistema | Quero cachear resultados de busca | Para não sobrecarregar as fontes de scraping |
| FC-08 | Admin | Como admin | Quero que os scrapers atualizem preços periodicamente | Para manter os dados frescos |

---

## 🧩 Regras de Negócio

### RN-01: Busca de Voos
- A busca deve aceitar origem (código IATA), destino (código IATA) e data de partida
- A data de partida não pode ser no passado
- O código IATA deve ter exatamente 3 caracteres
- Retorna lista consolidada de voos ordenada por preço total

### RN-02: Scraping e Cache
- Resultados de busca são cacheados com TTL de 30 minutos
- Se cache expirar, executa scrapers em paralelo
- Se todos os scrapers falharem, usa dados mockados como fallback
- Resultados são normalizados (deduplicação, ordenação por preço)

### RN-03: Alertas de Preço
- Alerta é criado vinculado a um email e uma rota (origem + destino)
- Preço alvo deve ser maior que zero
- Email deve ser válido (conter '@')
- Job de verificação de alertas executa a cada 2 horas

### RN-04: Histórico de Preços
- Preços são salvos automaticamente a cada coleta de scraping
- Histórico retorna todos os pontos de preço de um voo ordenados por data

### RN-05: Aeroportos e Rotas
- Aeroportos são identificados por código IATA (3 letras)
- Rotas são pares origem-destino únicos
- Autocomplete de aeroportos funciona com mínimo de 2 caracteres

---

## ✅ Critérios de Aceitação (BDD)

### FC-01 — Buscar Passagens

```
Dado que o viajante informa origem (GRU), destino (REC) e data de partida (válida)
Quando ele envia a requisição GET /api/voos/busca?origem=GRU&destino=REC&dataPartida=2026-06-15
Então o sistema deve retornar HTTP 200 com lista de voos ordenados por preço

Dado que o viajante informa uma data no passado
Quando a requisição é enviada
Então o sistema deve retornar HTTP 400 com mensagem de erro
```

### FC-05 — Criar Alerta de Preço

```
Dado que o viajante informa email válido, origem, destino e preço alvo
Quando ele envia POST /api/alertas
Então o sistema deve retornar HTTP 201 com os detalhes do alerta

Dado que o viajante informa email inválido (sem @)
Quando a requisição é enviada
Então o sistema deve retornar HTTP 400 com mensagem de erro
```

### FC-06 — Histórico de Preços

```
Dado que um voo existe e possui histórico de preços
Quando o viajante envia GET /api/voos/precos/{vooId}
Então o sistema deve retornar HTTP 200 com o histórico de preços do voo

Dado que o voo não existe
Quando a requisição é enviada
Então o sistema deve retornar HTTP 404
```

---

## 🏁 Definition of Done (DoD)

- [ ] FC-01: Busca de voos funcionando com cache, scrapers e mock fallback
- [ ] FC-02: Preço total incluindo taxas exibido nos resultados
- [ ] FC-03: Filtros por companhia, paradas e horário implementados no frontend
- [ ] FC-04: Ordenação por preço, duração ou horário disponível
- [ ] FC-05: Criação e listagem de alertas de preço funcionando
- [ ] FC-06: Histórico de preços disponível por voo
- [ ] FC-07: Cache de resultados implementado (memória + Redis opcional)
- [ ] FC-08: Jobs de scraping agendados com Hangfire
