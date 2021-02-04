using ReportPortal.Client;
using ReportPortal.Client.Extentions;
using ReportPortal.Shared.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ReportPortal.Shared.Reporter.Http
{
    public class HttpClientFactory : IHttpClientFactory
    {
        public HttpClientFactory(IConfiguration configuration, HttpClientHandler httpClientHandler)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            if (httpClientHandler is null) throw new ArgumentNullException(nameof(httpClientHandler));

            Configuration = configuration;
            HttpClientHandler = httpClientHandler;
        }

        protected IConfiguration Configuration { get; }

        protected HttpClientHandler HttpClientHandler { get; }

        public virtual HttpClient Create()
        {
            var httpClient = new HttpClient(HttpClientHandler);

            var url = Configuration.GetValue<string>(ConfigurationPath.ServerUrl);

            var token = Configuration.GetValue<string>(ConfigurationPath.ServerAuthenticationUuid);

            httpClient.BaseAddress = new Uri(url).Normalize();

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            httpClient.DefaultRequestHeaders.Add("User-Agent", ".NET Reporter");

            return httpClient;
        }
    }
}
