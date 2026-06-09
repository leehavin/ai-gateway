namespace DataChat.Gateway.Models;

public sealed class ChatParametersDto
{
    public double? Temperature { get; set; }
    public double? TopP { get; set; }
    public int? MaxTokens { get; set; }
}
