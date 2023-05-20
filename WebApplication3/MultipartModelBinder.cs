using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3
{
    public class MultipartModelBinder : IModelBinder
    {
        private readonly JsonOptions _jsonOptions;
        private readonly ConcurrentDictionary<ActionDescriptor, string?> _tailStreamSectionNames = new(); 

        public MultipartModelBinder(JsonOptions jsonOptions)
        {
            _jsonOptions = jsonOptions;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            MultipartCollection multipart = await bindingContext.HttpContext.Request.ReadMultipartAsync(GetTailStreamSectionName, bindingContext.HttpContext.RequestAborted);

            object? value = GetMultipartValue(multipart, bindingContext.ModelMetadata);
            bindingContext.Result = ModelBindingResult.Success(value);

            string? GetTailStreamSectionName()
            {
                return _tailStreamSectionNames.GetOrAdd(bindingContext.ActionContext.ActionDescriptor, GetTailStreamSectionName);

                string? GetTailStreamSectionName(ActionDescriptor action)
                {
                    return action
                        .Parameters
                        .Cast<ControllerParameterDescriptor>()
                        .Select(p => (p.Name, Attribute: p.ParameterInfo.GetCustomAttribute<FromMultipartAttribute>()!))
                        .Where(t => t.Attribute.IsTailStream)
                        .Select(t => t.Attribute.Name ?? t.Name)
                        .SingleOrDefault();
                }
            }
        }

        private object? GetMultipartValue(MultipartCollection multipart, ModelMetadata metadata)
        {
            string? name = metadata.BinderModelName ?? metadata.Name;
            object? value = null;
            if (name is not null)
            {
                if (metadata.ModelType == typeof(IFormFile))
                {
                    value = multipart.GetFormFile(name);
                }
                else
                {
                    string? json = multipart.GetJsonValue(name);
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
