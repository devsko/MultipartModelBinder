using Microsoft.AspNetCore.Mvc;

namespace WebApplication3
{
    [ApiController]
    public class XController : ControllerBase
    {
        [HttpPost("/x")]
        [Consumes("multipart/mixed")]
        public async Task<IActionResult> Put([FromMultipart]Dto body)
        {
            using (Stream stream = body.file!.OpenReadStream())
            {
                MemoryStream local = new((int)stream.Length);
                await stream.CopyToAsync(local);
                Console.WriteLine(local.ToArray().Length);
            }

            return Ok();
        }
    }

    public class Dto
    {
        public Nested1Dto n1dto { get; set; }
        public Nested2Dto? n2Dto { get; set; }
        public IFormFile? file { get; set; }
        public StatusEnum status { get; set; }
    }
    public record Nested1Dto(string Str, int Int);
    public class Nested2Dto
    {
        public string Str2 { get; set; }
        public int Int2 { get; set; }
    }

    public enum StatusEnum
    {
        Created,
        Started,
    }
}
