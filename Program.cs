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

// Configure static files
app.UseDefaultFiles();
app.UseStaticFiles();

// GET all tasks with filtering and search
app.MapGet("/tasks", async (ICacheService cache, 
    bool? completed = null, 
    string? search = null, 
    string? sortBy = null) =>
{
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    
    // กรองตาม completed status
    if (completed.HasValue)
    {
        allTasks = allTasks.Where(t => t.IsCompleted == completed.Value).ToList();
    }
    
    // ค้นหาใน title หรือ description
    if (!string.IsNullOrEmpty(search))
    {
        allTasks = allTasks.Where(t => 
            t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }
    
    // เรียงลำดับ
    allTasks = sortBy?.ToLower() switch
    {
        "title" => allTasks.OrderBy(t => t.Title).ToList(),
        "created" => allTasks.OrderBy(t => t.CreatedAt).ToList(),
        "createddesc" => allTasks.OrderByDescending(t => t.CreatedAt).ToList(),
        _ => allTasks.OrderByDescending(t => t.CreatedAt).ToList() // default
    };
    
    return Results.Ok(allTasks);
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

// POST bulk create tasks from JSON array
app.MapPost("/tasks/bulk", async (List<TaskItem> tasks, ICacheService cache) =>
{
    var addedTasks = new List<TaskItem>();
    
    foreach (var task in tasks)
    {
        task.Id = Guid.NewGuid().ToString();
        task.CreatedAt = DateTime.UtcNow;
        await cache.SetAsync($"task_{task.Id}", task, TimeSpan.FromDays(30));
        addedTasks.Add(task);
    }

    // อัปเดต all_tasks list
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    allTasks.AddRange(addedTasks);
    await cache.SetAsync("all_tasks", allTasks, TimeSpan.FromDays(30));

    return Results.Ok(new { message = $"Created {addedTasks.Count} tasks", tasks = addedTasks });
})
.WithName("BulkCreateTasks")
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

// GET task statistics
app.MapGet("/tasks/statistics", async (ICacheService cache) =>
{
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    
    var stats = new
    {
        totalTasks = allTasks.Count,
        completedTasks = allTasks.Count(t => t.IsCompleted),
        pendingTasks = allTasks.Count(t => !t.IsCompleted),
        completionRate = allTasks.Count > 0 ? 
            Math.Round((double)allTasks.Count(t => t.IsCompleted) / allTasks.Count * 100, 2) : 0,
        
        // สถิติเพิ่มเติม
        tasksCreatedToday = allTasks.Count(t => t.CreatedAt.Date == DateTime.UtcNow.Date),
        tasksCreatedThisWeek = allTasks.Count(t => 
            t.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
        
        // Top keywords ใน titles
        topKeywords = allTasks
            .SelectMany(t => t.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(word => word.Length > 2) // เฉพาะคำที่ยาวกว่า 2 ตัวอักษร
            .GroupBy(word => word.ToLower())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count()),
            
        // สถิติตามสถานะ
        tasksByStatus = new
        {
            completed = allTasks.Count(t => t.IsCompleted),
            pending = allTasks.Count(t => !t.IsCompleted)
        },
        
        // Tasks ที่สร้างล่าสุด
        recentTasks = allTasks
            .OrderByDescending(t => t.CreatedAt)
            .Take(3)
            .Select(t => new { t.Id, t.Title, t.CreatedAt, t.IsCompleted })
            .ToList()
    };
    
    return Results.Ok(stats);
})
.WithName("GetTaskStatistics")
.WithOpenApi();

// GET quick summary
app.MapGet("/tasks/summary", async (ICacheService cache) =>
{
    var allTasks = await cache.GetAsync<List<TaskItem>>("all_tasks") ?? new List<TaskItem>();
    
    var summary = new
    {
        message = $"You have {allTasks.Count(t => !t.IsCompleted)} pending tasks out of {allTasks.Count} total tasks",
        completionPercentage = allTasks.Count > 0 ? 
            Math.Round((double)allTasks.Count(t => t.IsCompleted) / allTasks.Count * 100, 1) : 0,
        nextTasks = allTasks
            .Where(t => !t.IsCompleted)
            .OrderBy(t => t.CreatedAt)
            .Take(3)
            .Select(t => t.Title)
            .ToList()
    };
    
    return Results.Ok(summary);
})
.WithName("GetTaskSummary")
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