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

    public async Task<List<ControlledUser>> GetUsersAsync()
    {
        try
        {
            // Read and validate file exists
            if (!File.Exists(_authFilePath))
            {
                _logger.LogError($"Auth file not found at: {_authFilePath}");
                return new List<ControlledUser>();
            }

            string encryptedJson = await File.ReadAllTextAsync(_authFilePath);
            
            // Validate content is not empty
            if (string.IsNullOrEmpty(encryptedJson))
            {
                _logger.LogError("Auth file is empty");
                return new List<ControlledUser>();
            }

            // Remove any BOM or whitespace
            encryptedJson = encryptedJson.Trim();

          
            // Decrypt content
            string decryptedJson = AesEncryption.Decrypt(encryptedJson, _encryptionKey);
            if (string.IsNullOrEmpty(decryptedJson))
            {
                _logger.LogError("Decryption failed or produced empty result");
                return new List<ControlledUser>();
            }
            Console.WriteLine(decryptedJson);
            // Parse JSON
            var users = JsonSerializer.Deserialize<List<ControlledUser>>(decryptedJson);
            return users ?? new List<ControlledUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auth file");
            return new List<ControlledUser>();
        }
    }

    
    
}
public record ControlledUser(string username,string machinename,string role,string team,string englishname);