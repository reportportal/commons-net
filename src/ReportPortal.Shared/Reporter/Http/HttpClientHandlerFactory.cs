using ReportPortal.Shared.Configuration;
using System;
using System.Net;
using System.Net.Http;

namespace ReportPortal.Shared.Reporter.Http
{
    public class HttpClientHandlerFactory
    {
        public HttpClientHandlerFactory(IConfiguration configuration)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            Configuration = configuration;
        }

        protected IConfiguration Configuration { get; }

        public virtual HttpClientHandler Create()
        {
            var httpClientHandler = new HttpClientHandler();

            httpClientHandler.Proxy = GetProxy();

#if NETSTANDARD2_0
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
#else
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#endif

            return httpClientHandler;
        }

        public virtual IWebProxy GetProxy()
        {
            WebProxy webProxy = null;

            var proxyUrl = Configuration.GetValue<string>("Server:Proxy:Url", null);

            if (proxyUrl != null)
            {
                webProxy = new WebProxy(proxyUrl);

                var username = Configuration.GetValue<string>("Server:Proxy:Username", null);

                if (username != null)
                {
                    var password = Configuration.GetValue<string>("Server:Proxy:Password", null);

                    var domain = Configuration.GetValue<string>("Server:Proxy:Domain", null);

                    var credential = new NetworkCredential(username, password, domain);

                    webProxy.Credentials = credential;
                }
            }

            return webProxy;
        }
    }
}
