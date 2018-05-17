using System.Threading.Tasks;
using Enbiso.Common.EventBus.Events;

namespace Enbiso.Common.EventBus.Abstractions
{
    /// <summary>
    /// Integration event handler with type event
    /// </summary>
    /// <typeparam name="TEvent"></typeparam>
    public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler where TEvent: IIntegrationEvent
    {
        Task Handle(TEvent @event);
    }

    /// <summary>
    /// Integration event handler
    /// </summary>
    public interface IIntegrationEventHandler
    {

    }
}
