using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Enbiso.Common.EventBus.Abstractions;
using Enbiso.Common.EventBus.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Enbiso.Common.EventBus.RabbitMq
{
    public class EventBusRabbitMq : IEventBus, IDisposable
    {
        private readonly string _brokerName;
        private readonly string _autofacScopeName;

        private readonly IRabbitMqPersistentConnection _persistentConnection;
        private readonly ILogger<EventBusRabbitMq> _logger;
        private readonly IEventBusSubscriptionsManager _subscriptionsManager;
        private readonly ILifetimeScope _autofac;
        private readonly int _retryCount;
        private IModel _consumerChannel;
        private readonly string _queueName;

        public EventBusRabbitMq(IRabbitMqPersistentConnection persistentConnection, ILogger<EventBusRabbitMq> logger,
            ILifetimeScope autofac, IEventBusSubscriptionsManager subscriptionManager, string queueName = null, int retryCount = 5, 
            string brokerName = "enbiso_broker", string autofacScopeName = "enbiso_scope")
        {
            _brokerName = brokerName;
            _autofacScopeName = autofacScopeName;
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionsManager = subscriptionManager ?? new InMemoryEventBusSubscriptionsManager();
            _queueName = queueName;
            _autofac = autofac;
            _retryCount = retryCount;
            _subscriptionsManager.OnEventRemoved += SubscriptionManager_OnEventRemoved;
            _consumerChannel = CreateConsumerChannel();
        }

        public void Initialize(Action action = null)
        {
            action?.Invoke();
            var consumer = new EventingBasicConsumer(_consumerChannel);
            consumer.Received += async (model, ea) =>
            {
                var eventName = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body);
                await ProcessEvent(eventName, message);
            };
            _consumerChannel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        }

        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            var channel = _persistentConnection.CreateModel();
            channel.ExchangeDeclare(exchange: _brokerName, type: "direct");
            channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
            };

            return channel;
        }

        private async Task ProcessEvent(string eventName, string message)
        {            
            if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName)) return;

            using (var scope = _autofac.BeginLifetimeScope(_autofacScopeName))
            {
                var subscriptions = _subscriptionsManager.GetHandlersForEvent(eventName);
                foreach (var subscription in subscriptions)
                {
                    if (subscription.IsDynamic)
                    {
                        var handler =
                            scope.ResolveOptional(subscription.HandlerType) as IDynamicIntegrationEventHandler;
                        dynamic eventData = JObject.Parse(message);
                        if (handler != null) await handler.Handle(eventData);
                        else _logger.LogWarning($"Handler not found for {subscription.HandlerType} on {eventName}.");
                    }
                    else
                    {
                        var eventType = _subscriptionsManager.GetEventTypeByName(eventName);
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);
                        var handler = scope.ResolveOptional(subscription.HandlerType);
                        var concreteHandlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteHandlerType.GetMethod("Handle").Invoke(handler, new[] {integrationEvent});
                    }
                }
            }
        }

        private void SubscriptionManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
                _persistentConnection.TryConnect();

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueUnbind(_queueName, _brokerName, eventName);
            }
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex.ToString());
                });

            using (var channel = _persistentConnection.CreateModel())
            {
                var eventName = @event.GetType().Name;
                channel.ExchangeDeclare(exchange: _brokerName, type: "direct");

                var message = JsonConvert.SerializeObject(@event);
                var body = Encoding.UTF8.GetBytes(message);
                policy.Execute(() =>
                {
                    channel.BasicPublish(exchange: _brokerName, routingKey: eventName, basicProperties: null, body: body);
                });
            }
        }

        public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subscriptionsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);
            _subscriptionsManager.AddSubscription<T, TH>();
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);
            _subscriptionsManager.AddDynamicSubscription<TH>(eventName);
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subscriptionsManager.HasSubscriptionsForEvent(eventName);
            if (containsKey) return;

            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueBind(queue: _queueName, exchange: _brokerName, routingKey: eventName);
            }
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            _subscriptionsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            _subscriptionsManager.RemoveSubscription<T, TH>();
        }

        public void Dispose()
        {
            _consumerChannel?.Dispose();
            _subscriptionsManager.Clear();
        }
    }
}

