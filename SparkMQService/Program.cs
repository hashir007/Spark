using SparkMQService;
using SparkMQService.Services;
using SparkService;
using SparkService.Models;
using SparkService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Core.DependencyInjection;
using System.Runtime;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(Environment.CurrentDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


var rabbitMqSection = builder.Configuration.GetSection("RabbitMq");
var exchangeSection = builder.Configuration.GetSection("RabbitMqExchange");

builder.Services.AddRabbitMqServices(rabbitMqSection)
                             .AddConsumptionExchange("HAPPY_SUGAR_DADDY_EXCHANGE", exchangeSection)
                             .AddMessageHandlerTransient<QueueService>("HAPPY_SUGAR_DADDY_APP");

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<SparkDatabaseSettings>(
    builder.Configuration.GetSection("HappySugarDaddyDatabase"));
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection("AppSettings"));


builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<MailService>();

builder.Services.AddSingleton<UsersService>();
builder.Services.AddSingleton<ProfilesService>();
builder.Services.AddSingleton<RolesService>();
builder.Services.AddSingleton<UserRolesService>();
builder.Services.AddSingleton<EmailTemplateService>();
builder.Services.AddSingleton<EmailVerificationRequestsService>();
builder.Services.AddSingleton<FileService>();
builder.Services.AddSingleton<ConversationService>();
builder.Services.AddSingleton<EmailMessageService>();
builder.Services.AddSingleton<FriendshipsService>();
builder.Services.AddSingleton<LikesDisLikesProfilesService>();
builder.Services.AddSingleton<FavoritesService>();
builder.Services.AddSingleton<KissesService>();
builder.Services.AddSingleton<TraitsService>();
builder.Services.AddSingleton<InterestsService>();
builder.Services.AddSingleton<CompatibilityScoresService>();
builder.Services.AddSingleton<PhotosService>();
builder.Services.AddTransient<SubscriptionService>();

using IHost host = builder.Build();

Startup(host.Services);

await host.RunAsync();


static void Startup(IServiceProvider hostProvider)
{
    Console.WriteLine("STARTED Happy Sugar Daddy MQ Service");
}