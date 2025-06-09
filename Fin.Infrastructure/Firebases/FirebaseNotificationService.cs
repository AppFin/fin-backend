using Fin.Infrastructure.AutoServices.Interfaces;
using FirebaseAdmin.Messaging;

namespace Fin.Infrastructure.Firebases;

public interface IFirebaseNotificationService
{
    Task<List<string>> SendPushNotificationAsync(List<Message> messages);
}

public class FirebaseNotificationService(IFirebaseClient firebaseClient): IFirebaseNotificationService, IAutoTransient
{
    public async Task<List<string>> SendPushNotificationAsync(List<Message> messages)
    {
        var (failureCount, responses) = await firebaseClient.SendEachAsync(messages);

        var toRemoveTokenErrors = new List<MessagingErrorCode>
        {
            MessagingErrorCode.InvalidArgument,
            MessagingErrorCode.Unregistered,
            MessagingErrorCode.SenderIdMismatch
        };

        var tokensToRemove = new List<string>();
        for (var i = 0; i < responses.Count; i++)
        {
            var result = responses[i];
            if (result.IsSuccess) continue;

            var errorCode = result.ErrorCode;
            var token = messages[i].Token;

            if (!errorCode.HasValue || !toRemoveTokenErrors.Contains(errorCode.Value)) continue;
            tokensToRemove.Add(token);
        }

        return tokensToRemove;
    }
}