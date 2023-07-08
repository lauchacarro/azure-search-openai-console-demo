using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

using System.Text;
using System.Text.Json;

namespace AzureOpenAIDemo
{

    //Retrieval-Augmented Generation

    internal class GenerateResponseRAG
    {
        private readonly OpenAIClient _openAiClient;
        private readonly SearchClient _searchClient;

        public GenerateResponseRAG()
        {

            OpenAIClient openAiClient = new(
                new Uri(Configuration.OpenAIEndpoint),
                new AzureKeyCredential(Configuration.OpenAISecret));


            SearchClient searchClient = new(
                            new Uri(Configuration.SearchEndpoint),
                            Configuration.SearchIndex,
                            new AzureKeyCredential(Configuration.SearchSecret));

            _openAiClient = openAiClient;
            _searchClient = searchClient;
        }

        public async Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> historyMessages)
        {

            #region Generate Search Query

            var prompt = Prompts.QueryPromptTemplate
                .Replace("{{$chat_history}}", JsonSerializer.Serialize(historyMessages))
                .Replace("{{$question}}", userMessage);

            Response<Completions> completionsResponse = await _openAiClient.GetCompletionsAsync(
                deploymentOrModelName: Configuration.OpenAIImplementationName,
                new CompletionsOptions()
                {
                    Prompts = { prompt },
                    Temperature = (float)0,
                    MaxTokens = 100,
                    NucleusSamplingFactor = (float)0.5,
                    FrequencyPenalty = (float)0,
                    PresencePenalty = (float)0,
                    StopSequences = { "<|im_end|>" }
                });


            Completions completions = completionsResponse.Value;

            var searchQuery = completions.Choices[0].Text;


            #endregion


            #region Search Documents

            SearchOptions searchOptions = new SearchOptions
            {
                Filter = "",
                QueryType = SearchQueryType.Simple,
                QueryLanguage = "es-es",
                QuerySpeller = "lexicon",
                SemanticConfigurationName = "default",
                Size = 5,
            };

            var searchResultResponse = await _searchClient.SearchAsync<SearchDocument>(searchQuery, searchOptions, default);

            SearchResults<SearchDocument> searchResult = searchResultResponse.Value;

            #region Formatter Document Contents

            var sb = new StringBuilder();
            foreach (var doc in searchResult.GetResults())
            {
                doc.Document.TryGetValue("filepath", out var filepathValue);
                doc.Document.TryGetValue("chunk_id", out var chunkIdValue);
                string? contentValue;
                try
                {

                    doc.Document.TryGetValue("content", out var value);
                    contentValue = (string)value;

                }
                catch (ArgumentNullException)
                {
                    contentValue = null;
                }

                if (filepathValue is string filepath && chunkIdValue is string chunkId && contentValue is string content)
                {
                    content = content.Replace('\r', ' ').Replace('\n', ' ');
                    sb.AppendLine($"{filepath}-{chunkId}:{content}");
                }
            }

            var documentContents = sb.ToString();
            #endregion


            #endregion


            #region Generate Response

            prompt = Prompts.AnswerPromptTemplate
                .Replace("{{$chat_history}}", JsonSerializer.Serialize(historyMessages))
                .Replace("{{$question}}", userMessage)
                .Replace("{{$sources}}", documentContents);


            var completionsAnswerResponse = await _openAiClient.GetCompletionsAsync(
                deploymentOrModelName: Configuration.OpenAIImplementationName,
                new CompletionsOptions()
                {
                    Prompts = { prompt },
                    Temperature = (float)0,
                    MaxTokens = 100,
                    NucleusSamplingFactor = (float)0.5,
                    FrequencyPenalty = (float)0,
                    PresencePenalty = (float)0,
                    StopSequences = { "<|im_end|>" }
                });


            var completionsAnswer = completionsAnswerResponse.Value;
            var response = completionsAnswer.Choices[0].Text;

            #endregion

            return response;

        }
    }
}
