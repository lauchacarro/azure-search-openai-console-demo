
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

using System.Text;
using System.Text.Json;

// Configurations

var openaiUrl = "";
var openaiSecret = "";
var openaiImplementationName = "";

var azureSearchServiceEndpoint = "";
var azureSearchIndex = "";
var azureSearchSecret = "";

//


string QueryPromptTemplate = """
            <|im_start|>system
            Chat history:
            {{$chat_history}}
            
            Here's a few examples of good search queries:
            ### Good example 1 ###
            Precios y metodos de Pago
            ### Good example 2 ###
            Menu
            ### Good example 3 ###
            Servicio de Wifi
            ###


            <|im_end|>
            <|im_start|>system
            Generate search query for followup question. You can refer to chat history for context information. Just return search query and don't include any other information.
            {{$question}}
            <|im_end|>
            <|im_start|>assistant
            """;

string AnswerPromptTemplate = """
        <|im_start|>system
        You are a system assistant who helps the company employees with their healthcare plan questions, and questions about the employee handbook. Be brief in your answers.
        Answer ONLY with the facts listed in the list of sources below. If there isn't enough information below, say you don't know. Do not generate answers that don't use the sources below.


        For tabular information return it as an html table. Do not return markdown format.
        Each source has a name followed by colon and the actual information, ALWAYS reference source for each fact you use in the response. Use square brakets to reference the source. List each source separately.
     


        Here're a few examples:
        ### Good Example 1 (include source) ###
        Apple is a fruit[reference1.pdf].
        ### Good Example 2 (include multiple source) ###
        Apple is a fruit[reference1.pdf][reference2.pdf].
        ### Good Example 2 (include source and use double angle brackets to reference question) ###
        Microsoft is a software company[reference1.pdf].  <<followup question 1>> <<followup question 2>> <<followup question 3>>
        ### END ###
        Sources:
        {{$sources}}

        Chat history:
        {{$chat_history}}
        <|im_end|>
        <|im_start|>user
        {{$question}}
        <|im_end|>
        <|im_start|>assistant
        """;


string SystemMessage = @"Sos un asistente virtual del Hotel Valle del Volcán. Tu objetivo es responder todas las dudas de los clientes para lograr que realicen una reserva. Las respuestas a los clientes deben ser amables e incluir emojis apropiados. Tus respuestas deben estar en el mismo idioma que la pregunta del usuario.";



OpenAIClient client = new(
    new Uri(openaiUrl),
    new AzureKeyCredential(openaiSecret));


SearchClient searchClient = new(
                new Uri(azureSearchServiceEndpoint), azureSearchIndex, new AzureKeyCredential(azureSearchSecret));

List<ChatMessage> historyMessages = new List<ChatMessage>()
{
    new ChatMessage(ChatRole.System, SystemMessage),
};

Console.WriteLine("Hola! Soy tu asistente virtual, ¿En que puedo ayudarte?");
Console.WriteLine();

while (true)
{
    var userMessage = Console.ReadLine();

    #region Generate Search Query

    var prompt = QueryPromptTemplate
        .Replace("{{$chat_history}}", JsonSerializer.Serialize(historyMessages))
        .Replace("{{$question}}", userMessage);

    Response<Completions> completionsResponse = await client.GetCompletionsAsync(
        deploymentOrModelName: openaiImplementationName,
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

    var searchResultResponse = await searchClient.SearchAsync<SearchDocument>(searchQuery, searchOptions, default);

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

    prompt = AnswerPromptTemplate
        .Replace("{{$chat_history}}", JsonSerializer.Serialize(historyMessages))
        .Replace("{{$question}}", userMessage)
        .Replace("{{$sources}}", documentContents);


    var completionsAnswerResponse = await client.GetCompletionsAsync(
        deploymentOrModelName: openaiImplementationName,
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

    historyMessages.AddRange(new[] {
        new ChatMessage(ChatRole.User, userMessage),
        new ChatMessage(ChatRole.Assistant, response)
    });



    Console.WriteLine(response);
    Console.WriteLine();
}



