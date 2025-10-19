using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StockSensePro.API.Controllers;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StockSensePro.API.Filters
{
    public class AnalyzeRequestSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != typeof(AnalyzeRequest))
            {
                return;
            }

            schema.Example = new OpenApiObject
            {
                [nameof(AnalyzeRequest.Symbol)] = new OpenApiString("AAPL"),
                [nameof(AnalyzeRequest.EnabledAgents)] = new OpenApiArray
                {
                    new OpenApiString("FundamentalAnalyst"),
                    new OpenApiString("TechnicalAnalyst"),
                    new OpenApiString("SentimentAnalyst")
                },
                [nameof(AnalyzeRequest.IncludeDebate)] = new OpenApiBoolean(true),
                [nameof(AnalyzeRequest.IncludeRiskAssessment)] = new OpenApiBoolean(true)
            };
        }
    }
}
