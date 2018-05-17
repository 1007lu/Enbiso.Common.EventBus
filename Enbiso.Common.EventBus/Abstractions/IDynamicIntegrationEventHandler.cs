using System.Threading.Tasks;

namespace Enbiso.Common.EventBus.Abstractions
{
    /// <summary>
    /// Dynamic integration event handler interface
    /// </summary>
    public interface IDynamicIntegrationEventHandler
    {
        /// <summary>
        /// Handle dynamic event
        /// </summary>
        /// <param name="eventData"></param>
        /// <returns></returns>
        Task Handle(dynamic eventData);
    }
}
