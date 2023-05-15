using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace PaymentService.Service.BLL
{
    public interface IRabbitMQProducer
    {
        void PublishDepositSuccess(int custId);
    }

    public class RabbitMQProducer : IRabbitMQProducer
    {
        private readonly IConnectionFactory _connectionFactory;

        public RabbitMQProducer(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void PublishDepositSuccess(int custId)
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var data = new
            {
                CustId = custId
            };

            var message = JsonConvert.SerializeObject(data);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "MemberExchange",
                                 routingKey: "deposit",
                                 basicProperties: null,
                                 body: body);
        }
    }
}
