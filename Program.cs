using TaskManagerAPI.Models;
using TaskManagerAPI.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    ConnectionMultiplexer.Connect(redisConnectionString));

// Register Cache Service
builder.Services.AddScoped<ICacheService, RedisCacheService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// GET all tasks
app.MapGet("/tasks", async (ICacheService cache) =>
{
    var tasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    return Results.Ok(tasks);
})
.WithName("GetTasks")
.WithOpenApi();

// GET task by ID
app.MapGet("/tasks/{id}", async (string id, ICacheService cache) =>
{
    var task = await cache.GetAsync<TaskItem>($"task_{id}");
    return task is not null ? Results.Ok(task) : Results.NotFound();
})
.WithName("GetTask")
.WithOpenApi();

// POST create task
app.MapPost("/tasks", async (TaskItem task, ICacheService cache) =>
{
    // สร้าง ID และเวลา
    task.Id = Guid.NewGuid().ToString();
    task.CreatedAt = DateTime.UtcNow;
    
    // บันทึก task แต่ละตัว
    await cache.SetAsync($"task_{task.Id}", task, TimeSpan.FromDays(30));
    
    // อัปเดต list ของ tasks ทั้งหมด
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    allTasks.Add(task);
    await cache.SetAsync("all_tasks", allTasks, TimeSpan.FromDays(30));
    
    return Results.Created($"/tasks/{task.Id}", task);
})
.WithName("CreateTask")
.WithOpenApi();

// PUT update task
app.MapPut("/tasks/{id}", async (string id, TaskItem updatedTask, ICacheService cache) =>
{
    var task = await cache.GetAsync<TaskItem>($"task_{id}");
    if (task is null) return Results.NotFound();
    
    // อัปเดตข้อมูล
    task.Title = updatedTask.Title;
    task.Description = updatedTask.Description;
    task.IsCompleted = updatedTask.IsCompleted;
    
    // บันทึกกลับ Redis
    await cache.SetAsync($"task_{id}", task, TimeSpan.FromDays(30));
    
    // อัปเดต list ด้วย
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    var index = allTasks.FindIndex(t => t.Id == id);
    if (index >= 0)
    {
        allTasks[index] = task;
        await cache.SetAsync("all_tasks", allTasks, TimeSpan.FromDays(30));
    }
    
    return Results.Ok(task);
})
.WithName("UpdateTask")
.WithOpenApi();

// DELETE task
app.MapDelete("/tasks/{id}", async (string id, ICacheService cache) =>
{
    var task = await cache.GetAsync<TaskItem>($"task_{id}");
    if (task is null) return Results.NotFound();
    
    // ลบ task
    await cache.RemoveAsync($"task_{id}");
    
    // ลบจาก list ด้วย
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    allTasks.RemoveAll(t => t.Id == id);
    await cache.SetAsync("all_tasks", allTasks, TimeSpan.FromDays(30));
    
    return Results.NoContent();
})
.WithName("DeleteTask")
.WithOpenApi();

app.Run();