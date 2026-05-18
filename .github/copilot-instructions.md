# Copilot Instructions for Fin-Backend Project
Este documento fornece instruções detalhadas para o GitHub Copilot ao trabalhar com o projeto Fin-Backend. Segue os padrões e melhores práticas estabelecidos pela equipe.
---
## 📋 Visão Geral do Projeto
**Fin-Backend** é um sistema de gerenciamento financeiro construído em C# .NET 8+ com arquitetura em camadas:
- **Domain Layer**: Entidades com lógica de negócio encapsulada
- **Application Layer**: Serviços de orquestração com validação centralizada
- **Infrastructure Layer**: Acesso a dados via EF Core e validações
- **API Layer**: Controladores thin que mapeiam resultados para HTTP
Arquitetura: **Multi-tenant**, **Auditada**, **Domain-Driven Design**
---
## 🏗️ Padrões Arquiteturais
### 1. Domain Layer - Encapsulamento e Integridade
#### Entidades
- **Propriedades**: Use `private set` para todas as propriedades
- **State Mutation**: Apenas através de métodos públicos na própria entidade
- **Lógica de Negócio**: Métodos de cálculo e validação pertencem à entidade
#### Relacionamentos Many-to-Many
- Sincronização de relacionamentos deve estar na entidade proprietária
- Método `SyncCategories()` que gerencia adições e deleções
### 2. Application Layer - Fluxo de Controle
#### Services
- **Interface**: Todo serviço deve implementar uma interface dedicada (`IWalletService`)
- **SRP**: Foco em orquestração (Transação, Persistência, Fluxo de Controle)
- **Delegação**: Validação → `IValidationService`, Lógica Complexa → Serviços Especializados
- **Resultado**: Retorne `ValidationResultDto<TSuccess, TErrorCode>`
#### Query Projection
- Minimize consumo de memória: Use `.Select(n => new OutputDTO(n))` direto em `IQueryable<T>`
- Materializar apenas após projeção: `.ToListAsync()`
### 3. Validação Pipeline
#### Validação Modular
- **Interface**: `IValidationRule<TInput, TErrorCode>` ou `IValidationRule<TInput, TErrorCode, TErrorData>`
- **Discovery**: Regras registradas em DI, descobertas automaticamente pelo tipo de entrada
- **Fail-Fast**: Retorna no primeiro erro
#### Regras com Dados de Erro
- Para retornar dados auxiliares (ex: lista de IDs que falharam)
- Use `ValidationPipelineOutput<TErrorCode, TErrorData>`
### 4. Infrastructure Layer
#### EF Core Configuration
- **Financial Fields**: Alta precisão `numeric(19,4)` com `HasPrecision(19, 4)`
- **Uniqueness**: Índices únicos compostos para isolamento tenant
- **Foreign Keys**: `OnDelete(DeleteBehavior.Restrict)` para entidades financeiras
#### Repository Pattern
- **Read-Only**: Use `repository.AsNoTracking()` (melhor performance)
- **Write**: Use `repository` diretamente (tracking ativado)
- **Batch Operations**: Use `AddRangeAsync()`, não loops
#### AutoSave Parameter
- **`true`**: Operação única simples, Testes
- **`false`**: Múltiplas operações relacionadas, Transações
### 5. API Controllers
Controllers são thin wrappers que apenas mapeiam resultados para HTTP:
- Status 200/201 para sucesso
- Status 422 para erro de validação
- Status 404 para NotFound específicos
---
## 🧪 Padrões de Testing
### Base Classes
```csharp
// Entity test - sem banco de dados
public class MyEntityTest { }
// Service test - com banco de dados
public class MyServiceTest : TestUtils.BaseTestWithContext { }
// Controller test - sem banco de dados
public class MyControllerTest : TestUtils.BaseTest { }
```
### Naming Convention
- Classe: `{EntityName}Test` ou `{ServiceName}Test`
- Método: `{Method}_Should{Behavior}_When{Condition}`
### Test Data
Use `TestUtils` para dados consistentes:
- `TestUtils.Guids[0]` - GUID pré-configurado
- `TestUtils.Strings[0]` - Texto
- `TestUtils.Decimals[0]` - Valor decimal
- `TestUtils.UtcDateTimes[0]` - DateTime UTC
### AAA Pattern
Sempre use: Arrange → Act → Assert
---
## 🔐 Multi-Tenancy e Auditoria
### IAmbientData
Sempre gerencie contexto de usuário via `IAmbientData`:
- `TenantId`: Isolamento multi-tenant
- `UserId`: Identificação do usuário
- `DisplayName`: Nome amigável
- `IsAdmin`: Privilégios administrativos
- `IsLogged`: Status de autenticação
### Interceptores Automáticos
- **TenantEntityInterceptor**: Filtra automaticamente por `TenantId`
- **AuditedEntityInterceptor**: Seta `CreatedBy`, `UpdatedBy`, `CreatedAt`, `UpdatedAt`
---
## 📊 QueryableExtensions
Use extensões para queries dinâmicas:
```csharp
query = query.ApplyFilterAndSorter(input);
return await query.Select(w => new WalletOutput(w)).ToPagedResult(input);
```
---
## ⚠️ ErrorMessage Attribute
Associe mensagens de erro aos enums com `[ErrorMessage("...")]`
---
## 🚀 Best Practices
### DO ✅
1. **Entity Encapsulation**: `public string Name { get; private set; }`
2. **Use ValidationResultDto**: `.WithSuccess(wallet)` ou `.WithError(code)`
3. **Query Projection**: `.Select(w => new Output(w)).ToListAsync()`
4. **Repository AsNoTracking**: `.AsNoTracking().Where(...).ToListAsync()`
5. **Check IsLogged**: Valide antes de usar dados do usuário
6. **Use Test Data**: `TestUtils.Strings[0]`
7. **Mock External Services**: `new Mock<IService>()`
### DO NOT ❌
1. **Public Setters**: `public string Name { get; set; }`
2. **Tracking on Read**: Sempre use `AsNoTracking()`
3. **Mutation Outside Entity**: Não faça `wallet.Name = "..."`
4. **Direct Query.Query()**: OBSOLETO, use `AsNoTracking()`
5. **Multiple autoSave: true**: Use `false` + `SaveChangesAsync()`
6. **Load All for Validation**: Use `AnyAsync()` com Where
7. **SetData in Services**: Apenas autenticação/middleware
---
## 🔄 Transaction Pattern
Para operações financeiras complexas:
```csharp
await using (var scope = await _unitOfWork.BeginTransactionAsync(ct))
{
    await _repository.UpdateAsync(e1, autoSave: false);
    await _repositoryOther.AddAsync(e2, autoSave: false);
    await _unitOfWork.SaveChangesAsync(ct);
}
```
---
## 📝 Code Review Checklist
- [ ] Entidades têm `private set` nas propriedades?
- [ ] Serviços retornam `ValidationResultDto`?
- [ ] Validação usa `IValidationRule`?
- [ ] Read queries usam `AsNoTracking()`?
- [ ] Controllers são thin wrappers?
- [ ] Testes seguem AAA pattern?
- [ ] Dados de teste usam `TestUtils`?
- [ ] Queries são projetadas cedo (`.Select()`)?
- [ ] IAmbientData verificado antes de usar?
---
## 🔗 Referências Internas
Consulte os documentos de AI para mais detalhes:
- `ai_docs/best_pratices_and_standards.md`
- `ai_docs/i_repository.md`
- `ai_docs/i_ambient_data.md`
- `ai_docs/validation_pipeline.md`
- `ai_docs/test_best_practices_and_standards.md`
- `ai_docs/test_utils.md`
- `ai_docs/queryable_extensions.md`
---
**Última atualização**: 17 de maio de 2026
