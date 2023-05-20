using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebApplication3
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class FromMultipartAttribute : Attribute, IBindingSourceMetadata, IModelNameProvider
    {
        public BindingSource? BindingSource => MultipartModelBinderProvider.BindingSource;

        public string? Name { get; set; }

        public bool IsTailStream { get; set; }
    }
}
