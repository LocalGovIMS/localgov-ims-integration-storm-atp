using Application.Data;
using Application.Entities;
using LocalGovImsApiClient.Model;
using MediatR;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class CreatePendingTransactionCommand : IRequest<CreatePendingTransactionCommandResult>
    {
        public string Reference { get; set; }
        public string Type { get; set; }
        public string Amount { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class CreatePendingTransactionCommandHandler : IRequestHandler<CreatePendingTransactionCommand, CreatePendingTransactionCommandResult>
    {
        private readonly IConfiguration _configuration;
        private readonly IAsyncRepository<Payment> _paymentRepository;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApi _pendingTransactionsApi;

        private CreatePendingTransactionCommandResult _result = new();
        private PendingTransactionModel _pendingTransaction = new();
        private Payment _payment;

        public CreatePendingTransactionCommandHandler(
            IConfiguration configuration,
            IAsyncRepository<Payment> paymentRepository,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi)
        {
            _configuration = configuration;
            _paymentRepository = paymentRepository;
            _pendingTransactionsApi = pendingTransactionsApi;

        }

        public async Task<CreatePendingTransactionCommandResult> Handle(CreatePendingTransactionCommand request, CancellationToken cancellationToken)
        {
            BuildPendingTransaction(request);

            await CreatePendingTransaction(cancellationToken);

            await CreateIntegrationPayment(cancellationToken);
            
            return _result;

        }

        public void BuildPendingTransaction(CreatePendingTransactionCommand request)
        {
            _pendingTransaction.OfficeCode = _configuration.GetValue<string>("TransactionDetails:OfficeCode");
            _pendingTransaction.TransactionDate = DateTime.Now;
            _pendingTransaction.AccountReference = request.Reference;
            _pendingTransaction.FundCode = request.Type;
            _pendingTransaction.MopCode = _configuration.GetValue<string>("TransactionDetails:MethodOfPaymentCode");
            decimal.TryParse(request.Amount, out decimal amount);
            _pendingTransaction.Amount = amount / 100;
            _pendingTransaction.Narrative = request.PhoneNumber;
        }

        public async Task CreatePendingTransaction(CancellationToken cancellationToken)
        {
            if (_pendingTransaction.Amount > 0)
            {
                var result = await _pendingTransactionsApi.PendingTransactionsPostAsync(_pendingTransaction);
                _result.Reference = result.First().InternalReference;
                _result.Successful = true;
            }
        }

        public async Task CreateIntegrationPayment(CancellationToken cancellationToken)
        {
            _payment = (await _paymentRepository.Add(new Payment()
            {
                Amount = _pendingTransaction.Amount.Value,
                CreatedDate = DateTime.Now,
                Reference = _result.Reference
              
            })).Data;
        }
    }
}
