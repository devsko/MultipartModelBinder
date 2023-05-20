using Microsoft.AspNetCore.Mvc;

namespace WebApplication3
{
    [ApiController]
    public class XController : ControllerBase
    {
        [HttpPost("/x")]
        [Consumes("multipart/mixed")]
        public async Task<IActionResult> Put1(
            [FromMultipart] Nested1Dto n1dto, 
            [FromMultipart] Nested2Dto? n2dto, 
            [FromMultipart(Name = "XXX")] StatusEnum status, 
            [FromMultipart(IsTailStream = true)] IFormFile? file)
        {
            using (Stream stream = file!.OpenReadStream())
            await using (FileStream save = new(@"C:\Users\stefa\Downloads\test.tif", FileMode.Create))
            {
                await stream.CopyToAsync(save);
            }

            return Ok();
        }

        [HttpPost("/y")]
        [Consumes("multipart/mixed")]
        public async Task<IActionResult> Put2(
            [FromMultipart] Nested1Dto n1dto,
            [FromMultipart] Nested2Dto? n2dto,
            [FromMultipart(Name = "XXX")] StatusEnum status,
            [FromMultipart(IsTailStream = false)] IFormFile? file)
        {
            using (Stream stream = file!.OpenReadStream())
            await using (FileStream save = new(@"C:\Users\stefa\Downloads\test.tif", FileMode.Create))
            {
                await stream.CopyToAsync(save);
            }

            return Ok();
        }

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
