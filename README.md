# Task Manager API

A simple REST API for task management built with .NET 8 and Redis.

## Features
- ✅ CRUD operations for tasks
- ✅ In-memory storage (will add Redis soon)
- ✅ Swagger API documentation
- ✅ RESTful design

## API Endpoints
- `GET /tasks` - Get all tasks
- `GET /tasks/{id}` - Get task by ID
- `POST /tasks` - Create new task
- `PUT /tasks/{id}` - Update task
- `DELETE /tasks/{id}` - Delete task

## How to Run
1. Install .NET 8 SDK
2. Clone this repository
3. Run `dotnet restore`
4. Run `dotnet run`
5. Open `http://localhost:5178/swagger`

## Tech Stack
- .NET 8
- ASP.NET Core Web API
- Swagger/OpenAPI
- Redis (coming soon)

## Next Features
- [ ] Redis integration
- [ ] JWT Authentication
- [ ] Rate limiting
- [ ] Task filtering and search