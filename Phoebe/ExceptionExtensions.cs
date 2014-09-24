using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Phoebe
{
    public static class ExceptionExtensions
    {
        public static IEnumerable<Exception> TraverseTree (this Exception ex)
        {
            yield return ex;

            while (ex.InnerException != null) {
                ex = ex.InnerException;
                yield return ex;
            }
        }

        public static bool IsNetworkFailure (this Exception ex)
        {
            return ex.TraverseTree ().Any (
                       exc => exc is System.Net.Http.HttpRequestException
                       || exc is System.Net.Sockets.SocketException
                       || exc is System.Net.WebException);
        }
    }
}
