# HashiCorp Vault Integration

This application supports loading secrets from HashiCorp Vault automatically through the configuration system.

## How It Works

The Vault configuration provider:
1. Loads secrets from Vault during application startup
2. Adds them directly to the `IConfiguration` system using the secret key names from Vault
3. Uses standard `IOptions<T>` binding via the `BindFromConfiguration` extension

**Important**: Secret keys in Vault must use the same format as configuration keys (e.g., `MongoDbSettings:ConnectionString`).

The provider supports two modes:
1. **Fetch All Secrets** (Recommended): Load all secrets from the mount path automatically when `SecretPath` is empty
2. **Specific Secret Path**: Load secrets from a specific path only

## Configuration

### Fetch All Secrets Mode (Recommended)

This mode loads all secrets from the mount path automatically.

```json
{
  "VaultSettings": {
    "Address": "http://your-vault-server:8200",
    "Token": "hvs.YOUR_VAULT_TOKEN",
    "MountPath": "kv"
  }
}
```

With this configuration, all secrets at `kv/*` will be loaded.

### 2. Store Secrets in Vault

Store your secrets in Vault using configuration key format. **Secret keys must match configuration paths** (use `:` for nested keys):

```bash
# Store MongoDB settings - keys use configuration format
vault kv put kv/mongodb \
  MongoDbSettings:ConnectionString="mongodb://user:pass@mongo:27017" \
  MongoDbSettings:DatabaseName="CinemaListDb"

# Store other secrets
vault kv put kv/omdb \
  OMDbSettings:ApiKey="your-omdb-api-key"
```

### 3. Use Secrets via IOptions

Secrets are automatically bound to your options classes:

```csharp
// In your service
public class MyService
{
    private readonly MongoDbSettings _settings;
    
    public MyService(IOptions<MongoDbSettings> settings)
    {
        _settings = settings.Value;
        // _settings.ConnectionString will contain the value from Vault
    }
}
```

## Example: MongoDB with Vault (Fetch All Mode)

This is the simplest approach - all secrets are automatically loaded.

1. **Store MongoDB credentials in Vault with proper key names:**
   ```bash
   vault kv put kv/mongodb \
     MongoDbSettings:ConnectionString="mongodb://admin:secret@localhost:27017" \
     MongoDbSettings:DatabaseName="CinemaListDb"
   ```

2. **Configure Vault in appsettings.override.json:**
   ```json
   {
     "VaultSettings": {
       "Address": "http://192.168.2.41:8200",
       "Token": "hvs.YOUR_TOKEN",
       "MountPath": "kv"
     }
   }
   ```

3. **Bind settings in your code:**
   ```csharp
   // In ServiceCollectionExtensions or Program.cs
   services.BindFromConfiguration<MongoDbSettings>(configuration, "MongoDbSettings");
   ```

4. **The IOptions<MongoDbSettings> will automatically have the Vault values!**

## Example: Specific Secret Path

If you prefer to organize secrets in a specific path:

1. **Store secrets in a specific path:**
   ```bash
   vault kv put kv/cinemalist/secrets \
     MongoDbSettings:ConnectionString="mongodb://admin:secret@localhost:27017" \
     MongoDbSettings:DatabaseName="CinemaListDb" \
     OMDbSettings:ApiKey="your-key"
   ```

2. **Configure Vault with SecretPath:**
   ```json
   {
     "VaultSettings": {
       "Address": "http://192.168.2.41:8200",
       "Token": "hvs.YOUR_TOKEN",
       "MountPath": "kv",
       "SecretPath": "cinemalist/secrets"
     }
   }
   ```


## Security Notes

- Never commit Vault tokens to source control
- Use `appsettings.override.json` (which is gitignored) for local Vault configuration
- In production, use Vault authentication methods like AppRole or Kubernetes auth instead of static tokens
- Consider using Vault's dynamic secrets for database credentials

