using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FromMultipartAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource? BindingSource => MultipartModelBinderProvider.BindingSource;
    }
}
