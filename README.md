# 🌿 EcoTrack — Gestão de Condicionantes de Licenciamento Ambiental

Sistema Full-Stack desenvolvido em **C# (.NET)** e **Angular** para o controle operacional e preventivo de licenças ambientais (LAP, LAI, LAO, AuA), monitoramento de condicionantes, cálculo automático da janela de renovação legal (CONAMA 237/97) e gestão de evidências comprobatórias.

## 📋 Sobre o Problema e Valor Ambiental

Empreendimentos sujeitos ao licenciamento ambiental (indústrias, obras de infraestrutura, saneamento, agronegócio) precisam cumprir dezenas de condicionantes técnicas e administrativas exigidas pelos órgãos ambientais (IMA, IBAMA, secretarias municipais).

O controle manual via planilhas expõe as empresas a riscos críticos, como multas, embargo de atividades e perda da renovação automática da licença. O **EcoTrack** resolve esse gargalo através de:

* **Bloqueio de Conclusão sem Evidência:** Garantia de conformidade técnica — uma condicionante só pode ser concluída após o upload de ao menos um documento comprobatório (ART, laudo laboratorial, relatório fotográfico ou protocolo).

* **Alerta Preventivo de Renovação (CONAMA 237/97):** Cálculo automático da data limite de 120 dias de antecedência para protocolo de renovação da licença.

* **Semáforo de Risco em Tempo Real:** Classificação visual automática (`Normal`, `Alerta`, `Crítico`, `Concluído`) baseada nos prazos de vencimento.

* **Varredura Automatizada:** Serviço em segundo plano (*Background Worker*) que atualiza diariamente o status de condicionantes vencidas.

## 🛠️ Stack Tecnológica

### Back-end (`server`)

* **C# / .NET 8+** (ASP.NET Core Web API)

* **Entity Framework Core** (Persistência e Migrations)

* **PostgreSQL** ou **SQL Server**

* **FluentValidation** (Validação de regras de entrada)

* **xUnit** (Testes unitários das regras de domínio)

* **Swagger / OpenAPI** (Documentação interativa da API)

### Front-end (`client`)

* **Angular 17+** (Standalone Components)

* **Angular Signals** (`signal`, `computed` para reatividade do dashboard e filtros)

* **Reactive Forms** (Formulários tipados e validação de arquivos)

* **TypeScript / SCSS**

## 🏗️ Arquitetura do Projeto

O back-end segue os princípios de **Clean Architecture** e **DDD Pragmático**, isolando as regras de negócio da Engenharia Ambiental de detalhes de infraestrutura:

```
EcoTrack/
├── server/
│   ├── src/
│   │   ├── EcoTrack.Domain/          # Entidades ricas, Enums, Exceções de Domínio e Interfaces
│   │   ├── EcoTrack.Application/     # Services, DTOs, Validadores e IFileStorageService
│   │   ├── EcoTrack.Infrastructure/  # AppDbContext, Migrations, Storage local, Worker e Seeder
│   │   └── EcoTrack.WebApi/          # Controllers REST, Middlewares (ProblemDetails) e Program.cs
│   └── tests/
│       └── EcoTrack.Domain.Tests/    # Testes unitários das regras de licenciamento
└── client/
    └── src/app/
        ├── core/                     # Models, Services HTTP e Interceptors
        ├── features/
        │   ├── dashboard/            # KPIs, Semáforo de Risco e Vencimentos Críticos
        │   ├── empreendimentos/      # Cadastro e listagem de unidades operacionais
        │   └── licencas/             # Detalhes da licença, condicionantes e upload de evidências
        └── shared/                   # Badges de status, pipes e componentes reutilizáveis
```

## 🗄️ Modelo de Dados (Domínio)

```
[Empreendimento] 1 ──────< N [Licenca] 1 ──────< N [Condicionante] 1 ──────< N [Evidencia]
```

* **`Empreendimento`:** Unidade física sujeita ao licenciamento (Razão Social, CNPJ, Município/UF, Código CNAE/CONSEMA).

* **`Licenca`:** Ato administrativo (Número do Processo, Número da Licença, Órgão Emissor, Tipo `LAP/LAI/LAO/AuA`, `DateOnly` de Emissão e Validade, Antecedência de Renovação padrão de `120 dias`).

* **`Condicionante`:** Obrigação específica vinculada à licença (Código do Item, Título, Periodicidade, Status, `DateOnly` de Vencimento, Responsável Técnico).

* **`Evidencia`:** Metadados do documento anexado (Nome original, Caminho seguro no storage, Content-Type, Tamanho, Número da ART e Data UTC de upload).

## 🚀 Como Executar Localmente

### Pré-requisitos

* [.NET SDK 8.0+](https://dotnet.microsoft.com/download)

* [Node.js 20+](https://nodejs.org/) e Angular CLI (`npm install -g @angular/cli`)

* PostgreSQL ou SQL Server rodando localmente (ou via Docker)

### 1. Configurando e rodando o Back-end

```bash
# Acesse a pasta do servidor
cd server

# Restaure as dependências
dotnet restore

# Aplique as migrations no banco de dados
dotnet ef database update --project src/EcoTrack.Infrastructure --startup-project src/EcoTrack.WebApi

# Inicie a API (o Seeder populará dados iniciais automaticamente em ambiente Development)
dotnet run --project src/EcoTrack.WebApi
```

> A documentação interativa da API estará disponível em `https://localhost:5001/swagger` (ou porta configurada no `launchSettings.json`).

### 2. Configurando e rodando o Front-end

```bash
# Em outro terminal, acesse a pasta do cliente
cd client

# Instale as dependências
npm install

# Inicie o servidor de desenvolvimento
ng serve
```

> Acesse a aplicação no navegador em `http://localhost:4200`.

### 3. Executando os Testes Unitários

```bash
cd server
dotnet test
```

## 🌱 Dados de Demonstração (Database Seeder)

Para permitir o uso imediato sem depender de documentos reais ou sigilosos, o sistema conta com um **Seeder automático** em ambiente de desenvolvimento que cria:

* **2 Empreendimentos fictícios** (*Madeireira Vale Verde Ltda.* e *Frigorífico Serra Catarinense S.A.*).

* **3 Licenças** (LAO vigente normal, LAO na janela crítica de 120 dias para renovação e LAI).

* **8 Condicionantes** distribuídas entre `Concluída` (com arquivo PDF de exemplo anexado), `Normal`, `Alerta` (vencendo em menos de 30 dias) e `Atrasada`.

## 🗺️ Roadmap (Próximas Evoluções)

* [ ] **v1.0 (MVP):** CRUD completo, upload de evidências com vínculo de ART, dashboard reativo e worker de vencimentos.

* [ ] **v1.1:** Clonagem automática de condicionantes periódicas (Mensal/Semestral/Anual) ao concluir o ciclo atual.

* [ ] **v1.2:** Exportação de Relatório de Conformidade Ambiental em PDF.

* [ ] **v2.0:** Módulo acoplado de Automonitoramento de Efluentes (enquadramento Resolução CONAMA 430).

## 📄 Licença

Este projeto está sob a licença MIT. Consulte o arquivo `LICENSE` para mais detalhes.