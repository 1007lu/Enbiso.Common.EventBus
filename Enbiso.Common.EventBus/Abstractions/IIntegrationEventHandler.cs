using System.Threading.Tasks;
using Enbiso.Common.EventBus.Events;

namespace Enbiso.Common.EventBus.Abstractions
{
    public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler where TIntegrationEvent: IntegrationEvent
    {
        Task Handle(TIntegrationEvent @event);
    }

    public interface IIntegrationEventHandler
    {

    }
}
