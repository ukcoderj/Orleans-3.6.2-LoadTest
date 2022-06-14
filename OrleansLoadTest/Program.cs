using LoadTest.Grains;
using LoadTest.SharedBase.Models;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Versions.Compatibility;
using Orleans.Versions.Selector;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<Program>()
            .Build();

// Add services to the container.

builder.Services.AddApplicationInsightsTelemetry(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var storageAccountName = config.GetValue<string>("StorageAccountWeb:AcctName");
var storageAccountSas = config.GetValue<string>("StorageAccountWeb:Sas");
var storageAccountUrlString = $"https://{storageAccountName}.table.core.windows.net/";
StorageConnectionInfo storageInfo = new StorageConnectionInfo(storageAccountUrlString, storageAccountSas);



builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.AddAzureTableGrainStorage(
                    name: "LoadTestNumbersTableStorage1",
                    configureOptions: options =>
                    {
                        options.UseJson = true;
                        options.ConfigureTableServiceClient(storageInfo.StorageUri, storageInfo.SasCredential);
                        //options.ConfigureTableServiceClient("UseDevelopmentStorage=true");
                    })
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansLoadTest";
                })
                .Configure<GrainVersioningOptions>(options =>
                {
                    options.DefaultCompatibilityStrategy =
                    nameof(BackwardCompatible);
                    options.DefaultVersionSelectorStrategy =
                    nameof(MinimumVersion);
                })
                .UseDashboard(options => {
                    options.HostSelf = false;
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(NumberStoreGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole());
});



var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
