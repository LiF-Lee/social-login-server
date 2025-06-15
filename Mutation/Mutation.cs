namespace social_login_server.Mutation;

public class Mutation(AccountMutation account, AppMutation app)
{
    public AccountMutation Account { get; } = account;
    
    public AppMutation App { get; } = app;
}