using DoAnCoSo_Web.Areas.Student.EmailService;
using DoAnCoSo_Web.Areas.Student.MomoService;
using DoAnCoSo_Web.Areas.Student.VnpayService;
using DoAnCoSo_Web.Data;
using DoAnCoSo_Web.Models;
using DoAnCoSo_Web.Models.AppSettings;
using DoAnCoSo_Web.Models.Momo;
using DoAnCoSo_Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//-------------------------------------------
// Các service 
//-------------------------------------------
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();
builder.Services.AddScoped<EmailServer>();
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<User, IdentityRole>()
    .AddDefaultTokenProviders()
    .AddDefaultUI()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddAuthentication(options => { })
    .AddCookie()
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration.GetSection("GoogleKeys:ClientId").Value;
        options.ClientSecret = builder.Configuration.GetSection("GoogleKeys:ClientSecret").Value;
    });

//-------------------------------------------
//  THÊM CẤU HÌNH JWT (cho Flutter)
//-------------------------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = jwtSection["Key"];
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
Console.WriteLine($"[Debug] JWT Key: {key}");

builder.Services.AddAuthentication()
    .AddJwtBearer("JwtBearer", options =>
    {
        options.RequireHttpsMetadata = false; // dev: false, production: true
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
            ValidateLifetime = true
        };
    });

//-------------------------------------------
// Bật CORS cho Flutter
//-------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFlutter", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

//-------------------------------------------
// ✅ Thêm Swagger cho API
//-------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "DoAnCoSo API",
        Description = "API for DoAnCoSo project (Flutter + Web)"
    });

    // Cho phép nhập Bearer token trong Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập 'Bearer' [space] + token của bạn",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    var securityReq = new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    };
    c.AddSecurityRequirement(securityReq);
});

//-------------------------------------------
// Build app
//-------------------------------------------
var app = builder.Build();

//-------------------------------------------
// Pipeline
//-------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger(); // ✅ bật Swagger
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoAnCoSo API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseSwagger(); // nếu muốn bật Swagger cả ở Production
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DoAnCoSo API V1");
        c.RoutePrefix = "swagger";
    });
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

//  CORS + Auth
app.UseCors("AllowFlutter");
app.UseAuthentication();
app.UseAuthorization();

//-------------------------------------------
// Map routes MVC + API song song
//-------------------------------------------

// Map API (cho Flutter)
app.MapControllers();

// Map MVC (cũ)
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Student}/{controller=StudentHome}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages().WithStaticAssets();

//-------------------------------------------
app.Run();