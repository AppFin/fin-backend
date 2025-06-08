using Fin.Infrastructure.AutoServices.Interfaces;
using FirebaseAdmin.Messaging;

namespace Fin.Infrastructure.Firebases;

public interface IFirebaseClient
{
    Task<(int FailureCount, IReadOnlyList<FirebaseSendResult> Responses)> SendEachAsync(IReadOnlyList<Message> messages);
}

public class FirebaseClient : IFirebaseClient, IAutoTransient
{
    public async Task<(int, IReadOnlyList<FirebaseSendResult>)> SendEachAsync(IReadOnlyList<Message> messages)
    {
        var sdkResponse = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages);

        var results = sdkResponse.Responses.Select(r => new FirebaseSendResult
        {
            IsSuccess = r.IsSuccess,
            ErrorCode = r.Exception?.MessagingErrorCode
        }).ToList();

        return (sdkResponse.FailureCount, results);
    }
}