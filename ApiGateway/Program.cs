using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

//Chche
builder.Services.AddMemoryCache();
//Ocelot
builder.Services.AddOcelot().AddPolly();
builder.Services.AddRateLimiting();

//Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => {
        builder.WithOrigins("http://localhost:4200").AllowAnyMethod().AllowAnyHeader();
    });
});
var app = builder.Build();
app.UseCors();
await app.UseOcelot();

app.Run();
