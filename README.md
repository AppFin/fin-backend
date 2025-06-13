[Em portugês](./assets/README_pt-br.md)

## Overview
FinApp is a modern, intuitive personal finance management solution designed to help young adults and teenagers take control of their finances. This backend provides secure, scalable APIs powering Fin’s web and mobile experiences, replacing traditional spreadsheets with a more engaging and accessible digital approach.

----
## Purpose
Deliver a platform that makes financial management simple, fast, and attractive, promoting financial education through a gamified and accessible experience. Fin is the “Duolingo for finance,” focused on building financial awareness with a friendly interface and practical features.

---
## Target Audience
- Primary: Young adults (18-25) and teenagers (16-18);
- Users transitioning to financial independence, digital natives seeking simplicity, speed, and mobility.

---
## Value Proposition
- **Simplicity**: Clean, intuitive interface;
- **Speed**: Register transactions in seconds;
- **Education**: Integrated financial insights and tips (_doing..._); 
- **Mobility**: Access anywhere, anytime;

---
## Project Differentials
- Full focus on young users, with appropriate language and design;
- Ultra-fast transaction recording;
- Minimalist interface to reduce friction;
- Integrated financial education in a non-intrusive way;

---
## Expected Benefits
- Increased awareness of financial habits;
- Reduction of unnecessary expenses;
- Development of financial discipline;
- Easy goal tracking;

---
## Backend Scope
### Includes:

- User registration and authentication;
- Management of transactions (income, expenses, transfers) (_doing..._);
- Budget and goal tracking (_doing..._);
- Financial reports and insights (_doing..._);
- RESTful API for web/mobile integration (_doing..._);
- Interactive documentation via Swagger;

### Does not include:

- Automatic bank integrations;
- Advanced investment features;
- Business or social features;
- Direct financial services;

## Tech Stack
- **Framework**: .NET 9 (ASP.NET Core Web API);
- **ORM**: Entity Framework Core;
- **Tests**: FluentAssertions + :moq:MOQ  + :xunit:xUnit;
- **Api Documentation**: Swagger/OpenAPI;
- **Database**: PostgreSQL or SQLite (for tests);
- **BackgroudJobs**: Hangfire;
- **Mail send**: MailKit;
- **WebSocket**: SignalR;
- **Storage**: Supbase;
- **Cache**: Redis;
- **PushNotification mobile**: Firebase;


---
## How to Run

* Prerequisites: .NET 9 SDK, PostgreSQL, Git, Firebase account, Supabase account, Redis and Google App password;
* Clone repository:
``` bash
git clone https://github.com/AppFin/fin-backend.git
cd fin-backend
```
* Restore dependencies;
* Configure [appsettings.json](./Fin.Api/appsettings.json) based on exemples;
* Restore dependencies: ```dotnet restore```;
* Build: ```dotnet build```;
* Run: ```dotnet run --project .\Fin.Api\```;

The API will start, and you’ll see output indicating the URLs where it is listening (e.g., http://localhost:5045)

### Access Swagger UI
Open your browser and navigate to: [http://localhost:5045/swagger/index.html](http://localhost:5045/swagger/index.html);

This interactive documentation lets you explore and test the API endpoints directly.

## API Documentation
All endpoints are documented and testable via the built-in Swagger UI.

For more details, visit /swagger after running the project


## License
MIT

---
_This README reflects the vision, scope, and unique value of the Fin project, providing clear guidance for developers and contributors._
