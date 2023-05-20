using System.IO.Pipelines;
using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace WebApplication3
{
    public sealed class MultipartReaderDisposer : IAsyncDisposable
    {
        private static PropertyInfo? _getFinalBoundaryFound;

        private readonly MultipartReader _reader;
        private readonly FileMultipartSection _section;
        private readonly string _url;

        public MultipartReaderDisposer(MultipartReader reader, FileMultipartSection section, string url)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(url);

            _reader = reader;
            _section = section;
            _url = url;

            if (_getFinalBoundaryFound is null)
            {
                _getFinalBoundaryFound = _section.Section.Body.GetType().GetProperty("FinalBoundaryFound", BindingFlags.Instance | BindingFlags.Public);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _section.Section.Body.DrainAsync(default);
            if ((bool?)_getFinalBoundaryFound?.GetValue(_section.Section.Body) == false)
            {
                throw new InvalidOperationException($"Multipart content not read completely because of tail stream '{_section.Name}' {_url}.");
            }
        }
    }
}
