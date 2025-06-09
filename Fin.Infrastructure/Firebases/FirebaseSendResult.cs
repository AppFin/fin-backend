using FirebaseAdmin.Messaging;

namespace Fin.Infrastructure.Firebases;

public class FirebaseSendResult
{
    public bool IsSuccess { get; set; }
    public MessagingErrorCode? ErrorCode { get; set; }
}