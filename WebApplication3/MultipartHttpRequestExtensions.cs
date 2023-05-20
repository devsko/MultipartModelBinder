using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace WebApplication3
{
    public static class MultipartHttpRequestExtensions
    {
        public static MultipartCollection? GetMultipart(this HttpRequest request)
        {
            return request.HttpContext.Features.Get<IMultipartFeature>()?.Multipart;
        }

        public static Task<MultipartCollection> ReadMultipartAsync(this HttpRequest request, Func<string?> getTailStreamSectionName, CancellationToken cancellationToken)
        {
            IFeatureCollection features = request.HttpContext.Features;
            IMultipartFeature? feature = features.Get<IMultipartFeature>();
            if (feature?.Multipart is null)
            {
                feature = new MultipartFeature(request);
                features.Set<IMultipartFeature>(feature);
            }
            
            return feature.ReadMultipartAsync(getTailStreamSectionName, cancellationToken);
        }
    }
}
