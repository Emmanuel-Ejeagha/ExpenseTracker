using ExpensesTracker.Filters;
using ExpensesTracker.Models.Data;
using ExpensesTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpLogging;
using ExpensesTracker;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Services.AddHttpLogging(opts =>
{
    opts.LoggingFields = HttpLoggingFields.RequestProperties | 
                         HttpLoggingFields.RequestBody |
                         HttpLoggingFields.ResponseStatusCode |
                         HttpLoggingFields.Response;
    opts.RequestBodyLogLimit = 4096;
    opts.ResponseBodyLogLimit = 4096;
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

// Add services to the container
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure database context
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    options.EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Register application services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Configure distributed memory cache
builder.Services.AddDistributedMemoryCache();

// Configure response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure HTTP client
builder.Services.AddHttpClient("default", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "ExpensesTracker/2.0");
});

// Register Syncfusion license
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"] 
    ?? "Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWX5edXRcRGBZUEF1XkVWYEs=");

var app = builder.Build();

// Configure HTTP logging
app.UseHttpLogging();

// Apply database migrations and seed data
// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // For development, use EnsureCreated (simpler)
        if (app.Environment.IsDevelopment())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            // In production, use migrations
            await context.Database.MigrateAsync();
        }
        
        // Seed data
        await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while setting up the database.");
        
        if (app.Environment.IsDevelopment())
        {
            // In development, we can continue without database
            Console.WriteLine($"Database error: {ex.Message}");
            Console.WriteLine("Application will continue with limited functionality.");
        }
        else
        {
            throw; // In production, fail fast
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // UseDatabaseErrorPage is deprecated in .NET 8, use exception handler instead
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    app.UseHsts();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    if (!context.Response.Headers.ContainsKey("Cache-Control"))
    {
        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
    
    await next();
});

// Enable middleware
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 1 year
        ctx.Context.Response.Headers["Cache-Control"] = "public,max-age=31536000";
        ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
    }
});

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware for request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
    
    var startTime = DateTime.UtcNow;
    await next();
    var duration = DateTime.UtcNow - startTime;
    
    logger.LogInformation("Response: {StatusCode} in {Duration}ms", 
        context.Response.StatusCode, duration.TotalMilliseconds);
});

// Configure endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// Fallback for SPA routing
app.MapFallbackToController("Index", "Dashboard");

Console.WriteLine($"Application started in {app.Environment.EnvironmentName} mode");

app.Run();