namespace social_login_server.Models;

public class App
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string ClientId { get; set; } = null!;
    
    public string ClientSecret { get; set; } = null!;
    
    public string RedirectUri { get; set; } = null!;
    
    public int OwnerUserId { get; set; }
    
    public User OwnerUser { get; set; } = null!;
    
    public DateTime Created { get; set; } = DateTime.UtcNow;
}