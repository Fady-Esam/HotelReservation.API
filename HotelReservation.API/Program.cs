


using FluentValidation;
using FluentValidation.AspNetCore;
using HotelReservation.API.BL.Interfaces;
using HotelReservation.API.BL.Services;
using HotelReservation.API.Common.Middlewares;
using HotelReservation.API.Common.Responses;
using HotelReservation.API.Common.Settings;
using HotelReservation.API.DL;
using HotelReservation.API.DL.Logging;
using HotelReservation.API.DL.Repositories;
using HotelReservation.API.Domain.Entities;
using HotelReservation.API.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
// Bind JWT settings into JWT class
builder.Services.Configure<JwtSetting>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailSetting>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<SmsSetting>(builder.Configuration.GetSection("Sms"));
builder.Services.Configure<GoogleSetting>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<FacebookSetting>(builder.Configuration.GetSection("Facebook"));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtSetting>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<EmailSetting>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<SmsSetting>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<GoogleSetting>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<FacebookSetting>>().Value);

SerilogSetup.Configure(builder.Host);
var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSetting>();
// Register JWT as singleton for direct injection (optional)
builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<JwtSetting>>().Value);
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString).ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    //options.SuppressModelStateInvalidFilter = true;
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//.AddJsonOptions(opts =>
// {
//     opts.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
// });



builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
             .Where(e => e.Value?.Errors.Count > 0)
             .SelectMany(e => e.Value!.Errors.Select(err => err.ErrorMessage))
             .ToList(); // now errors is List<string>

        var response = ApiResponse<object>.Failure("Validation failed", errors);
        return new BadRequestObjectResult(response);
    };
});
// ? Register all validators automatically
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Add JWT Authentication definition
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer {your JWT token}'"
    });

    // Require JWT globally (optional)
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
            new string[] { }
        }
    });
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IUserConfirmationCodeService, UserConfirmationCodeService>();
builder.Services.AddScoped<IUserConfirmationCodeRepository, UserConfirmationCodeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();


builder.Services.AddAutoMapper(typeof(Program));



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    //var jwtSettings = builder.Configuration.GetSection("JWT").Get<JWT>();
    options.SaveToken = false;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.Key)),
        ClockSkew = TimeSpan.Zero, // Optional: Set to zero to avoid clock skew issues
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        //NameClaimType = ClaimTypes.NameIdentifier,  // maps the URI
        //RoleClaimType = ClaimTypes.Role,
    };
});
var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();
