using Asp.Versioning;
using CleanArc.Application.Features.WordLists.Commands;
using CleanArc.Application.Features.WordLists.Queries;
using CleanArc.WebFramework.BaseController;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArc.Web.Api.Controllers.V1.WordList
{
    [ApiVersion("1")]
    [ApiController]
    [Route("api/v{version:apiVersion}/WordList")]
    [Authorize]
    public class WordListController(ISender sender) : BaseController
    {
        [HttpPost("CreateNewWordList")]
        public async Task<IActionResult> CreateNewWordList(CreateWordListCommand model)
        {
            model.UserId = base.UserId;
            var command = await sender.Send(model);

            return base.OperationResult(command);
        }

        [HttpPut("UpdateWordList")]
        public async Task<IActionResult> UpdateWordList(UpdateWordListCommand model)
        {
            model.UserId = base.UserId;
            var command = await sender.Send(model);
            return base.OperationResult(command);
        }

        [HttpDelete("DeleteWordList/{wordListId:int}")]
        public async Task<IActionResult> DeleteWordList(int wordListId)
        {
            return base.OperationResult(await sender.Send(new DeleteWordListCommand(wordListId)
            {
                UserId = base.UserId,
            }));
        }

        [HttpGet("GetUserWordLists")]
        public async Task<IActionResult> GetUserWordLists()
        {
            var query = await sender.Send(new GetUserWordListsQuery(UserId));

            return base.OperationResult(query);
        }

        [HttpGet("GetOneWordList/{wordlistId:int}")]
        public async Task<IActionResult> GetOneWordList(int wordlistId)
        {
            var query = await sender.Send(new GetOneWordListQuery(wordlistId, base.UserId));

            return base.OperationResult(query);
        }
    }
}
