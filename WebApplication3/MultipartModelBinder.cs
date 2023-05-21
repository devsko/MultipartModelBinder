using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3
{
    public class MultipartModelBinder : IModelBinder
    {
        private readonly JsonOptions _jsonOptions;
        private readonly ConcurrentDictionary<ActionDescriptor, string?> _tailStreamNames = new(); 

        public MultipartModelBinder(JsonOptions jsonOptions)
        {
            _jsonOptions = jsonOptions;
        }

        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            MultipartCollection multipart = await bindingContext.HttpContext.Request.ReadMultipartAsync(GetTailStreamName, bindingContext.HttpContext.RequestAborted);

            object? value = GetMultipartValue(multipart, bindingContext.ModelMetadata);
            bindingContext.Result = ModelBindingResult.Success(value);

            string? GetTailStreamName()
            {
                return _tailStreamNames.GetOrAdd(bindingContext.ActionContext.ActionDescriptor, GetTailStreamName);

                string? GetTailStreamName(ActionDescriptor action)
                {
                    foreach (ParameterDescriptor parameter in action.Parameters)
                    {
                        FromMultipartAttribute? attribute = ((IParameterInfoParameterDescriptor)parameter).ParameterInfo.GetCustomAttribute<FromMultipartAttribute>();
                        if (attribute?.IsTailStream == true)
                        {
                            return attribute.Name ?? parameter.Name;
                        }
                    }

                    return null;
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
