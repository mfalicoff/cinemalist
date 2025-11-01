# cinemalist

## Local Development Configuration

For local development, you can create an `appsettings.override.json` file to override any configuration settings without affecting the repository.

### Setup
1. Create a file named `appsettings.override.json` in the root directory of the project.
2. Edit `appsettings.override.json` with your local settings:
   - Database connection strings
   - API keys
   - Other environment-specific settings

This file is automatically loaded last and will override all previous configuration sources. It's already included in `.gitignore` so your local settings won't be committed to the repository.

## HashiCorp Vault Integration

This application supports loading secrets from HashiCorp Vault automatically. Secrets are loaded during application startup and made available through the standard `IConfiguration` and `IOptions<T>` patterns.

### Quick Start with Vault

1. **Configure Vault connection in `appsettings.override.json`:**
   ```json
   {
     "VaultSettings": {
       "Address": "http://your-vault-server:8200",
       "Token": "hvs.YOUR_VAULT_TOKEN",
       "MountPath": "kv"
     }
   }
   ```
   
   Note: Omit `SecretPath` to fetch all secrets from the mount path automatically.

2. **Store secrets in Vault using configuration key format:**
   ```bash
   vault kv put kv/mongodb \
     MongoDbSettings:ConnectionString="mongodb://user:pass@mongo:27017" \
     MongoDbSettings:DatabaseName="CinemaListDb"
   ```
   
   **Important**: Secret keys must use configuration format (e.g., `MongoDbSettings:ConnectionString`).

3. Secrets will be automatically loaded and bound to your `IOptions<T>` classes.

For detailed Vault setup and usage, see [VAULT.md](VAULT.md).

