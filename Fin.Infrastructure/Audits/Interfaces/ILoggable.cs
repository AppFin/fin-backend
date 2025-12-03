namespace Fin.Infrastructure.Audits.Interfaces;

public interface ILoggable
{
    object GetLogSnapshot();
    string GetLogId();
}