using System.Text.Json;
using BestStories.Api.Contracts;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BestStories.Api.OpenApi;

public static class ApiDocumentation
{
    public static void Describe(OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, _) =>
        {
            // Left to generate its own sample from the schema, the API reference shows "string"
            // placeholders and a Z-suffixed time, which is not the format responses carry.
            // Serialising a real story keeps the documented sample honest by construction.
            if (context.JsonTypeInfo.Type == typeof(StoryDto))
            {
                schema.Example = JsonSerializer.SerializeToNode(StoryExample.Story, JsonSerializerOptions.Web);
            }

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, _, _) =>
        {
            // n binds as text, so the generated parameter is a string. The contract a caller
            // has to meet is a positive integer, and only this says so.
            foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
            {
                if (parameter.Name == "n" && parameter.Schema is OpenApiSchema schema)
                {
                    schema.Type = JsonSchemaType.Integer;
                    schema.Format = "int32";
                    schema.Minimum = "1";
                }
            }

            return Task.CompletedTask;
        });
    }
}
