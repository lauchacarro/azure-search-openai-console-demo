namespace AzureOpenAIDemo.Models;


public class FieldMapping
{
    public List<object> ContentField { get; set; }
    public object TitleField { get; set; }
    public object UrlField { get; set; }
    public object FilepathField { get; set; }
}

public class Parameters
{
    public string Endpoint { get; set; }
    public string Key { get; set; }
    public string IndexName { get; set; }
    public FieldMapping FieldsMapping { get; set; }
    public bool InScope { get; set; }
    public int TopNDocuments { get; set; }
    public string QueryType { get; set; }
    public string SemanticConfiguration { get; set; }
    public string RoleInformation { get; set; }
}

public class DataSource
{
    public string Type { get; set; }
    public Parameters Parameters { get; set; }
}

public class Message
{
    public string Role { get; set; }
    public string Content { get; set; }
}

public class RequestBody
{
    public IEnumerable<DataSource> DataSources { get; set; }
    public IEnumerable<Message> Messages { get; set; }
}

public class Choice
{
    public IEnumerable<Message> Messages { get; set; }
}

public class ResponseBody
{
    public List<Choice> Choices { get; set; }
}
