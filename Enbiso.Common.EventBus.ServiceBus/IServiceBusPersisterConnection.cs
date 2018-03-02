using System;
using Microsoft.Azure.ServiceBus;

namespace Enbiso.Common.EventBus.ServiceBus
{
    public interface IServiceBusPersisterConnection : IDisposable
    {
        ServiceBusConnectionStringBuilder ServiceBusConnectionStringBuilder { get; }

        ITopicClient CreateModel();
    }
}