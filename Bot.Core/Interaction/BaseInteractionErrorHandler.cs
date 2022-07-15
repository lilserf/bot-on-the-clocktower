using Bot.Api;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bot.Core.Interaction
{
    public abstract class BaseInteractionErrorHandler<TKey> where TKey : notnull
    {
        private readonly IProcessLoggerFactory m_processLoggerFactory;
        private readonly ITask m_task;

        private readonly TimeSpan m_verboseTimeout = TimeSpan.FromMilliseconds(20000);

        public BaseInteractionErrorHandler(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_processLoggerFactory);
            serviceProvider.Inject(out m_task);
        }

        protected abstract string GetFriendlyStringForKey(TKey key);

        public async Task<InteractionResult> TryProcessReportingErrorsAsync(TKey key, IMember requester, Func<IProcessLogger, Task<InteractionResult>> process)
        {
            var logger = m_processLoggerFactory.Create();
            InteractionResult result = "An error occurred processing this command - please check your private messages for a detailed report.";
            try
            {
                using (var cts = new CancellationTokenSource())
                {
                    var processTask = process(logger);
                    var verboseTimeoutTask = m_task.Delay(m_verboseTimeout, cts.Token).ContinueWith(tsk => tsk.Exception == default); // ContinueWith to avoid Cancel exceptions

                    await Task.WhenAny(verboseTimeoutTask, processTask);
                    
                    if (!processTask.IsCompleted)
                    {
                        // Timeout hit, tell the process logger to start logging verbose messages
                        logger.EnableVerboseLogging();

                        // TODO: Maybe a second timeout here to skip the process and log a timeout?
                        await processTask;
                    }

                    cts.Cancel();

                    if (processTask.IsFaulted)
                    {
                        if (processTask.Exception is AggregateException ae && ae.InnerException != null)
                            throw ae.InnerException;
                        else if (processTask.Exception != null)
                            throw processTask.Exception;
                        else
                            throw new ApplicationException("Failed to retrieve exception from completed process.");
                    }

                    result = processTask.Result;
                }
            }
            catch (Exception e)
            {
                await TrySendExceptionToAuthorAsync(key, requester, e, GetFriendlyStringForKey);
            }

            result.AddLogMessages(logger.Messages);

            return result;
        }

        private static Task TrySendExceptionToAuthorAsync(TKey key, IMember requester, Exception e, Func<TKey, string> getFriendlyStringForKey)
        {
            return ExceptionReportingHelper.TrySendExceptionToMemberAsync($"Interaction error [{getFriendlyStringForKey(key)}]", requester, e);
        }
    }
}