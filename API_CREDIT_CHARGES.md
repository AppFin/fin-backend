# API de Credit Charges - Documentação Técnica

## Visão Geral
API para gerenciar lançamentos de cartão de crédito (Credit Charges) com suporte a parcelas, categorias e divisão entre pessoas.

**Base URL:** `/credit-charges`  
**Autenticação:** Bearer Token (obrigatório em todos os endpoints)

---

## Endpoints

### 1. Listar Credit Charges
```
GET /credit-charges
```

**Descrição:** Retorna lista paginada de credit charges com filtros opcionais.

**Query Parameters:**

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `page` | int | Não | Número da página (padrão: 1) |
| `pageSize` | int | Não | Itens por página (padrão: 10) |
| `sortBy` | string | Não | Campo para ordenação (ex: `date`, `value`) |
| `sortDirection` | string | Não | `Asc` ou `Desc` (padrão: `Desc`) |
| `categoryIds` | array[guid] | Não | Filtrar por categorias (múltiplas) |
| `categoryOperator` | string | Não | `And` ou `Or` para operador lógico entre categorias |
| `personIds` | array[guid] | Não | Filtrar por pessoas (múltiplas) |
| `personOperator` | string | Não | `And` ou `Or` para operador lógico entre pessoas |
| `creditCardIds` | array[guid] | Não | Filtrar por cartões de crédito |
| `dateFrom` | datetime | Não | Data inicial (formato: `YYYY-MM-DD`) |
| `dateTo` | datetime | Não | Data final (formato: `YYYY-MM-DD`) |

**Response (200):**
```json
{
  "pageIndex": 1,
  "pageSize": 10,
  "totalCount": 25,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "description": "Amazon Purchase",
      "value": 250.50,
      "numberOfInstallments": 3,
      "date": "2026-06-15T10:30:00Z",
      "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
      "creditChargeCategoriesIds": [
        "770e8400-e29b-41d4-a716-446655440002"
      ],
      "creditChargePeople": [
        {
          "personId": "880e8400-e29b-41d4-a716-446655440003",
          "financialSplit": 100.0
        }
      ]
    }
  ]
}
```

---

### 2. Obter Credit Charge por ID
```
GET /credit-charges/{id}
```

**Parâmetros:**

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `id` | guid | Sim | ID do credit charge |

**Response (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "description": "Amazon Purchase",
  "value": 250.50,
  "numberOfInstallments": 3,
  "date": "2026-06-15T10:30:00Z",
  "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
  "creditChargeCategoriesIds": [
    "770e8400-e29b-41d4-a716-446655440002"
  ],
  "creditChargePeople": [
    {
      "personId": "880e8400-e29b-41d4-a716-446655440003",
      "financialSplit": 100.0
    }
  ]
}
```

**Response (404):** Quando o credit charge não é encontrado.

---

### 3. Criar Credit Charge
```
POST /credit-charges
```

**Descrição:** Cria um novo credit charge. Automaticamente:
- Gera N parcelas (Installments) conforme `numberOfInstallments`
- Cria ou atualiza a fatura (CardBilling) do cartão
- Cria ou atualiza o título de pagamento (Title) associado

**Body (JSON):**
```json
{
  "value": 250.50,
  "description": "Amazon Purchase",
  "date": "2026-06-15T10:30:00Z",
  "numberOfInstallments": 3,
  "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
  "creditChargeCategoriesIds": [
    "770e8400-e29b-41d4-a716-446655440002",
    "770e8400-e29b-41d4-a716-446655440003"
  ],
  "creditChargePeople": [
    {
      "personId": "880e8400-e29b-41d4-a716-446655440004",
      "financialSplit": 50.0
    },
    {
      "personId": "880e8400-e29b-41d4-a716-446655440005",
      "financialSplit": 50.0
    }
  ]
}
```

**Validações Executadas:**

| Validação | Erro | Descrição |
|-----------|------|-----------|
| Descrição vazia | `DescriptionIsRequired` | Campo obrigatório |
| Descrição > 100 caracteres | `DescriptionTooLong` | Máximo 100 caracteres |
| Valor <= 0 | `ValueMustBeGreaterThanZero` | Deve ser maior que zero |
| Parcelas < 1 | `NumberOfInstallmentsMustBePositive` | Mínimo 1 parcela |
| Cartão não existe | `CreditCardNotFound` | ID do cartão inválido |
| Cartão inativo | `CreditCardInactive` | Cartão não está ativo |
| Categorias não existem | `SomeCategoriesNotFound` | Uma ou mais categorias inválidas |
| Categorias inativas | `SomeCategoriesInactive` | Uma ou mais categorias inativas |
| Pessoas não existem | `SomePeopleNotFound` | Uma ou mais pessoas inválidas |
| Pessoas inativas | `SomePeopleInactive` | Uma ou mais pessoas inativas |
| Split fora do intervalo | `PeopleSplitRange` | Split deve estar entre 0 e 100 |

**Response (201):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "description": "Amazon Purchase",
  "value": 250.50,
  "numberOfInstallments": 3,
  "date": "2026-06-15T10:30:00Z",
  "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
  "creditChargeCategoriesIds": [
    "770e8400-e29b-41d4-a716-446655440002"
  ],
  "creditChargePeople": [
    {
      "personId": "880e8400-e29b-41d4-a716-446655440004",
      "financialSplit": 50.0
    }
  ]
}
```

**Response (422 - Validation Error):**
```json
{
  "success": false,
  "errorCode": "CreditCardNotFound",
  "errorMessage": "Credit card not found",
  "data": null
}
```

---

### 4. Atualizar Credit Charge
```
PUT /credit-charges/{id}
```

**Descrição:** Atualiza um credit charge existente. Reprocessa:
- Parcelas (Installments) — regeneradas conforme novo `numberOfInstallments`
- Fatura (CardBilling) — atualiza valores
- Título de pagamento (Title) — sincroniza com novo valor

**Parâmetros:**

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `id` | guid | Sim | ID do credit charge a atualizar |

**Body (JSON):** Mesmo formato da criação.

**Validações Executadas:** Mesmas da criação, mais:

| Validação | Erro | Descrição |
|-----------|------|-----------|
| Credit charge não existe | `CreditChargeNotFound` | ID inválido ou já deletado |

**Response (200):** Sucesso (sem corpo).

**Response (404):** Quando o credit charge não existe.

**Response (422):** Erro de validação (mesmo formato da criação).

---

### 5. Deletar Credit Charge
```
DELETE /credit-charges/{id}
```

**Descrição:** Deleta um credit charge. Automaticamente:
- Remove todas as parcelas (Installments) associadas
- Reprocessa a fatura (CardBilling)
- Reprocessa o título de pagamento (Title)

**Parâmetros:**

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `id` | guid | Sim | ID do credit charge a deletar |

**Validações Executadas:**

| Validação | Erro | Descrição |
|-----------|------|-----------|
| Credit charge não existe | `CreditChargeNotFound` | ID inválido ou já deletado |

**Response (200):** Sucesso (sem corpo).

**Response (404):** Quando o credit charge não existe.

---

## Estrutura de Dados

### CreditChargeInput (Request Body)
```typescript
{
  value: number;              // Valor total da compra (ex: 250.50)
  description: string;        // Descrição (máx 100 caracteres)
  date: string (ISO 8601);   // Data da compra (YYYY-MM-DDTHH:mm:ssZ)
  numberOfInstallments: number; // Número de parcelas (padrão: 1, mín: 1)
  creditCardId: string (GUID); // ID do cartão de crédito
  creditChargeCategoriesIds: string[] (GUID[]); // Categorias associadas
  creditChargePeople: {
    personId: string (GUID);  // ID da pessoa
    financialSplit: number;   // Percentual (0-100)
  }[]
}
```

### CreditChargeOutput (Response)
```typescript
{
  id: string (GUID);
  description: string;
  value: number;
  numberOfInstallments: number;
  date: string (ISO 8601);
  creditCardId: string (GUID);
  creditChargeCategoriesIds: string[] (GUID[]);
  creditChargePeople: {
    personId: string (GUID);
    financialSplit: number;
  }[]
}
```

### CreditChargeGetListInput (Query Parameters)
```typescript
{
  // Paginação
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: "Asc" | "Desc";
  
  // Filtros
  categoryIds?: string[] (GUID[]);
  categoryOperator?: "And" | "Or";
  personIds?: string[] (GUID[]);
  personOperator?: "And" | "Or";
  creditCardIds?: string[] (GUID[]);
  dateFrom?: string (YYYY-MM-DD);
  dateTo?: string (YYYY-MM-DD);
}
```

### PagedOutput (Response Structure)
```typescript
{
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  data: CreditChargeOutput[]
}
```

### ValidationResult (Error Response)
```typescript
{
  success: false;
  errorCode: string;  // Código do erro (ex: "CreditCardNotFound")
  errorMessage: string; // Mensagem legível
  data: null
}
```

---

## Códigos de Erro

### Criação/Atualização (CreditChargeCreateOrUpdateErrorCode)

| Código | HTTP | Mensagem |
|--------|------|---------|
| `CreditChargeNotFound` | 404 | Credit charge not found |
| `DescriptionTooLong` | 422 | Description must have less than 100 characters. |
| `DescriptionIsRequired` | 422 | Description is required. |
| `CreditCardNotFound` | 422 | Credit card not found |
| `CreditCardInactive` | 422 | Credit card is inactive |
| `ValueMustBeGreaterThanZero` | 422 | Value must be greater than zero. |
| `NumberOfInstallmentsMustBePositive` | 422 | Number of installments must be at least 1. |
| `SomeCategoriesNotFound` | 422 | Some categories was not found |
| `SomeCategoriesInactive` | 422 | Some categories is inactive |
| `SomePeopleNotFound` | 422 | Some people was not found |
| `SomePeopleInactive` | 422 | Some people is inactive |
| `PeopleSplitRange` | 422 | Financial split between people must be greater than or equal to 0 and less than or equal to 100 |

### Deleção (CreditChargeDeleteErrorCode)

| Código | HTTP | Mensagem |
|--------|------|---------|
| `CreditChargeNotFound` | 404 | Credit charge not found |

---

## Comportamentos Especiais

### Geração de Parcelas
Quando um credit charge é criado com `numberOfInstallments > 1`:
- O sistema divide o valor total em N parcelas iguais
- A última parcela recebe o restante (ajuste de centavos)
- Cada parcela tem uma data de vencimento calculada com base na data da compra

**Exemplo:**
- Valor: 250.50, Parcelas: 3
- Parcela 1: 83.50 (15/07)
- Parcela 2: 83.50 (15/08)
- Parcela 3: 83.50 (15/09)

### Atualização de Fatura
Quando um credit charge é criado/atualizado:
1. **Se não existe fatura (CardBilling) no período:** 
   - Sistema cria automaticamente uma nova `CardBilling`
   - Cria um novo `Title` (título de pagamento) associado
   
2. **Se já existe fatura no período:**
   - Sistema atualiza o valor da fatura
   - Atualiza o `Title` associado
   - Reprocessa saldos das carteiras afetadas

### Deleção com Reprocessamento
Ao deletar um credit charge:
- Todas as parcelas (Installments) são removidas
- A fatura (CardBilling) é reprocessada com novos valores
- O `Title` é atualizado ou removido conforme necessário

---

## Exemplos de Uso (cURL)

### Listar com filtros
```bash
curl -X GET "http://localhost:5000/credit-charges?page=1&pageSize=10&dateFrom=2026-01-01&dateTo=2026-12-31&categoryOperator=Or" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json"
```

### Criar credit charge
```bash
curl -X POST "http://localhost:5000/credit-charges" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "value": 250.50,
    "description": "Amazon Purchase",
    "date": "2026-06-15T10:30:00Z",
    "numberOfInstallments": 3,
    "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
    "creditChargeCategoriesIds": ["770e8400-e29b-41d4-a716-446655440002"],
    "creditChargePeople": [
      {
        "personId": "880e8400-e29b-41d4-a716-446655440004",
        "financialSplit": 100.0
      }
    ]
  }'
```

### Atualizar credit charge
```bash
curl -X PUT "http://localhost:5000/credit-charges/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "value": 300.00,
    "description": "Amazon Purchase Updated",
    "date": "2026-06-15T10:30:00Z",
    "numberOfInstallments": 4,
    "creditCardId": "660e8400-e29b-41d4-a716-446655440001",
    "creditChargeCategoriesIds": ["770e8400-e29b-41d4-a716-446655440002"],
    "creditChargePeople": [
      {
        "personId": "880e8400-e29b-41d4-a716-446655440004",
        "financialSplit": 100.0
      }
    ]
  }'
```

### Deletar credit charge
```bash
curl -X DELETE "http://localhost:5000/credit-charges/550e8400-e29b-41d4-a716-446655440000" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## Headers Comuns

Todos os requests devem incluir:

```
Authorization: Bearer {token}
Content-Type: application/json
```

---

## Status HTTP

| Status | Significado |
|--------|------------|
| 200 | Sucesso (GET/PUT/DELETE) |
| 201 | Criado com sucesso (POST) |
| 404 | Recurso não encontrado |
| 422 | Erro de validação |
| 500 | Erro interno do servidor |

---

**Última atualização:** 18 de junho de 2026

