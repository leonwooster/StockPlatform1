using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using StockSensePro.Core.Entities;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StockSensePro.API.Filters
{
    public class StockSchemaExampleFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type != typeof(Stock))
            {
                return;
            }

            schema.Example = new OpenApiObject
            {
                [nameof(Stock.Symbol)] = new OpenApiString("AAPL"),
                [nameof(Stock.Name)] = new OpenApiString("Apple Inc."),
                [nameof(Stock.Exchange)] = new OpenApiString("NASDAQ"),
                [nameof(Stock.Sector)] = new OpenApiString("Technology"),
                [nameof(Stock.Industry)] = new OpenApiString("Consumer Electronics"),
                [nameof(Stock.CurrentPrice)] = new OpenApiDouble(189.87),
                [nameof(Stock.PreviousClose)] = new OpenApiDouble(188.06),
                [nameof(Stock.Open)] = new OpenApiDouble(189.20),
                [nameof(Stock.High)] = new OpenApiDouble(190.10),
                [nameof(Stock.Low)] = new OpenApiDouble(187.95),
                [nameof(Stock.Volume)] = new OpenApiLong(53214567),
                [nameof(Stock.LastUpdated)] = new OpenApiString("2025-10-18T16:04:00Z")
            };
        }
    }
}
