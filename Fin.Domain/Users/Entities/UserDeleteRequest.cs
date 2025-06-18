using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Users.Entities;

public class UserDeleteRequest : IAuditedEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; }

    public Guid? UserAbortedId { get; private set; }
    public virtual User UserAborted { get;  set; }
    public DateTime AbortedAt { get; private set; }
    public bool Aborted { get; private set; }

    public DateTime DeleteRequestedAt { get; set; }
    public DateOnly DeleteEffectivatedAt { get; set; }


    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserDeleteRequest()
    {
    }

    public UserDeleteRequest(Guid userId, DateTime deleteRequestedAt, int daysToDelete  = 30)
    {
        UserId = userId;
        DeleteRequestedAt = deleteRequestedAt;
        DeleteEffectivatedAt = DateOnly.FromDateTime(deleteRequestedAt).AddDays(daysToDelete);
    }

    public void Abort(Guid userAbortedId, DateTime abortedAt)
    {
        if (Aborted) return;

        UserAbortedId = userAbortedId;
        AbortedAt = abortedAt;
        Aborted = true;
    }

}