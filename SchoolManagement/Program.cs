using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SchoolManagement.Data;
using SchoolManagement.Service;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers(options => options.Filters.Add<CrudPermissionFilter>());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter token like this: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyCorsPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://school.cognerasystems.com",
                "https://school.cognerasystems.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// 🔥 AUTO DI (Repositories + Services)
builder.Services.RegisterAppServices();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<SalaryGenerationService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

    // Role-bearing staff tokens must be refreshed after a promotion/demotion.
    // Student/parent tokens use a separate identity format without RoleId.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var role = context.Principal?.FindFirst("RoleId")?.Value;
            if (role == null) return;
            var idText = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idText, out var id)) { context.Fail("Invalid user."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.Users.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.RoleId, x.IsActive }).FirstOrDefaultAsync();
            if (user == null || !user.IsActive || user.RoleId?.ToString() != role)
                context.Fail("Your role has changed. Please sign in again.");
        }
    };
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MyCorsPolicy");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Never return the React application for a missing API route. Clients must
// receive a real JSON error instead of HTTP 200 with index.html.
app.MapFallback("/api/{**path}", (HttpContext context) =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    return context.Response.WriteAsJsonAsync(new
    {
        success = false,
        message = "API endpoint was not found. Verify that the latest API is deployed.",
        path = context.Request.Path.Value
    });
});
app.MapFallbackToFile("index.html");

app.Run();
