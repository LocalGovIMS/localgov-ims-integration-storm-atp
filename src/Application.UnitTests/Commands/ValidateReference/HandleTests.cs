using Application.Commands;
using Application.Data;
using Application.Entities;
using Application.Models;
using Application.Result;
using FluentAssertions;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LocalGovImsApiClient.Model;
using Command = Application.Commands.ValidateReferenceCommand;
using Handler = Application.Commands.ValidateReferenceCommandHandler;

namespace Application.UnitTests.Commands.ValidateReference
{
    public class HandleTests
    {
        private const string SecretKey = "ddc4fc675f404a108feb82ae475cbc982da072350b7c42c6b647ae41d208a9d0ce71d501023345de981abd6a7ab1e9092f81b0c2b44845fabcc63ad9f85b4e1105be4e5446334446883e044ecd1b7c285d2a3647ccec477e9989fe0704f5920181a0b6f004f4438eba3142486e90a62b8708904253ca437e906c96de20dd0230";
        private readonly Handler _commandHandler;
        private Command _command;

        private readonly Mock<IAsyncRepository<AccountQueries>> _accountQueryRepository = new Mock<IAsyncRepository<AccountQueries>>();
        private readonly Mock<LocalGovImsApiClient.Api.IAccountHoldersApi> _accountHolderApi = new Mock<LocalGovImsApiClient.Api.IAccountHoldersApi>();

        public HandleTests()
        {
            _commandHandler = new Handler(
                _accountQueryRepository.Object,
                _accountHolderApi.Object);

            SetupConfig();
            SetupCommand("123456");

        }

        private void SetupConfig()
        {

            _accountHolderApi.Setup(x => x.AccountHoldersGetAsync(It.IsAny<string>(), 0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccountHolderModel());

            _accountQueryRepository.Setup(x => x.Add(It.IsAny<AccountQueries>()))
                .ReturnsAsync(new OperationResult<AccountQueries>(true) { Data = new AccountQueries() });

        }


        private void SetupCommand(string reference)
        {
            _command = new Command() { Reference = reference};
        }

        [Fact]
        public async Task Handle_returns_a_ValidateReferenceCommandResult()
        {
            // Arrange


            // Act
            var result = await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            result.Should().BeOfType<ValidateReferenceCommandResult>();
        }

        [Fact]
        public async Task Should_return_no_reference_when_IMSapi_is_not_available()
        {
            // Arrange
            AccountHolderModel accountHolderModel = new ();
            accountHolderModel = null;
            _accountHolderApi.Setup(x => x.AccountHoldersGetAsync(It.IsAny<string>(), 0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accountHolderModel);

            // Act
            var result = await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            result.Should().BeOfType<ValidateReferenceCommandResult>();
            result.result.Should().Contain("No Reference");
        }

        [Fact]
        public async Task Should_return_fund_code_when_account_exists()
        {
            // Arrange
            AccountHolderModel accountHolderModel = new();
            accountHolderModel.FundCode = "05";
            _accountHolderApi.Setup(x => x.AccountHoldersGetAsync(It.IsAny<string>(), 0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(accountHolderModel);

            // Act
            var result = await _commandHandler.Handle(_command, new System.Threading.CancellationToken());

            // Assert
            result.Should().BeOfType<ValidateReferenceCommandResult>();
            result.result.Should().Contain("05");
        }
    }
}
