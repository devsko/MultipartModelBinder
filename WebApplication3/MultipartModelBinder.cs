using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3
{
    public class MultipartModelBinder : IModelBinder
    {
        private readonly JsonOptions _jsonOptions;

        private MultipartCollection? _multiparts;

        public MultipartModelBinder(JsonOptions jsonOptions)
        {
            _jsonOptions = jsonOptions;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            _multiparts ??= await bindingContext.HttpContext.Request.ReadMultipartAsync(bindingContext.HttpContext.RequestAborted);

            object? value = GetMultipartValue(bindingContext.ModelMetadata);
            bindingContext.Result = ModelBindingResult.Success(value);
        }

        private object? GetMultipartValue(ModelMetadata metadata)
        {
            string? name = metadata.Name;
            object? value = null;
            if (name is not null)
            {
                if (metadata.ModelType == typeof(IFormFile))
                {
                    value = _multiparts!.GetFormFile(name);
                }
                else
                {
                    string? json = _multiparts!.GetJsonValue(name);
                    if (json is not null)
                    {
                        value = JsonSerializer.Deserialize(json, metadata.ModelType, _jsonOptions.JsonSerializerOptions);
                    }
                }
            }

            return value;
        }
    }
}
