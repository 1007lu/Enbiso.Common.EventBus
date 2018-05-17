using Enbiso.Common.EventBus.Abstractions;
using Enbiso.Common.EventBus.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace Enbiso.Common.EventBus.RabbitMq.Config
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// Add Event bus wth custom connection and subscription manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="option"></param>
        /// <typeparam name="TConnection"></typeparam>
        /// <typeparam name="TSubscriptionManager"></typeparam>
        public static void AddEventBusRabbitMq<TConnection, TSubscriptionManager>(this IServiceCollection services, RabbitMqOption option)
            where TConnection: class, IRabbitMqPersistentConnection
            where TSubscriptionManager: class, IEventBusSubscriptionsManager
        {
            services.AddSingleton(option);
            services.AddSingleton<IRabbitMqPersistentConnection, TConnection>();   
            services.AddSingleton<IEventBusSubscriptionsManager, TSubscriptionManager>();
            services.AddSingleton<IEventBus, EventBusRabbitMq>();
        }
        
        /// <summary>
        /// Add Event bus with default connection and manager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="option"></param>
        public static void AddEventBusRabbitMq(this IServiceCollection services, RabbitMqOption option)
        {
            services.AddEventBusRabbitMq<DefaultRabbitMqPersistentConnection, InMemoryEventBusSubscriptionsManager>(option);
        }
    }
}