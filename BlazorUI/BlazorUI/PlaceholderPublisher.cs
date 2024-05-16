using Contracts;
using MassTransit;

namespace BlazorUI
{
    public class PlaceholderPublisher
    {
        private readonly IRequestClient<PlaceholderContract> _requestClient;

        public PlaceholderPublisher(IRequestClient<PlaceholderContract> requestClient)
        {
            _requestClient = requestClient;
        }
        public async Task<PlaceholderResponse> GetMyResponse(string myMessage)
        {
            var response = await _requestClient.GetResponse<PlaceholderResponse>(new
            {
                Test = "test"
            });
            var responseMessage = response;
            return response.Message;
        }
    }
}
