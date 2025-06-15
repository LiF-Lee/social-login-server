namespace social_login_server.Models.GraphQL;

public record AuthPayload(string AccessToken, string RefreshToken, User? User);
