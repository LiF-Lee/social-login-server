namespace social_login_server.Models;

public class User
{
    public int Id { get; set; }
    
    public string Name { get; set; } = null!;
    
    public string Email { get; set; } = null!;
    
    [GraphQLIgnore] public string Password { get; set; } = null!;
    
    public List<App> Apps { get; set; } = [];
    
    public DateTime Created { get; set; } = DateTime.UtcNow;
}