using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Application.Clients.CybersourceRestApiClient.Interfaces;
using System.Threading;
using Application.Entities;
using Microsoft.Extensions.Logging;
using LocalGovImsApiClient.Client;
using System.Linq;
using Domain.Exceptions;
using System;
using LocalGovImsApiClient.Model;
using Application.Data;

namespace Application.Commands
{
    public class ProcessUncapturedPaymentsCommand : IRequest<ProcessUncapturedPaymentsResult>
    {
        public int DaysAgo;
        public string ClientReference;

        public ProcessUncapturedPaymentsCommand(int daysAgo, string clientReference = "")
        {
            this.DaysAgo = daysAgo;
            this.ClientReference = clientReference;
        }

    }
    public class ProcessUncapturedPaymentsCommandHander : IRequestHandler<ProcessUncapturedPaymentsCommand, ProcessUncapturedPaymentsResult>
    {
        private readonly ICybersourceRestApiClient _cybersourceRestApiClient;
        private readonly ILogger<ProcessUncapturedPaymentsCommandHander> _logger;
        private readonly LocalGovImsApiClient.Api.IPendingTransactionsApi _pendingTransactionsApi;
        private readonly IAsyncRepository<Payment> _paymentRepository;

        private List<Payment> _uncapturedPayments = new();
        private List<Payment> _uncapturedCyberSourcePayments = new();
        private Payment _uncapturedPayment;
        private ProcessUncapturedPaymentsResult _processUncaturedPaymentResult;
        private int _numberOfErrors = 0;
        private int _transactionsProcessed = 0;
        private List<PendingTransactionModel> _pendingTransactions;
        private ProcessPaymentResponse _processPaymentResponse;
        private ProcessPaymentModel _processPaymentModel;


        public ProcessUncapturedPaymentsCommandHander(
            ICybersourceRestApiClient cybersourceRestApiClient,
            ILogger<ProcessUncapturedPaymentsCommandHander> logger,
            LocalGovImsApiClient.Api.IPendingTransactionsApi pendingTransactionsApi,
            IAsyncRepository<Payment> paymentRepository)
        {
            _cybersourceRestApiClient = cybersourceRestApiClient;
            _logger = logger;
            _pendingTransactionsApi = pendingTransactionsApi;
            _paymentRepository = paymentRepository;
        }

        public async Task<ProcessUncapturedPaymentsResult> Handle(ProcessUncapturedPaymentsCommand request, CancellationToken cancellationToken)
        {
            await GetPaymentTranactions(cancellationToken);
                        
            await ProcessUncapturedPayments(cancellationToken);
            
            CreateResult();

            return _processUncaturedPaymentResult;
        }
        private async Task GetPaymentTranactions(CancellationToken cancellationToken)
        {
            _uncapturedPayments = (await _paymentRepository.List(x => x.Finished != true && x.RefundReference == null)).Data;
        }

        private async Task ProcessUncapturedPayments(CancellationToken cancellationToken)
        {
            foreach (var uncapturedPayment in _uncapturedPayments)
            {
                _uncapturedPayment = uncapturedPayment;
                _uncapturedCyberSourcePayments = await _cybersourceRestApiClient.SearchPayments(_uncapturedPayment.Reference, 1);
                if (_uncapturedCyberSourcePayments != null && _uncapturedCyberSourcePayments.Any())
                {
                    _uncapturedPayment.CardPrefix = _uncapturedCyberSourcePayments.FirstOrDefault().CardPrefix;
                    _uncapturedPayment.CardSuffix = _uncapturedCyberSourcePayments.FirstOrDefault().CardSuffix;
                    _uncapturedPayment.PaymentId = _uncapturedCyberSourcePayments.FirstOrDefault().PaymentId;
                    await ProcessUncapturedPayment(cancellationToken);
                }
            }

            _logger.LogInformation(_uncapturedPayments.Count + " rows processed");
            _logger.LogInformation(_numberOfErrors + " failures. See logs for more details");
        }

        private async Task ProcessUncapturedPayment(CancellationToken cancellationToken)
        {
            try
            {
                await GetPendingTransactions(cancellationToken);

                BuildProcessPaymentModel();

                await ProcessPayment(cancellationToken);

                await UpdateIntegrationStatus(cancellationToken);
            }
            catch (Exception ex)
            {
                _numberOfErrors++;

                _logger.LogError(ex, "Unable to process uncaptured payment record: " + _uncapturedPayment.Id);
            }
        }
  
        private async Task GetPendingTransactions(CancellationToken cancellationToken)
        {
            try
            {
                _pendingTransactions = (await _pendingTransactionsApi.PendingTransactionsGetAsync(_uncapturedPayment.Reference)).ToList();

                if (_pendingTransactions == null || !_pendingTransactions.Any())
                {
                    throw new PaymentException("The reference provided is no longer a valid pending payment");
                }
            }
            catch (ApiException ex)
            {
                if (ex.ErrorCode == 404)
                    throw new PaymentException("The reference provided is no longer a valid pending payment");

                throw;
            }
        }
        private void BuildProcessPaymentModel()
        {
            _processPaymentModel = new ProcessPaymentModel()
            {
                AuthResult = LocalGovIMSResults.Authorised,
                PspReference = _uncapturedPayment.PaymentId,
                MerchantReference = _uncapturedPayment.Reference,
                Fee = 0,
                CardPrefix = _uncapturedPayment.CardPrefix,
                CardSuffix = _uncapturedPayment.CardSuffix,
                AmountPaid = _uncapturedCyberSourcePayments.FirstOrDefault().Amount
            };
        }

        private async Task ProcessPayment(CancellationToken cancellationToken)
        {
            _processPaymentResponse = await _pendingTransactionsApi.PendingTransactionsProcessPaymentAsync(_uncapturedPayment.PaymentId, _processPaymentModel);
            _transactionsProcessed++;
        }

        private async Task UpdateIntegrationStatus(CancellationToken cancellationToken)
        {
            _uncapturedPayment.Status = _processPaymentModel.AuthResult;
            _uncapturedPayment.CapturedDate = DateTime.Now;
            _uncapturedPayment.Finished = true;
            _uncapturedPayment = (await _paymentRepository.Update(_uncapturedPayment)).Data;
        }
        private void CreateResult()
        {
            _processUncaturedPaymentResult = new ProcessUncapturedPaymentsResult()
            {
                TotalIdentified = _uncapturedPayments.Count,
                TotalMarkedAsCaptured = _transactionsProcessed,
                TotalErrors = _numberOfErrors
            };
        }
    }
}
