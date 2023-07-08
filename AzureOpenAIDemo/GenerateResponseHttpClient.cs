using Azure.AI.OpenAI;

using AzureOpenAIDemo.Models;

using System.Text;
using System.Text.Json;

namespace AzureOpenAIDemo
{
    internal class GenerateResponseHttpClient
    {
        public async Task<string> GenerateResponseAsync(string userMessage, List<ChatMessage> historyMessages)
        {
            var messages = historyMessages.Where(x => x.Role != ChatRole.System)
                                          .Union(new[] { new ChatMessage(ChatRole.User, userMessage) });

            string url = $"{Configuration.OpenAIEndpoint}openai/deployments/{Configuration.OpenAIImplementationName}/extensions/chat/completions?api-version=2023-06-01-preview";

            string chatGptUrl = $"{Configuration.OpenAIEndpoint}openai/deployments/{Configuration.OpenAIImplementationName}/chat/completions?api-version=2023-03-15-preview";

            HttpClient client = new HttpClient();

            // Configurar los encabezados
            client.DefaultRequestHeaders.Add("api-key", Configuration.OpenAISecret);
            client.DefaultRequestHeaders.Add("chatgpt_url", chatGptUrl);
            client.DefaultRequestHeaders.Add("chatgpt_key", Configuration.OpenAISecret);

            // Construir el cuerpo de la solicitud

            RequestBody requestBody = new RequestBody()
            {
                DataSources = new List<DataSource>
                {
                    new DataSource
                    {
                        Type = "AzureCognitiveSearch",
                        Parameters = new Parameters
                        {
                            Endpoint = Configuration.SearchEndpoint,
                            Key = Configuration.SearchSecret,
                            IndexName = Configuration.SearchIndex,
                            InScope = true,
                            TopNDocuments = 5,
                            SemanticConfiguration = "",
                            QueryType = "simple",
                            RoleInformation = Prompts.SystemMessage,

                        }
                    }
                },
                Messages = messages.Select(x => new Message
                {
                    Role = x.Role.Label,
                    Content = x.Content
                })
            };



            var jsonBody = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });


            // Enviar la solicitud POST
            HttpResponseMessage response = await client.PostAsync(url, new StringContent(jsonBody, Encoding.UTF8, "application/json"));

            // Leer la respuesta

            string responseJson = await response.Content.ReadAsStringAsync();


            ResponseBody? responseBody = JsonSerializer.Deserialize<ResponseBody>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });


            // Manejar la respuesta aquí

            return responseBody.Choices.First().Messages.First(x => x.Role == ChatRole.Assistant).Content;
        }
    }
}
