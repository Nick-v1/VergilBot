namespace MyProject.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public decimal Balance { get; set; }
    public string DiscordId { get; set; }
    public bool HasSubscription { get; set; }
    public int? GenerationTokens { get; set; }
}
