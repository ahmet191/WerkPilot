using System;

namespace OmniFlow.Application.Common.Exceptions;

public class ApiException : Exception
{
    public string MessageKey { get; }

    public ApiException(string messageKey) : base(messageKey)
    {
        MessageKey = messageKey;
    }
}
