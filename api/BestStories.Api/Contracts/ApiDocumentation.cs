using System.Text.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BestStories.Api.Contracts;

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
            // n is taken as text so the endpoint can answer every malformed value itself, which
            // leaves the generated parameter typed as a string. The contract callers have to
            // meet is a positive integer, and this is what tells them so.
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
