namespace AuthorizeNet.Utilities
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public static class LogFactory
    {

        private static ILoggerFactory LoggerFactory => new LoggerFactory();

        public static ILogger getLog(Type classType)
        {
            return LoggerFactory?.CreateLogger(classType.FullName) ?? NullLogger.Instance;
        }
    }
}