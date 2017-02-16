using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.Policies;
using Ardi.ApacheNMS.Client;

//FYI: here's better but more complex implementation from MS
//http://www.getcodesamples.com/src/E957273E/6E323CDF

//FYI: a simple implementation of using pub/sub integration pattern
//https://remark.wordpress.com/articles/publish-subscribe-with-activemq-and-nms/

namespace Ardi.ApacheNMS.Client
{
    public class ActiveMqChannel : IActiveMqChannel, IDisposable
    {
        private readonly Lazy<IConnection> _connection;
        private readonly Lazy<ISession> _session;

        private readonly IDictionary<string, Lazy<IQueueSender>> _senders = new Dictionary<string, Lazy<IQueueSender>>();
        private readonly IDictionary<string, Lazy<IQueueReceiver>> _receivers = new Dictionary<string, Lazy<IQueueReceiver>>();

        //var connectionFactory = new ConnectionFactory("failover:tcp://mybroker:61613")
        public ActiveMqChannel(string serverAddress, string userName, string password)
        {
            var connectionSettings = new
            {
                //disable all async handling
                AsyncSend = false,
                SendAcksAsync = false,
                DispatchAsync = false,

                //milliseconds, 1000ms = 1sec
                InitialRedeliveryDelay = 500,
                RedeliveryDelay = 2000,

                //how many time message will redeliver before putting to dead letter queue
                MaximumRedeliveries = 3,

                //redelivery will delay pow(delay) on each attempt
                UseExponentialBackOff = false,

                //messages are acknowledge/successful if the listener Receive doesnt throw any error
                AcknowledgementMode = AcknowledgementMode.AutoAcknowledge,

                //an active compression would requires a gzip dependency
                UserCompression = false
            };

            Trace.TraceInformation($"ActiveMqChannel: Initializing channel with ff settings: {connectionSettings}");

            _connection = new Lazy<IConnection>(() =>
            {
                var connectionUri = new Uri(serverAddress);

                var factory = new ConnectionFactory(connectionUri)
                {
                    AsyncSend = connectionSettings.AsyncSend,
                    SendAcksAsync = connectionSettings.SendAcksAsync,
                    DispatchAsync = connectionSettings.DispatchAsync,
                    UseCompression = connectionSettings.UserCompression
                };

                Trace.TraceInformation("ActiveMqChannel: Creating AMQ connection from factory", serverAddress);
                var connection = factory.CreateConnection(userName, password);
                Trace.TraceInformation("ActiveMqChannel: Connection instance created");

                connection.ConnectionInterruptedListener += Connection_ConnectionInterruptedListener;
                connection.ConnectionResumedListener += Connection_ConnectionResumedListener;
                connection.ExceptionListener += exception =>
                {
                    Trace.TraceError($"ActiveMqChannel : Error Encounterd. {exception.ToString()}");
                    throw exception;
                };

                var connectionRedeliveryPolicy = new RedeliveryPolicy();
                connectionRedeliveryPolicy.InitialRedeliveryDelay = connectionSettings.InitialRedeliveryDelay;
                connectionRedeliveryPolicy.RedeliveryDelay(connectionSettings.RedeliveryDelay);
                connectionRedeliveryPolicy.MaximumRedeliveries = connectionSettings.MaximumRedeliveries;
                connectionRedeliveryPolicy.UseExponentialBackOff = connectionSettings.UseExponentialBackOff;
                connection.RedeliveryPolicy = connectionRedeliveryPolicy;

                if (!connection.IsStarted)
                {
                    Trace.TraceInformation($"ActiveMqChannel: Connecting to {serverAddress}");
                    connection.Start();
                    Trace.TraceInformation("ActiveMqChannel: Connection started.");
                }

                return connection;
            });

            _session = new Lazy<ISession>(() =>
            {
                var connection = _connection.Value;

                Trace.TraceInformation("ActiveMqChannel: Creating session");
                var session = connection.CreateSession(connectionSettings.AcknowledgementMode);
                Trace.TraceInformation("ActiveMqChannel: Session created");

                if (!connection.IsStarted)
                {
                    Trace.TraceInformation($"ActiveMqChannel: Connecting to {serverAddress}");
                    connection.Start();
                    Trace.TraceInformation("ActiveMqChannel: Connection started.");
                }

                return session;
            });
        }

        private void Connection_ConnectionResumedListener()
        {
            Trace.TraceWarning("ActiveMqChannel: Connection to queue was restored");
        }

        private void Connection_ConnectionInterruptedListener()
        {
            Trace.TraceWarning("ActiveMqChannel: Connection to queue was interrupted");
        }

        public Lazy<ISession> Session => _session;

        public Lazy<IConnection> Connection => _connection;

        public IQueueSender CreateSender(string destinationName, DestinationType destinationType)
        {
            //sender was previously created, dispose it
            if (_senders.ContainsKey(destinationName))
            {
                return _senders[destinationName].Value;
            }
            else
            {
                var tokenSource = new CancellationTokenSource();
                var token = tokenSource.Token;
                var timeOutMs = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

                var task = Task.Factory.StartNew(() =>
                {
                    if (destinationType == DestinationType.Queue)
                    {
                        Trace.TraceInformation($"ActiveMqChannel: Creating sender to queue {destinationName}", destinationName);

                        var sender = new Lazy<IQueueSender>(() => new QueueSender(_session.Value, destinationName));
                        var queueSender = sender.Value;
                        _senders.Add(destinationName, sender);

                        Trace.TraceInformation($"ActiveMqChannel: Created sender for {destinationName}", destinationName);

                        return queueSender;
                    }
                    else if (destinationType == DestinationType.Topic)
                    {
                        Trace.TraceInformation($"ActiveMqChannel: Creating sender to queue {destinationName}", destinationName);

                        var topic = new ActiveMQTopic(destinationName);
                        var sender = new Lazy<IQueueSender>(() => new QueueSender(_session.Value, topic));
                        var queueSender = sender.Value;
                        _senders.Add(destinationName, sender);

                        Trace.TraceInformation($"ActiveMqChannel: Created sender for {destinationName}", destinationName);

                        return queueSender;
                    }
                    else throw new NotSupportedException($"Destination type {destinationType} not supported.");

                }, token);

                if (!task.Wait(timeOutMs, token))
                {
                    Trace.TraceError($"Failed to create queue sender. Timeout after {timeOutMs} elapsed.");
                }

                return task.Result;
            }
        }

        public IQueueReceiver CreateReceiver(string sourceName, DestinationType destinationType)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            var timeOutMs = (int)TimeSpan.FromSeconds(15).TotalMilliseconds;

            var task = Task.Factory.StartNew(() =>
            {
                if (destinationType == DestinationType.Queue)
                {
                    Trace.TraceInformation($"ActiveMqChannel: Creating receiver from queue {sourceName}", sourceName);

                    var receiver = new Lazy<IQueueReceiver>(() => new QueueReceiver(_session.Value, sourceName));
                    var queueReceiver = receiver.Value;
                    _receivers.Add(sourceName, receiver);

                    Trace.TraceInformation($"ActiveMqChannel: Created receiver for {sourceName}", sourceName);

                    return queueReceiver;
                }
                else if (destinationType == DestinationType.Topic)
                {
                    Trace.TraceInformation($"ActiveMqChannel: Creating receiver from queue {sourceName}", sourceName);

                    var topic = new ActiveMQTopic(sourceName);
                    var receiver = new Lazy<IQueueReceiver>(() => new QueueReceiver(_session.Value, topic));
                    var queueReceiver = receiver.Value;
                    _receivers.Add(sourceName, receiver);

                    Trace.TraceInformation($"ActiveMqChannel: Created receiver for {sourceName}", sourceName);

                    return queueReceiver;
                }
                else throw new NotSupportedException($"Destination type {destinationType} not supported.");
            }, token);

            if (!task.Wait(timeOutMs, token))
            {
                Trace.TraceError($"Failed to create queue receiver. Timeout after {timeOutMs} elapsed.");
            }

            return task.Result;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (!disposing)
                    return;

                try
                {
                    try
                    {
                        _connection.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("ActiveMqChannel: Error disposing NMS connection", ex);
                    }

                    try
                    {
                        _session.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("ActiveMqChannel: Error disposing NMS session", ex);
                    }

                    try
                    {
                        _senders.ToList().ForEach(s => s.Value.Value.Dispose());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("ActiveMqChannel: Error disposing NMS producers", ex);
                    }

                    try
                    {
                        _receivers.ToList().ForEach(r => r.Value.Value.Dispose());
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("ActiveMqChannel: Error disposing NMS consumers", ex);
                    }

                }
                finally
                {
                    Trace.TraceError("ActiveMqChannel: ActiveMQ channel disposed");
                }
            }

            this.disposed = true;
        }
    }
}
