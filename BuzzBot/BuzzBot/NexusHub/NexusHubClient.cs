using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.NexusHub.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BuzzBot.NexusHub
{
    public class NexusHubClient
    {
        private readonly string _userKey;
        private readonly string _userSecret;
        private readonly HttpClient _client;
        private string _accessToken;
        private string _refreshToken;
        private CancellationTokenSource _refreshCts;
        private bool _refreshing;
        private string _server;
        private const int RefreshTimeoutMinutes = 58; //Refresh token every 58 minutes
        private const string ApiBaseAddress = @"https://api.nexushub.co";
        private const string AuthBaseAddress = @"https://auth.nexushub.co";
        public bool Initialized { get; private set; }

        public NexusHubClient(IConfiguration configuration)
        {
             _client = new HttpClient();
            _userKey = configuration["nexusHubUserKey"];
            _userSecret = configuration["nexusHubUserSecret"];
            _server = configuration["nexusHubServer"];
            _refreshCts = new CancellationTokenSource();
        }

        public async Task<NexusHubItemsViewModel> GetItem(int id, CancellationToken token)
        {
            var httpResponseMessage = await _client.GetAsync($"{ApiBaseAddress}/wow-classic/v1/items/{_server}/{id}/", token);
            if (!httpResponseMessage.IsSuccessStatusCode) return null;
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var response =  JsonConvert.DeserializeObject<NexusHubItemsViewModel>(content);
            return response;
        }

        public async Task<OverviewResponseViewModel> GetItemOverview(string server,
            CancellationToken token = default(CancellationToken))
        {
            var httpResponseMessage =
                await _client.GetAsync(@"https://api.nexushub.co/wow-classic/v1/items/Kromcrush-Horde/");
           // var httpResponseMessage = await _client.GetAsync($"{ApiBaseAddress}/wow-classic/v1/items/{server}", token);
            if (!httpResponseMessage.IsSuccessStatusCode) return null;
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<OverviewResponseViewModel>(content);
            return response;
        }

        public async Task Initialize()
        {
            var authenticationRequest = new AuthenticateRequestViewModel
            {
                UserKey = _userKey,
                UserSecret = _userSecret
            };
            var response = await Post<AuthenticateResponseViewModel>("authenticate", authenticationRequest);
            if (response == null) return;
            _accessToken = response.AccessToken;
            _refreshToken = response.RefreshToken;
#pragma warning disable 4014
            Task.Run(RefreshToken);
#pragma warning restore 4014
            Initialized = true;
        }

        private async Task RefreshToken()
        {
            while (!_refreshCts.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(RefreshTimeoutMinutes));
                _refreshing = true;
                var request = new RefreshRequestViewModel(){RefreshToken = _refreshToken};
                var response = await Post<RefreshResponseViewModel>("refresh", request);
                if (response == null)
                {
                    Initialized = false;
                    break;
                }

                _accessToken = response.AccessToken;
                _refreshing = false;
            }
        }

        private async Task<T> Post<T>(string route, object requestPayload)
        {
            var requestBody = JsonConvert.SerializeObject(requestPayload);
            var rawResponse =
                await _client.PostAsync($"{AuthBaseAddress}/{route}", new StringContent(requestBody));
            if (!rawResponse.IsSuccessStatusCode)
            {
                return default(T);
            }

            var response =
                JsonConvert.DeserializeObject<T>(
                    await rawResponse.Content.ReadAsStringAsync());
            return response;
        }
    }
}