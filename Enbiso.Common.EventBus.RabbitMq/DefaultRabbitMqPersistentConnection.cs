﻿using System;
using System.Net.Sockets;
using Enbiso.Common.EventBus.RabbitMq.Config;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using Polly;
using RabbitMQ.Client.Exceptions;

namespace Enbiso.Common.EventBus.RabbitMq
{
    public class DefaultRabbitMqPersistentConnection : IRabbitMqPersistentConnection
    {
        private IConnection _connection;
        private bool _disposed;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<DefaultRabbitMqPersistentConnection> _logger;
        private readonly int _retryCount;
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Create default Rabbit connection
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="option"></param>
        public DefaultRabbitMqPersistentConnection(
            ILogger<DefaultRabbitMqPersistentConnection> logger, 
            RabbitMqOption option)
        {
            _connectionFactory = new ConnectionFactory
            {
                HostName = option.Server ?? throw new ArgumentNullException(nameof(option.Server)),
            };
            if(!string.IsNullOrEmpty(option.UserName))
                _connectionFactory.UserName = option.UserName;
            if(!string.IsNullOrEmpty(option.Password))
                _connectionFactory.Password = option.Password;
            if (!string.IsNullOrEmpty(option.VirtualHost))
                _connectionFactory.VirtualHost = option.VirtualHost;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryCount = option.RetryCount;
        }

        /// <inheritdoc />
        public bool IsConnected 
            => _connection != null && _connection.IsOpen && !_disposed;

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _connection.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex.ToString());                
            }
        }

        /// <inheritdoc />
        public bool TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");
            lock (_syncRoot)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (ex, time) =>
                        {
                            _logger.LogWarning(ex.ToString());
                        });

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if (!IsConnected)
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");
                    return false;
                }

                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation(
                    $"RabbitMQ persistent connection acquired a connection {_connection.Endpoint.HostName} and is subscribed to failure events");
                return true;
            }
        }

        /// <inheritdoc />
        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        #region private methods

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");
            TryConnect();
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");
            TryConnect();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;
            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
            TryConnect();
        }

        #endregion
    }
}
