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
    var result = await GetDescribeIsExistsSecret(_amazonSecretsManager, secretModel.SecretName);

    if (result.DescribeSecretResponse is not null)
        return Results.BadRequest("secret already defined");

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

app.MapPut("update-secret-value", async (
    [FromServices] IAmazonSecretsManager _amazonSecretsManager,
    [FromBody] UpdateSecretValueModel updateSecretValueModel) =>
{
    var putSecretValueRequest = new PutSecretValueRequest()
    {
        SecretId = updateSecretValueModel.SecretName,
        SecretString = updateSecretValueModel.Value
    };

    var putSecretValueResponse = await _amazonSecretsManager.PutSecretValueAsync(putSecretValueRequest);

    return Results.Ok(putSecretValueResponse);
});

app.MapGet("describe-secret", async (
    [FromServices] IAmazonSecretsManager _amazonSecretsManager,
    [FromQuery] string secretName) =>
{
    var result = await GetDescribeIsExistsSecret(_amazonSecretsManager, secretName);

    if (result.DescribeSecretResponse is null)
        return Results.BadRequest(result.ErrorMessage);

    return Results.Ok(result.DescribeSecretResponse);

});
async static Task<GetDescribeIsExistSecretModel> GetDescribeIsExistsSecret(
    IAmazonSecretsManager amazonSecretManager,
    string secretName)
{

    DescribeSecretResponse? describeSecretResponse = default;
    GetDescribeIsExistSecretModel? getDescribeIsExistSecretModel = default;

    if (string.IsNullOrWhiteSpace(secretName))
        return new GetDescribeIsExistSecretModel(describeSecretResponse, "secret name is null");

    try
    {
    var describeSecretRequest = new DescribeSecretRequest()
    {
        SecretId = secretName
    };

    var describeSecretResponse = await _amazonSecretsManager.DescribeSecretAsync(describeSecretRequest);

        describeSecretResponse = await amazonSecretManager.DescribeSecretAsync(describeSecretRequest);

    }
    catch (ResourceNotFoundException)
    {
        return new GetDescribeIsExistSecretModel(describeSecretResponse, "secret not found");

    }
    catch (Exception ex)
    {
        return new GetDescribeIsExistSecretModel(describeSecretResponse, ex.Message);
    }

    return new GetDescribeIsExistSecretModel(describeSecretResponse, string.Empty);

}

internal record GetDescribeIsExistSecretModel(DescribeSecretResponse? DescribeSecretResponse, string ErrorMessage);
internal record CreateSecretModel(string SecretName, string Value, string Description);
internal record UpdateSecretValueModel(string SecretName, string Value);
