using Application.Data;
using Application.Entities;
using LocalGovImsApiClient.Client;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class ValidateReferenceCommand : IRequest<ValidateReferenceCommandResult>
    {
        public string Reference { get; set; }
    }

    public class ValidateReferenceCommandHandler : IRequestHandler<ValidateReferenceCommand , ValidateReferenceCommandResult>
    {
        private readonly IAsyncRepository<AccountQueries> _accountQueryRepository;
        private readonly LocalGovImsApiClient.Api.IAccountHoldersApi _accountHolderApi;

        private ValidateReferenceCommandResult _result = new() { result = DefaultMessage };
        private AccountQueries _accountQuery = new();
        private const string DefaultMessage = "No Reference";

        public ValidateReferenceCommandHandler(
            IAsyncRepository<AccountQueries> accountQueryRepository,
            LocalGovImsApiClient.Api.IAccountHoldersApi accountHolderApi)
        {
            _accountQueryRepository = accountQueryRepository;
            _accountHolderApi = accountHolderApi;
        }

        public async Task<ValidateReferenceCommandResult> Handle(ValidateReferenceCommand request, CancellationToken cancellationToken)
        {
            BuildQueryPaymentModel(request);

            await GetAccountRecord(request, cancellationToken);

            await SaveQueryResponse(cancellationToken);

            return _result;
        }

        private void BuildQueryPaymentModel(ValidateReferenceCommand request)
        {
            _accountQuery.Reference = request.Reference;
            _accountQuery.CapturedDate = DateTime.Now;
        }

        private async Task GetAccountRecord(ValidateReferenceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                //validates against database
                var account = await _accountHolderApi.AccountHoldersGetAsync(request.Reference);
                if (account == null)
                {
                    _accountQuery.Status = "IMSApi not found to validate account reference";
                }
                else
                {
                    _result.result = account.FundCode;
                    _accountQuery.Status = account.FundCode;
                }
                return;
            }
            catch (ApiException exception)
            {
                if (exception.ErrorCode == 404)
                {
                    _accountQuery.Status = "Account reference not found";
                    return;
                }
                throw new Exception("Unknown error from IMSApi when validating account reference");
            }
        }

        private async Task SaveQueryResponse(CancellationToken cancellationToken)
        {
           var result =  (await _accountQueryRepository.Add(_accountQuery)).Data;
            if (result == null)
            {
                _result.result = DefaultMessage;
                throw new Exception("Error saving query details to Account Query table");
            }
        }
    }
}
