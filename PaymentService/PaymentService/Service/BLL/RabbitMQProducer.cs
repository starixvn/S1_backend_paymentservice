using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace PaymentService.Service.BLL
{
    public interface IRabbitMQProducer
    {
        void Publish(string exchange, string routingKey, object data);
    }

    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQProducer> _logger;
        public RabbitMQProducer(IConnectionFactory connectionFactory, ILogger<RabbitMQProducer> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public void Publish(string exchange, string routingKey, object data)
        {
            var message = JsonConvert.SerializeObject(data);
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();
                
                var body = Encoding.UTF8.GetBytes(message);
                var properties = channel.CreateBasicProperties();
                properties.ContentType = "application/json";

                channel.BasicPublish(exchange: exchange,
                                     routingKey: routingKey,
                                     basicProperties: properties,
                                     body: body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"publish fail {exchange}, {routingKey}, {message}");
            }
        }
    }
}
