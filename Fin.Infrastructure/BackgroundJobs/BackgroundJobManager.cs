﻿using System.Linq.Expressions;
using Fin.Infrastructure.AutoServices.Interfaces;
using Hangfire;

namespace Fin.Infrastructure.BackgroundJobs;

public interface IBackgroundJobManager
{
    void Delete(string jobId);
    string Schedule<T>( Expression<Action<T>> methodCall, DateTimeOffset enqueueAt);
}

public class BackgroundJobManager : IBackgroundJobManager, IAutoScoped
{
    public void Delete(string jobId)
    {
        BackgroundJob.Delete(jobId);
    }

    public string Schedule<T>(Expression<Action<T>> methodCall, DateTimeOffset enqueueAt)
    {
        return BackgroundJob.Schedule<T>(methodCall, enqueueAt);
    }
}