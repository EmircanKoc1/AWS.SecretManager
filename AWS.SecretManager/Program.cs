using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;

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

    return Results.Ok((await _amazonSecretsManager.ListSecretsAsync(listSecretsRequest)).SecretList);

});


app.MapPost("create-secret", async (
    [FromServices] IAmazonSecretsManager _amazonSecretsManager,
    [FromBody] CreateSecretModel secretModel) =>
{
    var createSecretRequest = new CreateSecretRequest()
    {
        Description = secretModel.Description,
        Name = secretModel.SecretName,
        SecretString = secretModel.Value

    };

    var createSecretResponse = await _amazonSecretsManager.CreateSecretAsync(createSecretRequest);

    if (createSecretResponse.HttpStatusCode is System.Net.HttpStatusCode.OK)
        return Results.Ok(createSecretResponse);

    return Results.BadRequest("secrets not created");

});


app.MapGet("get-secret", async (
    [FromServices] IAmazonSecretsManager _amazonSecretsManager,
    [FromQuery] string secretName) =>
{
    var getSecretValueRequest = new GetSecretValueRequest()
    {
        SecretId = secretName
    };
    GetSecretValueResponse getSecretValueResponse = default;

    try
    {
        getSecretValueResponse = await _amazonSecretsManager.GetSecretValueAsync(getSecretValueRequest);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }

    return Results.Ok(getSecretValueResponse.SecretString);
    
});



app.Run();


internal record CreateSecretModel(string SecretName, string Value, string Description);
