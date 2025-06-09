using Fin.Infrastructure.Firebases;
using FirebaseAdmin.Messaging;
using FluentAssertions;
using Moq;

namespace Fin.Test.Firebases
{
    public class FirebaseNotificationServiceTest : TestUtils.BaseTest
    {
        private readonly Mock<IFirebaseClient> _firebaseClientMock;
        private readonly FirebaseNotificationService _service;

        public FirebaseNotificationServiceTest()
        {
            _firebaseClientMock = new Mock<IFirebaseClient>();
            _service = new FirebaseNotificationService(_firebaseClientMock.Object);
        }

        [Fact]
        public async Task SendPushNotificationAsync_ShouldReturnTokens_OnlyForRemovableErrorCodes()
        {
            // Arrange
            var messages = new List<Message>
            {
                new() { Token = "success-token" },
                new() { Token = "unregistered-token" }, // This error code means the token should be removed.
                new() { Token = "internal-error-token" }, // This error code means a temporary failure, token should NOT be removed.
                new() { Token = "invalid-arg-token" }, // This error code means the token should be removed.
            };

            // Create a fake response that the mocked client will return.
            var fakeResponses = new List<FirebaseSendResult>
            {
                new() { IsSuccess = true },
                new() { IsSuccess = false, ErrorCode = MessagingErrorCode.Unregistered },
                new() { IsSuccess = false, ErrorCode = MessagingErrorCode.Internal },
                new() { IsSuccess = false, ErrorCode = MessagingErrorCode.InvalidArgument }
            };
            var fakeResult = (FailureCount: 3, Responses: (IReadOnlyList<FirebaseSendResult>)fakeResponses);

            _firebaseClientMock
                .Setup(c => c.SendEachAsync(messages))
                .ReturnsAsync(fakeResult);

            // Act
            var tokensToRemove = await _service.SendPushNotificationAsync(messages);

            // Assert
            tokensToRemove.Should().NotBeNull();
            tokensToRemove.Should().HaveCount(2);

            // It should contain tokens whose errors are in the removable list.
            tokensToRemove.Should().Contain("unregistered-token");
            tokensToRemove.Should().Contain("invalid-arg-token");

            // It should NOT contain tokens that were successful or had non-removable errors.
            tokensToRemove.Should().NotContain("success-token");
            tokensToRemove.Should().NotContain("internal-error-token");
        }
    }
}