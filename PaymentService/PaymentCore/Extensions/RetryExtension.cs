namespace PaymentCore.Extensions;

public class RetryExtension
{
    public static TResult Do<T, TResult>(Func<T, TResult> func, T input, int maxRetryTimes, TimeSpan retryInterval)
    {
        var exceptions = new List<Exception>();
        for (var i = 0; i < maxRetryTimes; i++)
        {
            try
            {
                if (i > 0)
                {
                    Thread.Sleep(retryInterval);
                }
                return func(input);
            }
            catch(Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        throw new AggregateException(exceptions);
    }
}