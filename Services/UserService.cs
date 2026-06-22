using learning_core_api.Models;
using Npgsql;

namespace learning_core_api.Services;

public class UserService(NpgsqlDataSource dataSource)
{
    public async Task<List<User>> GetUsersAsync()
    {
        var users = new List<User>();

        await using var cmd = dataSource.CreateCommand("SELECT id, name, email FROM users");
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            users.Add(new User(reader.GetInt32(0), reader.GetString(1), reader.GetString(2)));
        }

        return users;
    }
}
