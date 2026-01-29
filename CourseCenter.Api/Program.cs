using CourseCenter.Api;
using CourseCenter.Api.Assessment.Services;
using CourseCenter.Api.Users.Authorization;
using CourseCenter.Api.Users.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// =========================
// 1️⃣ Services
// =========================

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CourseCenter API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// =========================
// 2️⃣ CORS (حل Failed to fetch)
// =========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()             // مهم لو Cookies / Auth
            .SetIsOriginAllowed(_ => true); // مهم مع ngrok
    });
});

// =========================
// 3️⃣ Database
// =========================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.CommandTimeout(300) // 5 minutes
    ));

// =========================
// 4️⃣ Authentication (JWT)
// =========================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)

            )

        };
        // Allow the refresh endpoint to be called without a valid (or any) access token.
        // Many clients send the expired access token in Authorization header when calling refresh,
        // which causes the JwtBearer middleware to fail the request before the controller runs.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // If this is the refresh endpoint, ignore any bearer token so the endpoint can validate the refresh token itself.
                if (context.Request.Path.StartsWithSegments("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = null;
                }

                return Task.CompletedTask;
            }
        };
    });

// =========================
// 5️⃣ Authorization (Policies)
// =========================
builder.Services.AddAuthorization(options =>
{
    // explicit claim-based policies for leads
    options.AddPolicy("LEADS_VIEW", policy =>
        policy.RequireClaim("permission", "LEADS_VIEW"));

    options.AddPolicy("LEADS_VIEW_ALL", policy =>
        policy.RequireClaim("permission", "LEADS_VIEW_ALL"));
});

// keep the dynamic permission policy provider for other permissions
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // مش ضروري تضيف الكونفرتر هنا
        // طالما مستخدمه Attribute على الـ DTO
    });


// =========================
// Build
// =========================
var app = builder.Build();

// =========================
// 6️⃣ Middleware Pipeline
// =========================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ⭐ لازم CORS قبل Auth
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =========================
// 7️⃣ Database Seed
// =========================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.Seed(context);
    CourseCenter.Api.Users.Permissions.PermissionSeeder.Seed(context);
}

app.Run();