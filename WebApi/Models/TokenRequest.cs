namespace WebApi.Models;

public class TokenRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}
