using ReportPortal.Client;
using ReportPortal.Client.Abstractions;
using ReportPortal.Shared.Configuration;
using System;

namespace ReportPortal.Shared.Reporter.Http
{
    public class ClientServiceBuilder
    {
        private readonly IConfiguration _configuration;

        private HttpClientHandlerFactory _httpClientHandlerFactory;

        private HttpClientFactory _httpClientFactory;

        public ClientServiceBuilder(IConfiguration configuration)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;
        }

        public ClientServiceBuilder UseHttpClientHandlerFactory(HttpClientHandlerFactory httpClientHandlerFactory)
        {
            _httpClientHandlerFactory = httpClientHandlerFactory;

            return this;
        }

        public ClientServiceBuilder UseHttpClientFactory(HttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;

            return this;
        }

        public IClientService Build()
        {
            var url = _configuration.GetValue<string>(ConfigurationPath.ServerUrl);

            var project = _configuration.GetValue<string>(ConfigurationPath.ServerProject);

            var token = _configuration.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid);

            if (_httpClientHandlerFactory is null)
            {
                _httpClientHandlerFactory = new HttpClientHandlerFactory(_configuration);
            }

            if (_httpClientFactory is null)
            {
                _httpClientFactory = new HttpClientFactory(_configuration, _httpClientHandlerFactory.Create());
            }

            IClientService service = new Service(new Uri(url), project, token, _httpClientFactory);

            return service;
        }
    }
}
