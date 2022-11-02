using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Application.Models;
using Application.Commands;

namespace Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StormApiController : BaseController
    {
        private readonly ILogger<StormApiController> _logger;

        private const string DefaultErrorMessage = "Unable to process the payment";

        public StormApiController(
            ILogger<StormApiController> logger)
        {
            _logger = logger;
        }

        [HttpPost("ValidateReference")]
        public async Task<IActionResult> ValidateReference(ValidateReferenceModel model)
        {
            try
            {
                var result = await Mediator.Send(new ValidateReferenceCommand()
                {
                    Reference = model.Data.Reference
                }); 
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("CreatePendingTransaction")]
        public async Task<IActionResult> CreatePendingTransaction(CreatePendingTransactionModel model)
        {
            try
            {
                CreatePendingTransactionCommandResult result = new();
                var validateResult = await Mediator.Send(new ValidateReferenceCommand()
                {
                    Reference = model.Data.Reference
                });
                if (validateResult.result.Length == 2)
                {
                    result = await Mediator.Send(new CreatePendingTransactionCommand()
                    {
                        Reference = model.Data.Reference,
                        Type = validateResult.result,
                        Amount = model.Data.Amount,
                        PhoneNumber = model.Data.PhoneNumber
                    });
                }    
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, DefaultErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
