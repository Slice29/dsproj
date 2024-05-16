using FakeAPI.Consumers;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


bool IsRunningInContainer()
{
    return bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) && inContainer;
}

var config = builder.Configuration;

builder.Services.AddMassTransit(mt =>
{
    var rabbitMqHost = config.GetValue<string>("RabbitMQHost");
    if (IsRunningInContainer()) rabbitMqHost = "rabbitmq";
    var rabbitMqPort = config.GetValue<string>("RabbitMQPort");
    var rabbitUri = new Uri($"rabbitmq://{rabbitMqHost.Trim('/')}:{rabbitMqPort}");
    string redisString = "";
    if (IsRunningInContainer())
    {
        redisString = "redis";
    }
    mt.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitUri);

        cfg.ConfigureEndpoints(context);
    });

    mt.AddConsumersFromNamespaceContaining<TestConsumer>();

});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
