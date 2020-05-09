using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BlazorLazyLoading
{
    public class Logger
    {
        private readonly TaskLoggingHelper _logger;

        public Logger(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public void Debug(string message, params object[] args)
        {
            _logger.LogMessage(MessageImportance.Normal, message, args);
        }

        public void Info(string message, params object[] args)
        {
            _logger.LogMessage(MessageImportance.High, message, args);
        }

        public void Error(string message, params object[] args)
        {
            _logger.LogError(message, args);
        }
    }
}
