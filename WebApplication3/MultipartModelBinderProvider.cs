using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Options;

namespace WebApplication3
{
    public class MultipartModelBinderProvider : IModelBinderProvider
    {
        internal static readonly BindingSource BindingSource = new("MultiPart", "MultiPart", false, true);

        private List<ModelMetadata>? _childMetadata;
        private MultipartModelBinder? _modelBinder;

        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            ModelMetadata metadata = context.Metadata;
            if (_childMetadata is not null && _childMetadata.Contains(metadata))
            {
                return _modelBinder!;
            }
            
            if (context.BindingInfo.BindingSource?.CanAcceptDataFrom(BindingSource) == true)
            {
                if (_childMetadata is not null)
                {
                    throw new InvalidOperationException("MultipartModelBinder cannot work recursive");
                }

                try
                {
                    JsonOptions jsonOptions = context.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
                    _modelBinder = new MultipartModelBinder(jsonOptions);

                    _childMetadata = new List<ModelMetadata>();
                    _childMetadata.AddRange(metadata.Properties);
                    _childMetadata.AddRange(metadata.BoundConstructor?.BoundConstructorParameters ?? Array.Empty<ModelMetadata>());

                    IModelBinder objectBinder = new ComplexObjectModelBinderProvider().GetBinder(context)!;
                    return objectBinder;
                }
                finally
                {
                    _childMetadata = null;
                    _modelBinder = null;
                }
            }

            return null;
        }
    }
}
