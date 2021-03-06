﻿using System;
using System.Threading.Tasks;
using Enbiso.Common.EventBus.Events;

namespace Enbiso.Common.EventBus.Abstractions
{
    /// <summary>
    /// Event bus interface
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// Initialize eventbus
        /// </summary>
        /// <param name="action"></param>
        void Initialize(Action action = null);

        /// <summary>
        /// Publish event
        /// </summary>
        /// <param name="event"></param>
        void Publish(IIntegrationEvent @event);

        /// <summary>
        /// Subscribe to events
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="TEventHandler"></typeparam>
        void Subscribe<TEvent, TEventHandler>()
            where TEvent : IIntegrationEvent
            where TEventHandler : IIntegrationEventHandler<TEvent>;

        /// <summary>
        /// Subscribe to events dynamically
        /// </summary>
        /// <param name="eventName"></param>
        /// <typeparam name="TEventHandler"></typeparam>
        void SubscribeDynamic<TEventHandler>(string eventName)
            where TEventHandler : IDynamicIntegrationEventHandler;

        /// <summary>
        /// Unsusbcribe to event dynamically
        /// </summary>
        /// <param name="eventName"></param>
        /// <typeparam name="TEventHandler"></typeparam>
        void UnsubscribeDynamic<TEventHandler>(string eventName)
            where TEventHandler : IDynamicIntegrationEventHandler;

        /// <summary>
        /// Unsubscribe to events
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="TEventHandler"></typeparam>
        void Unsubscribe<TEvent, TEventHandler>()
            where TEventHandler : IIntegrationEventHandler<TEvent>
            where TEvent : IIntegrationEvent;
    }
}
