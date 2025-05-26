using TaskManagerAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// In-memory storage (ชั่วคราว)
var tasks = new List<TaskItem>();

// GET all tasks
app.MapGet("/tasks", () => tasks)
   .WithName("GetTasks")
   .WithOpenApi();

// GET task by ID
app.MapGet("/tasks/{id}", (string id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
})
.WithName("GetTask")
.WithOpenApi();

// POST create task
app.MapPost("/tasks", (TaskItem task) =>
{
    // สร้าง ID ใหม่
    task.Id = Guid.NewGuid().ToString();
    task.CreatedAt = DateTime.UtcNow;
    
    tasks.Add(task);
    return Results.Created($"/tasks/{task.Id}", task);
})
.WithName("CreateTask")
.WithOpenApi();

// PUT update task
app.MapPut("/tasks/{id}", (string id, TaskItem updatedTask) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound();
    
    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.IsCompleted = updatedTask.IsCompleted;
    
    return Results.Ok(task);
})
.WithName("UpdateTask")
.WithOpenApi();

// DELETE task
app.MapDelete("/tasks/{id}", (string id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound();
    
    tasks.Remove(task);
    return Results.NoContent();
})
.WithName("DeleteTask")
.WithOpenApi();

app.Run();