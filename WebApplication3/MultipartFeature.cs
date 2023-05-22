using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace WebApplication3
{
    public class MultipartFeature : IMultipartFeature
    {
        public const string MultipartMediaType = "multipart/mixed";

        private readonly HttpRequest _request;

        private Task<MultipartCollection>? _readMultipart;

        public MultipartFeature(HttpRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _request = request;
        }

        public MultipartCollection? Multipart
        {
            get
            {
                return _readMultipart?.IsCompleted == true ? _readMultipart.Result : null;
            }
        }

        public bool HasMultipartContentType([NotNullWhen(true)] out string? boundary)
        {
            _ = MediaTypeHeaderValue.TryParse(_request.ContentType, out MediaTypeHeaderValue? contentType);
            if (contentType?.MediaType != MultipartMediaType)
            {
                boundary = null;
                return false;
            }
            boundary = contentType?.Boundary.ToString();

            return boundary is not null;
        }

        public Task<MultipartCollection> ReadMultipartAsync(Func<string?> getTailStreamSectionName, CancellationToken cancellationToken)
        {
            return _readMultipart ??= ReadAsync(cancellationToken);

            async Task<MultipartCollection> ReadAsync(CancellationToken cancellationToken)
            {
                if (!HasMultipartContentType(out string? boundary))
                {
                    throw new InvalidOperationException("Incorrect Content-Type: " + _request.ContentType);
                }
                if (boundary.Length > 64)
                {
                    throw new InvalidDataException("Multipart boundary too long");
                }

                if (_request.ContentLength == 0)
                {
                    return MultipartCollection.Empty;
                }

                string? tailStreamSectionName = getTailStreamSectionName();
                bool hasTailStream = false;
                Dictionary<string, object> values = new(StringComparer.OrdinalIgnoreCase);

                using (cancellationToken.Register((state) => ((HttpContext)state!).Abort(), _request.HttpContext))
                {
                    MultipartReader reader = new(boundary, _request.Body)
                    {
                        HeadersCountLimit = 16,
                        HeadersLengthLimit = 1024,
                    };

                    MultipartSection? section;
                    while ((section = await reader.ReadNextSectionAsync(cancellationToken)) is not null)
                    {
                        if (values.Count > 64)
                        {
                            throw new InvalidDataException($"Multipart seection count limit 64 exceeded.");
                        }
                        if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue? sectionDisposition) ||
                            !sectionDisposition.IsFormDisposition())
                        {
                            throw new InvalidDataException("Multipart section has invalid Content-Disposition value: " + section.ContentDisposition);
                        }
                        if (!MediaTypeHeaderValue.TryParse(section.ContentType, out MediaTypeHeaderValue? sectionType))
                        {
                            throw new InvalidOperationException("Multipart section has invalid Content-Type: " + section.ContentType);
                        }

                        string name;
                        object value;

                        if (sectionType.MediaType.Equals("application/json"))
                        {
                            FormMultipartSection formSection = new(section, sectionDisposition);

                            name = formSection.Name;
                            value = await formSection.GetValueAsync(cancellationToken);
                        }
                        else if (sectionType.MediaType.Equals("application/octet-stream"))
                        {
                            sectionDisposition.FileName = "data";
                            FileMultipartSection fileSection = new(section, sectionDisposition);

                            name = fileSection.Name;
                            hasTailStream = name == tailStreamSectionName;
                            long length;
                            if (hasTailStream)
                            {
                                _request.HttpContext.Response.RegisterForDispose(new MultipartReaderDisposer(fileSection, _request.GetDisplayUrl()));
                                length = int.MaxValue;
                            }
                            else
                            {
                                EnableRewind(section, _request.HttpContext.Response.RegisterForDispose);
                                await section.Body.DrainAsync(cancellationToken);
                                length = section.Body.Length;
                            }

                            FormFile file = section.BaseStreamOffset.HasValue
                                ? new FormFile(_request.Body, section.BaseStreamOffset.Value, length, name, string.Empty)
                                : new FormFile(section.Body, 0, length, name, string.Empty);
                            file.Headers = new HeaderDictionary(section.Headers);

                            value = file;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Multipart section MediaType {sectionType.MediaType} is not supported");
                        }

                        if (name.Length > 1024)
                        {
                            throw new InvalidDataException("Multipart section key length 1024 exceeded.");
                        }
                        values.Add(name, value);

                        if (hasTailStream)
                        {
                            break;
                        }
                    }
                }

                if (!hasTailStream && _request.Body.CanSeek)
                {
                    _request.Body.Seek(0, SeekOrigin.Begin);
                }

                return values.Count == 0 
                    ? MultipartCollection.Empty 
                    : new MultipartCollection(values);
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
