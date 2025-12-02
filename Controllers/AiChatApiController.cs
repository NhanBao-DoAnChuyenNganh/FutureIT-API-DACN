using DoAnCoSo_Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteTTTH.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiChatApiController : ControllerBase
    {
           
        [HttpPost("ask-ai")]
        public async Task<IActionResult> AskAi([FromBody] string question, [FromServices] GeminiService gemini)
        {
            var reply = await gemini.AskAsync(question);
            return Ok(new { question, reply });
        }
    }
}
