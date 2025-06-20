using Fin.Domain.Users.Entities;

namespace Fin.Domain.Users.Dtos;

public class UserDeleteRequestDto
{
    public Guid UserId { get; set; }
    public string UserName{ get; set; }

    public Guid? UserAbortedId { get; private set; }
    public string UserAbortedName { get;  set; }
    public DateTime? AbortedAt { get; private set; }
    public bool Aborted { get; private set; }

    public DateTime DeleteRequestedAt { get; set; }
    public DateOnly DeleteEffectivatedAt { get; set; }

    public Guid Id { get; set; }

    public UserDeleteRequestDto()
    {
    }

    public UserDeleteRequestDto(UserDeleteRequest deleteRequest)
    {
        Id = deleteRequest.Id;

        UserId = deleteRequest.UserId;
        UserName = deleteRequest.User.DisplayName;

        UserAbortedId = deleteRequest.UserAbortedId;;
        UserAbortedName = deleteRequest.UserAborted?.DisplayName;;
        AbortedAt = deleteRequest.AbortedAt;
        Aborted = deleteRequest.Aborted;
        DeleteRequestedAt = deleteRequest.DeleteRequestedAt;
        DeleteEffectivatedAt = deleteRequest.DeleteEffectivatedAt;
    }
}