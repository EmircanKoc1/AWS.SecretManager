using Amazon.SecretsManager;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("list-secretsmanagers", async (
    [FromServices] IAmazonSecretsManager _amazonSecretsManager,
    [FromQuery] int maxResults) =>
{
    var listSecretsRequest = new ListSecretsRequest()
    {
        MaxResults = maxResults
    };

    return Results.Ok(await _amazonSecretsManager.ListSecretsAsync(listSecretsRequest));

});





app.Run();

