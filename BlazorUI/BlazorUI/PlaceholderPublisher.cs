using Contracts;
using MassTransit;
using System.Threading.Tasks;

namespace BlazorUI
{
    public class PlaceholderPublisher
    {
        private readonly IRequestClient<PlaceholderContract> _requestClient;
        private readonly ApiAuthenticationStateProvider _authenticationStateProvider;

        public PlaceholderPublisher(IRequestClient<PlaceholderContract> requestClient, ApiAuthenticationStateProvider authenticationStateProvider)
        {
            _requestClient = requestClient;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<PlaceholderResponse> GetMyResponse(string myMessage)
        {
            var isAdmin = await _authenticationStateProvider.IsUserAdminAsync();

            var request = _requestClient.Create(new PlaceholderContract
            {
                Test = myMessage
            });

            // Set headers on the request
            request.UseExecute(context =>
            {
                context.Headers.Set("isAdmin", isAdmin.ToString().ToLower());
            });

            var response = await request.GetResponse<PlaceholderResponse>();
            return response.Message;
        }
    }
}
