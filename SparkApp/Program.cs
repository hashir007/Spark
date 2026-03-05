using Asp.Versioning;
using SparkApp;
using SparkApp.Authorization;
using SparkApp.AuthorizationHandler;
using SparkApp.Extensions;
using SparkApp.Hubs;
using SparkApp.Services;
using SparkService;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Org.BouncyCastle.Asn1.Ocsp;
using PayPal.Core;
using RabbitMQ.Client.Core.DependencyInjection;
using RabbitMQ.Client.Core.DependencyInjection.Configuration;
using System.Text;
using WebASparkApppi.Authorization;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", builder =>
{
    builder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(origin => true)
        .AllowCredentials();
}));
// Add services to the container.
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});
builder.Services.AddControllers().AddJsonOptions(
        options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Spark API", Version = "v1" });
    c.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Description = @"JWT Authorization header using the Bearer scheme. \r\n\r\n 
                      Enter 'Bearer' [space] and then your token in the text input below.
                      \r\n\r\nExample: 'Bearer 12345abcdef'",
            Name = HeaderNames.Authorization,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
        });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
      {
        {
          new OpenApiSecurityScheme
          {
            Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              },
              Scheme = "oauth2",
              Name = "Bearer",
              In = ParameterLocation.Header,

            },
            new List<string>()
          }
        });
});

builder.Services.AddCors(policyBuilder =>
    policyBuilder.AddDefaultPolicy(policy =>
        policy.WithOrigins("*").AllowAnyHeader().AllowAnyHeader())
);

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<SparkDatabaseSettings>(
    builder.Configuration.GetSection("HappySugarDaddyDatabase"));
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection(nameof(MailSettings)));
builder.Services.Configure<RabbitMq>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.Configure<PaypalOptions>(
    builder.Configuration.GetSection("PayPalOptions"));
builder.Services.Configure<AuthorizeNetOptions>(
    builder.Configuration.GetSection("AuthorizeNetOptions"));




var jwtIssuer = builder.Configuration.GetSection("AppSettings:JWTValidIssuer").Get<string>();
var jwtValidAudience = builder.Configuration.GetSection("AppSettings:JWTValidAudience").Get<string>();
var JWTSecret = builder.Configuration.GetSection("AppSettings:JWTSecret").Get<string>();

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
 .AddJwtBearer(options =>
 {
     options.IncludeErrorDetails = true;
     options.RequireHttpsMetadata = false;
     options.SaveToken = true;
     options.UseSecurityTokenValidators = true;
     options.TokenValidationParameters.SignatureValidator = (token, _) => new JsonWebToken(token);
     options.TokenValidationParameters = new TokenValidationParameters
     {
         ValidateIssuer = true,
         ValidateAudience = true,
         ValidateLifetime = true,
         ValidateIssuerSigningKey = true,
         ValidIssuer = jwtIssuer,
         ValidAudience = jwtValidAudience,
         ClockSkew = TimeSpan.Zero,
         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTSecret))
     };
     options.Events = new JwtBearerEvents
     {
         OnMessageReceived = context =>
         {
             if ((context.Request.Path.StartsWithSegments("/chat", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/email", StringComparison.OrdinalIgnoreCase)))
             {

                 if (context.Request.Query.TryGetValue("access_token", out var access_token))
                 {
                     context.Token = access_token;
                 }

             }
             return Task.CompletedTask;
         }
     };
 });

builder.Services.AddAuthorization(); // Adds the authorization service.
builder.Services.AddScoped<IAuthorizationHandler, UserAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PhotoAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ProfileAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TraitAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, InterestsAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, KissesAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FavoriteAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, LikesDisLikesProfileAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FriendshipAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EmailAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, FolderAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ConversationAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ConversationMemberAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, MessageAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, VenueAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EventAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EventOrganizerAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, NotificationAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionServicesAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionPlansAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionsAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionsHistoriesAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SubscriptionPaymentsAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, EmailMessageWithFoldersAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, LikesDisLikesPhotoAuthorizationCrudHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BlockedListAuthorizationCrudHandler>();


var rabbitMqSection = builder.Configuration.GetSection("RabbitMq");
var exchangeSection = builder.Configuration.GetSection("RabbitMqExchange");

builder.Services.AddRabbitMqProducer(rabbitMqSection)
    .AddProductionExchange("HAPPY_SUGAR_DADDY_EXCHANGE", exchangeSection);


builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<MailService>();
builder.Services.AddTransient<JwtUtils>();


builder.Services.AddSingleton<EmailHub>();
builder.Services.AddSingleton<ChatHub>();
builder.Services.AddTransient<UsersService>();
builder.Services.AddTransient<ProfilesService>();
builder.Services.AddTransient<RolesService>();
builder.Services.AddTransient<UserRolesService>();
builder.Services.AddTransient<EmailTemplateService>();
builder.Services.AddTransient<EmailVerificationRequestsService>();
builder.Services.AddTransient<FileService>();
builder.Services.AddTransient<ConversationService>();
builder.Services.AddTransient<EmailMessageService>();
builder.Services.AddTransient<FriendshipsService>();
builder.Services.AddTransient<LikesDisLikesProfilesService>();
builder.Services.AddTransient<FavoritesService>();
builder.Services.AddTransient<KissesService>();
builder.Services.AddTransient<TraitsService>();
builder.Services.AddTransient<InterestsService>();
builder.Services.AddTransient<CompatibilityScoresService>();
builder.Services.AddTransient<PhotosService>();
builder.Services.AddTransient<VenuesService>();
builder.Services.AddTransient<EventsService>();
builder.Services.AddTransient<EventOrganizersService>();
builder.Services.AddTransient<NotificationsService>();
builder.Services.AddTransient<UserNotificationService>();
builder.Services.AddTransient<SubscriptionService>();
builder.Services.AddTransient<ForgotPasswordRequestsService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(err => err.UseCustomErrors(app.Environment));
app.UseCors("CorsPolicy");
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(
                            Path.Combine(Directory.GetCurrentDirectory(), @"FileStore")),
    RequestPath = new PathString("/Store")
});

app.UseMiddleware<JwtMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chat");
app.MapHub<EmailHub>("/email");
app.Run();
