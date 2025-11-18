using System.Text;
using Api.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Api.Services;
using Api.Data;
using Api.GameLogic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddSwaggerGen(c =>
{
   c.SwaggerDoc("v1", new() { Title = "My API", Version = "v1" });

   // Add JWT Authentication
   c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
   {
      Name = "Authorization",
      Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
      Scheme = "Bearer",
      BearerFormat = "JWT",
      In = Microsoft.OpenApi.Models.ParameterLocation.Header,
      Description = "Enter: Bearer {your token}"
   });

   c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
         new string[] {}
      }
   });
});

builder.Services.AddCors(options => options.AddPolicy("AllowClient", policy =>
      policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearer(options =>
   {
      options.TokenValidationParameters = new TokenValidationParameters
      {
         ValidateIssuer = true,
         ValidIssuer = builder.Configuration["AppSettings:Issuer"],

         ValidateAudience = true,
         ValidAudience = builder.Configuration["AppSettings:Audience"],

         ValidateLifetime = true,

         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
         ValidateIssuerSigningKey = true
      };

      options.Events = new JwtBearerEvents
      {
         OnMessageReceived = context =>
         {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/matchHub"))
            {
               context.Token = accessToken;
            }
            return Task.CompletedTask;
         }
      };
   });
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.CommandTimeout(15)
    ));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<ILobbyService, LobbyService>();
builder.Services.AddSingleton<IGameFactory, GameFactory>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MatchHub>("/matchHub");

if (app.Environment.IsDevelopment())
{
   app.UseSwagger();
   app.UseSwaggerUI(options =>
   {
      options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
      options.RoutePrefix = string.Empty;
   });
}

app.Run();
