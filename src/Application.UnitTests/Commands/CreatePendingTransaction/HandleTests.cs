using Application.Commands;
using Application.Data;
using Application.Entities;
using System.Threading;
using Application.Result;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Command = Application.Commands.CreatePendingTransactionCommand;
using Handler = Application.Commands.CreatePendingTransactionCommandHandler;
using LocalGovImsApiClient.Model;

namespace Application.UnitTests.Commands.CreatePendingTransaction
{
    public class HandleTests
    {
        private const string SecretKey = "ddc4fc675f404a108feb82ae475cbc982da072350b7c42c6b647ae41d208a9d0ce71d501023345de981abd6a7ab1e9092f81b0c2b44845fabcc63ad9f85b4e1105be4e5446334446883e044ecd1b7c285d2a3647ccec477e9989fe0704f5920181a0b6f004f4438eba3142486e90a62b8708904253ca437e906c96de20dd0230";
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<IConfiguration> _mockConfiguration = new Mock<IConfiguration>();
        private readonly Mock<IAsyncRepository<Payment>> _mockPaymentRepository = new Mock<IAsyncRepository<Payment>>();
        private readonly Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi> _mockPendingTransactionsApi = new Mock<LocalGovImsApiClient.Api.IPendingTransactionsApi>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _mockConfiguration.Object,
                _mockPaymentRepository.Object,
                _mockPendingTransactionsApi.Object);

            SetupConfig();
            SetupCommand("test", "05", "500", "123456789");

        }

        private void SetupConfig()
        {
            var officeCodeConfigSection = new Mock<IConfigurationSection>();
            officeCodeConfigSection.Setup(a => a.Value).Returns("SP");
            _mockConfiguration.Setup(x => x.GetSection("TransactionDetails:OfficeCode")).Returns(officeCodeConfigSection.Object);

            var MopCodeConfigSection = new Mock<IConfigurationSection>();
            MopCodeConfigSection.Setup(a => a.Value).Returns("SP");
            _mockConfiguration.Setup(x => x.GetSection("TransactionDetails:MethodOfPaymentCode")).Returns(MopCodeConfigSection.Object);

            _mockPaymentRepository.Setup(x => x.Add(It.IsAny<Payment>()))
                .ReturnsAsync(new OperationResult<Payment>(true) { Data = new Payment() });

            PendingTransactionModel model = new PendingTransactionModel
            {
                Amount = 123,
                Reference = "123",
                InternalReference = "456789"
            };
            List<PendingTransactionModel> results = new List<PendingTransactionModel>();
            results.Add(model);

            _mockPendingTransactionsApi.Setup(x => x.PendingTransactionsPostAsync(It.IsAny<PendingTransactionModel>(), 0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);
        }


        private void SetupCommand(string reference, string  type, string amount, string phoneNumber)
        {
            _command = new Command() { Reference = reference, Type = type, Amount = amount, PhoneNumber = phoneNumber};
        }

        [Fact]
        public async Task Handle_returns_a_CreatePendingTransactionCommandResult()
        {
            // Arrange

            // Act
            var result = await _commandHandler.Handle(_command, new CancellationToken());

            // Assert
            result.Should().BeOfType<CreatePendingTransactionCommandResult>();
            result.Successful.Should().Be(true);
            result.Reference.Should().Be("456789");
        }
    }
}
