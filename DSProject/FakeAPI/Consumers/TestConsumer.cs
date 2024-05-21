using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Contracts;
namespace FakeAPI.Consumers
{
    public class TestConsumer : IConsumer<PlaceholderContract>
    {
        public async Task Consume(ConsumeContext<PlaceholderContract> context)
        {
            var resultToSend = new PlaceholderResponse { Response = "Raspunsul cel secret..."};
            await context.RespondAsync(resultToSend);
            
        }
    }
    public class TestConsumerDefinition: ConsumerDefinition<TestConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<TestConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseFilter(new MyFilter());
        }
    }
}