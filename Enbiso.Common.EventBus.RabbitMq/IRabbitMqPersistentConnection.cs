using System;
using RabbitMQ.Client;

namespace Enbiso.Common.EventBus.RabbitMq
{
    public interface IRabbitMqPersistentConnection: IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateModel();
    }
}