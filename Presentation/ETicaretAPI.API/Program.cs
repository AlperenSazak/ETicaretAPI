using ETicaretAPI.API.Configurations.ColumnWriters;
using ETicaretAPI.API.Extensions;
using ETicaretAPI.Application;
using ETicaretAPI.Application.Validators.Products;
using ETicaretAPI.Infrastructure;
using ETicaretAPI.Infrastructure.Filters;
using ETicaretAPI.Infrastructure.Services.Storage.Azure;
using ETicaretAPI.Infrastructure.Services.Storage.Local;
using ETicaretAPI.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Sinks.PostgreSQL;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using ETicaretAPI.SignalR;
using Microsoft.Extensions.DependencyInjection;
using ETicaretAPI.API.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor(); //Client'tan gelen request sonucunda oluşturulan HttpContext nesnesine katmanlardaki class'lar üzerinden (business logic) erişebilmemizi sağlayan bir servistir.
builder.Services.AddPersistenceServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
builder.Services.AddSignalRServices();

//builder.Services.AddStorage(StorageType.Azure);
builder.Services.AddStorage<LocalStorage>();
//builder.Services.AddStorage<AzureStorage>();

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:4200", "https://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

Logger log = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt")
    .WriteTo.PostgreSQL(builder.Configuration.GetConnectionString("PostgreSQL"), "logs", 
        needAutoCreateTable: true,
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            {"message", new RenderedMessageColumnWriter()},
            {"message_template", new MessageTemplateColumnWriter()},
            {"level", new LevelColumnWriter()},
            {"time_stamp", new TimestampColumnWriter()},
            {"exception", new ExceptionColumnWriter()},
            {"log_event", new LogEventSerializedColumnWriter()},
            {"user_name", new UsernameColumnWriter()}
        })
    .WriteTo.Seq(builder.Configuration["Seq:ServerURL"])
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .CreateLogger();

builder.Host.UseSerilog(log);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestHeaders.Add("sec-ch-ua");
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.Filters.Add<RolePermissionFilter>();

})
    .AddFluentValidation(configuration => configuration.RegisterValidatorsFromAssemblyContaining<CreateProductValidator>())
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Admin", options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateAudience = true, //Oluşturulacak token değerini kimlerin/hangi originlerin/sitelerin kullanıcı belirlediğimiz değerdir. -> www.bilmemne.com
            ValidateIssuer = true, //Oluşturulacak token değerini kimin dağıttığını ifade edeceğimiz alandır. -> www.myapi.com
            ValidateLifetime = true, //Oluşturulan token değerinin süresini kontrol edecek olan doğrulamadır.
            ValidateIssuerSigningKey = true, //Üretilecek token değerinin uygulamamıza ait bir değer olduğunu ifade eden sucirty key verisinin doğrulamasıdır. 

            ValidAudience = builder.Configuration["Token:Audience"],
            ValidIssuer = builder.Configuration["Token:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Token:SecurityKey"])),
            LifetimeValidator = (notBefore, expires, securityToken, validationParameters) => expires != null ? expires > DateTime.UtcNow : false,

            NameClaimType = ClaimTypes.Name //JWT üzerinde Name claimine karşılık gelen değeri User.Identity.Name propertysinden elde edebiliriz.
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.ConfigureExceptionHandler<Program>(app.Services.GetRequiredService<ILogger<Program>>());
app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseHttpLogging();
app.UseCors();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{

    var username = context.User?.Identity?.IsAuthenticated != null || true ? context.User.Identity.Name : null;
    LogContext.PushProperty("user_name", username);
    await next();
});

app.MapControllers();
app.MapHubs();

app.Run();
