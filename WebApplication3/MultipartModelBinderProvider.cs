using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace WebApplication3
{
    public class MultipartModelBinderProvider : IModelBinderProvider
    {
        internal static readonly BindingSource BindingSource = new("Multipart", "Multipart", false, true);

        private MultipartModelBinder? _modelBinder;

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.BindingInfo.BindingSource?.CanAcceptDataFrom(BindingSource) == true)
            {
                return _modelBinder ??= CreateModelBinder(context.Services);
            }

            return null;

            static MultipartModelBinder CreateModelBinder(IServiceProvider services)
            {
                JsonOptions jsonOptions = services.GetRequiredService<IOptions<JsonOptions>>().Value;
                return new MultipartModelBinder(jsonOptions);
            }
        }
    }
}
