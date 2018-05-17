using System;

namespace Enbiso.Common.EventBus.Events
{
    public interface IIntegrationEvent
    {
        Guid Id { get; }
        DateTime CreationDate { get; }
    }
}