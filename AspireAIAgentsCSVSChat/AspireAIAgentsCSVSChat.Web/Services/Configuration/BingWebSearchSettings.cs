using Microsoft.SemanticKernel.Plugins.Web.Bing;

namespace AspireAIAgentsCSVSChat.Web.Services.Configuration
{
    public class BingWebSearchSettings
    {
        public static BingTextSearch CreateBingTextSearch(IConfiguration configuration)
        {

            var bingApiKey = configuration["BingApiKey"];
            if (string.IsNullOrWhiteSpace(bingApiKey))
            {
                throw new ArgumentException("BingApiKey is not set in appsettings.json or environment variables.");
            }

            return new BingTextSearch(bingApiKey);
        }
    }
}
