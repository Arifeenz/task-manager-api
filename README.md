# Task Manager - Full Stack Web Application

A complete task management system built with .NET 8, Redis, and vanilla JavaScript.

## 🚀 Features
- ✅ **Full CRUD Operations** - Create, Read, Update, Delete tasks
- ✅ **Redis Persistence** - Data survives server restarts
- ✅ **Web UI** - Beautiful responsive frontend
- ✅ **Real-time Statistics** - Task completion rates and metrics
- ✅ **Advanced Filtering** - Search and filter by status
- ✅ **RESTful API** - Well-designed API endpoints
- ✅ **Swagger Documentation** - Interactive API docs

## 🎯 Demo
- **Web UI**: `http://localhost:5178/`
- **API Docs**: `http://localhost:5178/swagger`

## 🛠️ Tech Stack
- **Backend**: .NET 8, ASP.NET Core Web API
- **Database**: Redis (in-memory)
- **Frontend**: HTML5, CSS3, Vanilla JavaScript
- **Documentation**: Swagger/OpenAPI

## 📱 Screenshots
- Mobile-friendly responsive design
- Real-time task statistics
- Advanced search and filtering

## 🚀 Quick Start
1. Install .NET 8 SDK
2. Install Redis server
3. Clone this repository
4. Run `dotnet restore`
5. Run `dotnet run`
6. Open `http://localhost:5178`

## 📋 API Endpoints
- `GET /tasks` - Get all tasks (with filtering)
- `POST /tasks` - Create new task
- `PUT /tasks/{id}` - Update task
- `DELETE /tasks/{id}` - Delete task
- `GET /tasks/statistics` - Get task statistics
- `POST /tasks/bulk` - Bulk create tasks

## 🔄 Next Steps
- [ ] Deploy to Azure
- [ ] Add user authentication
- [ ] Task priorities and due dates
- [ ] File attachments