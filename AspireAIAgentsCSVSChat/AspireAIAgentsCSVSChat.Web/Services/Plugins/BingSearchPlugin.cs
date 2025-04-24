using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Plugins.Web.Bing;
using System.ComponentModel;
using System.Text;
namespace AspireAIAgentsCSVSChat.Web.Services.Plugins
{
    public class BingSearchPlugin
    {

        private readonly BingTextSearch _bingTextSearch;

        public BingSearchPlugin(BingTextSearch bingTextSearch)
        {
            _bingTextSearch = bingTextSearch;
        }

        [KernelFunction("gamp_text_search")]
        [Description("Text Search the web using Bing")]
        [return: Description("The text search results as a string. Each result is separated by a new line.")]
        public async Task<string> SearchAsync(string query)
        {
            //TODO add site filter
            var sb = new StringBuilder();
            KernelSearchResults<string> response = await _bingTextSearch.SearchAsync(query);
            await foreach (var result in response.Results)
            {
                sb.AppendLine(result);
            }

            return sb.ToString();
        }
        [KernelFunction("gamp_text_search_detailed")]
        [Description("Text Search the web using Bing with details")]
        public async Task<List<TextSearchResult>> TextSearchAsync(string query, string site)
        {
            //TODO add site filter
            List<TextSearchResult> results = new();
            KernelSearchResults<TextSearchResult> response = await _bingTextSearch.GetTextSearchResultsAsync(query);
            await foreach (var result in response.Results)
            {
                results.Add(result);
            }
            return results;
        }
        [KernelFunction("gamp_web_page_search")]
        [Description("Web Page Search the web using Bing")]
        [return: Description("The web page search results as a BingWebPage object. Each result is separated by a new line.")]
        public async Task<List<BingWebPage>> BingWebPageSearchAsync(string query)
        {
            //TODO add site filter
            List<BingWebPage> results = new();
            KernelSearchResults<object> response = await _bingTextSearch.GetSearchResultsAsync(query);
            await foreach (BingWebPage result in response.Results)
            {
                results.Add(result);
            }
            return results;
        }



    }
}
