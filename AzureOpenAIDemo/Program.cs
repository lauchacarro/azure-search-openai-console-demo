using Azure.AI.OpenAI;

using AzureOpenAIDemo;


Configuration.OpenAIEndpoint = ""; //https://xxxxx.openai.azure.com/
Configuration.OpenAISecret = "";
Configuration.OpenAIImplementationName = "";

Configuration.SearchEndpoint = ""; //https://xxxxx.search.windows.net/
Configuration.SearchIndex = "";
Configuration.SearchSecret = "";






List<ChatMessage> historyMessages = new List<ChatMessage>()
{
    new ChatMessage(ChatRole.System, Prompts.SystemMessage),
};

Console.WriteLine("Hola! Soy tu asistente virtual, ¿En que puedo ayudarte?");
Console.WriteLine();

while (true)
{
    var userMessage = Console.ReadLine();


    GenerateResponseHttpClient service = new GenerateResponseHttpClient();


    //GenerateResponseRAG service = new GenerateResponseRAG();


    var response = await service.GenerateResponseAsync(userMessage, historyMessages);


    historyMessages.AddRange(new[] { 
        new ChatMessage(ChatRole.User, userMessage),
        new ChatMessage(ChatRole.Assistant, response) 
    });



    Console.WriteLine(response);
    Console.WriteLine();
}



