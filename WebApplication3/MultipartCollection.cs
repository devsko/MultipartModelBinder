namespace WebApplication3
{
    public class MultipartCollection
    {
        private readonly Dictionary<string, object>? _values;

        public static MultipartCollection Empty { get; } = new();

        private MultipartCollection()
        { }

        public MultipartCollection(Dictionary<string, object> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            _values = values;
        }

        public string? GetJsonValue(string name)
        {
            if (_values is not null && _values.TryGetValue(name, out object? value) && value is string json)
            {
                return json;
            }

            return null;
        }

        public IFormFile? GetFormFile(string name)
        {
            if (_values is not null && _values.TryGetValue(name, out object? value) && value is IFormFile file)
            {
                return file;
            }

            return null;
        }
    }
}
