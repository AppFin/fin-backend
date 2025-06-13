[In english](../README.md)

## Visão Geral

FinApp é uma solução moderna e intuitiva de gerenciamento de finanças pessoais, projetada para ajudar jovens adultos e adolescentes a assumirem o controle de suas finanças. Este backend fornece APIs seguras e escaláveis que alimentam as experiências web e mobile do Fin, substituindo planilhas tradicionais por uma abordagem digital mais envolvente e acessível.

---

## Propósito

Entregar uma plataforma que torne o gerenciamento financeiro simples, rápido e atraente, promovendo a educação financeira por meio de uma experiência gamificada e acessível. O Fin é o “Duolingo das finanças”, focado em construir consciência financeira com uma interface amigável e funcionalidades práticas.

---

## Público-Alvo

* Principal: Jovens adultos (18–25 anos) e adolescentes (16–18 anos);
* Usuários em transição para a independência financeira, nativos digitais que buscam simplicidade, rapidez e mobilidade.

---

## Proposta de Valor

* **Simplicidade**: Interface limpa e intuitiva;
* **Rapidez**: Registre transações em segundos;
* **Educação**: Dicas e insights financeiros integrados (*em desenvolvimento...*);
* **Mobilidade**: Acesse de qualquer lugar, a qualquer hora;

---

## Diferenciais do Projeto

* Foco total no público jovem, com linguagem e design adequados;
* Registro ultra-rápido de transações;
* Interface minimalista para reduzir fricção;
* Educação financeira integrada de forma não intrusiva;

---

## Benefícios Esperados

* Maior consciência sobre hábitos financeiros;
* Redução de gastos desnecessários;
* Desenvolvimento de disciplina financeira;
* Facilidade no acompanhamento de metas;

---

## Escopo do Backend

### Inclui:

* Registro e autenticação de usuários;
* Gerenciamento de transações (receitas, despesas, transferências) (*em desenvolvimento...*);
* Acompanhamento de orçamentos e metas (*em desenvolvimento...*);
* Relatórios e insights financeiros (*em desenvolvimento...*);
* API RESTful para integração web/mobile (*em desenvolvimento...*);
* Documentação interativa via Swagger;

### Não inclui:

* Integrações bancárias automáticas;
* Funcionalidades avançadas de investimento;
* Funcionalidades de rede social ou corporativas;
* Serviços financeiros diretos;

---

## Stack Tecnológica

* **Framework**: .NET 9 (ASP.NET Core Web API);
* **ORM**: Entity Framework Core;
* **Testes**: FluentAssertions + \:moq\:MOQ + \:xunit\:xUnit;
* **Documentação da API**: Swagger/OpenAPI;
* **Banco de dados**: PostgreSQL ou SQLite (para testes);
* **Tarefas em segundo plano**: Hangfire;
* **Envio de e-mails**: MailKit;
* **WebSocket**: SignalR;
* **Armazenamento**: Supabase;
* **Cache**: Redis;
* **Notificações push no mobile**: Firebase;

---

## Como Rodar

* Pré-requisitos: .NET 9 SDK, PostgreSQL, Git, conta Firebase, conta Supabase, Redis e senha de aplicativo do Google;
* Clone o repositório:

```bash
git clone https://github.com/AppFin/fin-backend.git
cd fin-backend
```

* Restaure as dependências;
* Configure o [appsettings.json](./Fin.Api/appsettings.json) com base nos exemplos;
* Restaure as dependências: `dotnet restore`;
* Compile: `dotnet build`;
* Rode: `dotnet run --project .\Fin.Api\`;

A API será iniciada e exibirá no console os endereços onde está escutando (ex.: [http://localhost:5045](http://localhost:5045))

### Acessar Swagger UI

Abra seu navegador e vá até: [http://localhost:5045/swagger/index.html](http://localhost:5045/swagger/index.html);

Esta documentação interativa permite explorar e testar os endpoints da API diretamente.

---

## Documentação da API

Todos os endpoints estão documentados e podem ser testados via Swagger UI embutido.

Para mais detalhes, acesse `/swagger` após rodar o projeto.

---

## Licença

MIT

---

*Este README reflete a visão, o escopo e os diferenciais do projeto Fin, fornecendo orientação clara para desenvolvedores e colaboradores.*
