using System.Text.Json;
using emc_api.Middleware;

public class AuthService:IAuthService
{
    private readonly string _authFilePath;
    private readonly string _encryptionKey;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
    {
        _encryptionKey = configuration["EncryptionKey"];
        _authFilePath = Path.Combine(configuration["data:dir"], "auth.json");
        _logger = logger;
    }

    public async Task<List<User>> GetUsersAsync()
    {
        try
        {
            // Read and validate file exists
            if (!File.Exists(_authFilePath))
            {
                _logger.LogError($"Auth file not found at: {_authFilePath}");
                return new List<User>();
            }

            string encryptedJson = await File.ReadAllTextAsync(_authFilePath);
            
            // Validate content is not empty
            if (string.IsNullOrEmpty(encryptedJson))
            {
                _logger.LogError("Auth file is empty");
                return new List<User>();
            }

            // Remove any BOM or whitespace
            encryptedJson = encryptedJson.Trim();

          
            // Decrypt content
            string decryptedJson = AesEncryption.Decrypt(encryptedJson, _encryptionKey);
            if (string.IsNullOrEmpty(decryptedJson))
            {
                _logger.LogError("Decryption failed or produced empty result");
                return new List<User>();
            }
            Console.WriteLine(decryptedJson);
            // Parse JSON
            var users = JsonSerializer.Deserialize<List<User>>(decryptedJson);
            return users ?? new List<User>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auth file");
            return new List<User>();
        }
    }

    public async Task<User> ValidateUserAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Attempted login with empty username or password");
            return null;
        }

        var users = await GetUsersAsync();
        return users.FirstOrDefault(u => 
            u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase) && 
            u.Password == password);
    }
    public async Task<bool> ChangePasswordAsync(string username,string newPassword)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(newPassword))
        {
            _logger.LogWarning("Attempted change password with empty username or password");
            return false;
        }

        var users = await GetUsersAsync();
        var user = users.FirstOrDefault(u => u.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            _logger.LogWarning($"User not found: {username}");
            return false;
        }
        user.Password = newPassword;
        return await SaveUsersAsync(users);
    }
    private async Task<bool> SaveUsersAsync(IEnumerable<User> users)
    {
        try
        {
            // Serialize to JSON
            string json = JsonSerializer.Serialize(users);
            // Encrypt
            string encryptedJson = AesEncryption.EncryptOpenSSL(json, _encryptionKey);
            // Write to file
            await File.WriteAllTextAsync(_authFilePath, encryptedJson);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving users");
            return false;
        }
    }
}