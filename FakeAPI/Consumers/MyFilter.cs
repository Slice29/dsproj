using MassTransit;
using System.Diagnostics;

namespace FakeAPI.Consumers
{
    public class MyFilter : IFilter<ConsumeContext>
    {
        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("myConsumeFilter");
            context.Add("output", "console");
        }

        public Task Send(ConsumeContext context, IPipe<ConsumeContext> next)
        {
            var auth = context.Headers.TryGetHeader("isAdmin", out var authValue);
            Debug.WriteLine("UITE Consumul {0}", authValue);
            return next.Send(context);
        }
    }
}
