using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

JsonSerializerOptions options = new(JsonSerializerDefaults.Web) 
{ 
    Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } 
};

MultipartContent content = new("mixed", "==:boundary:==");

JsonContent json = JsonContent.Create(new Nested1Dto("asdf", 0), options: options);
json.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("form-data; name=\"n1dto\"");
content.Add(json);

json = JsonContent.Create(new Nested2Dto("xyz", 42), options: options);
json.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("form-data; name=\"n2dto\"");
content.Add(json);

using FileStream data = new(@"C:\Users\stefa\OneDrive\Bilder\PIA21345.tif", FileMode.Open);

StreamContent file = new(data);
file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
file.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("form-data; name=\"file\"");
content.Add(file);

json = JsonContent.Create(StatusEnum.Started, options: options);
json.Headers.ContentDisposition = ContentDispositionHeaderValue.Parse("form-data; name=\"status\"");
content.Add(json);

//string s = await content.ReadAsStringAsync();
//Console.WriteLine(s);

await Task.Delay(5000);

HttpClient client = new() { BaseAddress = new Uri("https://localhost:7038") };
HttpRequestMessage request = new(HttpMethod.Post, "x");
request.Content = content;

var response = await client.SendAsync(request);

Console.WriteLine(await response.Content.ReadAsStringAsync());

public record Nested1Dto(string Str, int Int);
public record Nested2Dto(string Str2, int Int2);

public enum StatusEnum
{
    Created,
    Started,
}
