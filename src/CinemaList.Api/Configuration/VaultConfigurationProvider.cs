using System;
using System.Collections.Generic;
using System.Linq;
using CinemaList.Api.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Vault;
using Vault.Client;
using Vault.Model;

namespace CinemaList.Api.Configuration;

/// <summary>
/// Configuration source that loads secrets from HashiCorp Vault.
/// </summary>
public class VaultConfigurationSource : IConfigurationSource
{
    public VaultSettings VaultSettings { get; set; } = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new VaultConfigurationProvider(VaultSettings);
    }
}

/// <summary>
/// Configuration provider that retrieves secrets from HashiCorp Vault and makes them available to the configuration system.
/// </summary>
public class VaultConfigurationProvider(VaultSettings vaultSettings) : ConfigurationProvider
{
    private readonly VaultSettings _vaultSettings = vaultSettings;

    public override void Load()
    {
        try
        {
            VaultConfiguration config = new(_vaultSettings.Address);
            VaultClient vaultClient = new(config);
            vaultClient.SetToken(_vaultSettings.Token);

            if (string.IsNullOrEmpty(_vaultSettings.SecretPath))
            {
                LoadAllSecretsFromMount(vaultClient);
            }
            else
            {
                LoadSecretFromPath(vaultClient, _vaultSettings.SecretPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load secrets from Vault: {ex.Message}");
        }
    }

    private void LoadSecretFromPath(VaultClient vaultClient, string secretPath)
    {
        VaultResponse<KvV2ReadResponse> response = vaultClient.Secrets.KvV2Read(
            path: secretPath,
            kvV2MountPath: _vaultSettings.MountPath
        );

        if (response?.Data?.Data == null) return;
        
        object? data = response.Data.Data;
            
        if (data is JObject jObject)
        {
            foreach (JProperty property in jObject.Properties())
            {
                // Add directly to configuration using the key from Vault
                // Keys should be in format like "MongoDbSettings:ConnectionString"
                Data[property.Name] = property.Value.ToString();
            }
        }
        else if (data is IDictionary<string, object> dictionary)
        {
            foreach (KeyValuePair<string, object> kvp in dictionary)
            {
                Data[kvp.Key] = kvp.Value.ToString() ?? string.Empty;
            }
        }
    }

    private void LoadAllSecretsFromMount(VaultClient vaultClient)
    {
        VaultResponse<StandardListResponse> listResponse = vaultClient.Secrets.KvV2List(
            path: string.Empty,
            kvV2MountPath: _vaultSettings.MountPath
        );

        if (listResponse?.Data?.Keys == null) return;

        foreach (string secretPath in listResponse.Data.Keys.Where(secretPath => !secretPath.EndsWith('/')))
        {
            try
            {
                LoadSecretFromPath(vaultClient, secretPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load secret from path '{secretPath}': {ex.Message}");
            }
        }
    }
}
