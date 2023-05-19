using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace WebApplication3
{
    public static class MultipartHttpRequestExtensions
    {
        public const string MultipartMediaType = "multipart/mixed";

        public static async Task<MultipartCollection> ReadMultipartAsync(this HttpRequest request, CancellationToken cancellationToken = default)
        {
            _ = MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? contentType);

            if (contentType?.MediaType != MultipartMediaType)
            {
                throw new InvalidOperationException("Incorrect Content-Type: " + request.ContentType);
            }
            string? boundary = contentType?.Boundary.ToString();
            if (boundary is null)
            {
                throw new InvalidOperationException("Boundary not set");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (request.ContentLength == 0)
            {
                return MultipartCollection.Empty;
            }

            Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);

            using (cancellationToken.Register((state) => ((HttpContext)state!).Abort(), request.HttpContext))
            {
                MultipartReader reader = new(boundary, request.Body);
                MultipartSection? section;
                while ((section = await reader.ReadNextSectionAsync(cancellationToken)) is not null)
                {
                    if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue? sectionDisposition) ||
                        !sectionDisposition.IsFormDisposition())
                    {
                        throw new InvalidDataException("Multipart section has invalid Content-Disposition value: " + section.ContentDisposition);
                    }
                    if (!MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue? sectionType))
                    {
                        throw new InvalidOperationException("Multipart section has invalid Content-Type: " + section.ContentType);
                    }

                    if (sectionType.MediaType.Equals("application/json"))
                    {
                        FormMultipartSection formSection = new(section, sectionDisposition);

                        string name = formSection.Name;
                        string value = await formSection.GetValueAsync(cancellationToken);

                        values.Add(name, value);
                    }
                    else if (sectionType.MediaType.Equals("application/octet-stream"))
                    {
                        sectionDisposition.FileName = "data";
                        FileMultipartSection fileSection = new(section, sectionDisposition);

                        EnableRewind(section, request.HttpContext.Response.RegisterForDispose);

                        await section.Body.DrainAsync(cancellationToken);

                        string name = fileSection.Name;
                        string fileName = string.Empty;

                        FormFile file;
                        if (section.BaseStreamOffset.HasValue)
                        {
                            file = new FormFile(request.Body, section.BaseStreamOffset.GetValueOrDefault(), section.Body.Length, name, fileName);
                        }
                        else
                        {
                            file = new FormFile(section.Body, 0, section.Body.Length, name, fileName);
                        }
                        file.Headers = new HeaderDictionary(section.Headers);

                        values.Add(name, file);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Multipart section MediaType {sectionType.MediaType} is not supported");
                    }
                }
            }

            if (request.Body.CanSeek)
            {
                request.Body.Seek(0, SeekOrigin.Begin);
            }

            if (values.Count == 0)
            {
                return MultipartCollection.Empty;
            }
            else
            {
                return new MultipartCollection(values);
            }


            static void EnableRewind(MultipartSection section, Action<IDisposable> registerForDispose, int bufferThreshold = 1024 * 1024)
            {
                var body = section.Body;
                if (!body.CanSeek)
                {
                    var fileStream = new FileBufferingReadStream(body, bufferThreshold);
                    section.Body = fileStream;
                    registerForDispose(fileStream);
                }
            }
        }
    }
}
