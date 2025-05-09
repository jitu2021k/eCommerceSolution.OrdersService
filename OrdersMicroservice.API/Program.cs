using eCommerce.OrdersMicroservice.BusinessLogicLayer;
using eCommerce.OrdersMicroservice.DataAccessLayer;
using eCommerce.OrdersMicroservice.API.Middleware;
using FluentValidation.AspNetCore;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.HttpClients;
using System.Text.Json.Serialization;
using Polly;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataAccessLayer(builder.Configuration);
builder.Services.AddBusinessLogicLayer(builder.Configuration);
builder.Services.AddControllers();

//FluentValidation
builder.Services.AddFluentValidationAutoValidation();

//Add Model Binder to read values from JSON to enum
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder => {  
        builder.WithOrigins("http://localhost:4200")
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddTransient<IUsersMicroservicePolicies, UsersMicroservicePolicies>();
builder.Services.AddTransient<IProductsMicroservicePolicies, ProductsMicroservicePolicies>();
builder.Services.AddTransient<IPollyPolicies, PollyPolicies>();

builder.Services.AddHttpClient<UsersMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["UsersMicroserviceName"]}:{builder.Configuration["UsersMicroservicePort"]}");
})
   .AddPolicyHandler(
       builder.Services.BuildServiceProvider()
        .GetRequiredService<IUsersMicroservicePolicies>().GetCombinedPolicy()
    )
   .AddPolicyHandler(
       builder.Services.BuildServiceProvider()
        .GetRequiredService<IUsersMicroservicePolicies>().GetFallbackPolicy()
    );

builder.Services.AddHttpClient<ProductsMicroserviceClient>(client =>
{
    client.BaseAddress = new Uri($"http://{builder.Configuration["ProductsMicroserviceName"]}:{builder.Configuration["ProductsMicroservicePort"]}");
}).AddPolicyHandler(
       builder.Services.BuildServiceProvider()
        .GetRequiredService<IProductsMicroservicePolicies>().GetFallbackPolicy()
    ).AddPolicyHandler(
       builder.Services.BuildServiceProvider()
        .GetRequiredService<IProductsMicroservicePolicies>().GetBulkheadIsolationPolicy()
    );

var app = builder.Build();

app.UseExceptionHandlingMiddleware();
app.UseRouting();

//Cors
app.UseCors();

//Swagger

app.UseSwagger();
app.UseSwaggerUI();
//if (builder.Environment.IsDevelopment())
//{
//    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
//    {
//        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
//        options.RoutePrefix = string.Empty;
//    });
//}


//Auth
//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

//Endpoints
app.MapControllers();

app.Run();
