using System.Threading.Tasks;

namespace Enbiso.Common.EventBus.Abstractions
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
