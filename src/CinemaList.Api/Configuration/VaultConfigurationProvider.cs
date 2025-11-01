using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CinemaList.Api.Settings;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
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
    
    private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex.Message.Contains("rate-limited", StringComparison.OrdinalIgnoreCase)),
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = args =>
            {
                Console.WriteLine($"Rate limited while loading Vault secret. Retrying in {args.RetryDelay.TotalMilliseconds}ms (attempt {args.AttemptNumber + 1})");
                return default;
            }
        })
        .Build();

    public override void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        try
        {
            VaultConfiguration config = new(_vaultSettings.Address);
            VaultClient vaultClient = new(config);
            vaultClient.SetToken(_vaultSettings.Token);

            if (string.IsNullOrEmpty(_vaultSettings.SecretPath))
            {
                await LoadAllSecretsFromMountAsync(vaultClient);
            }
            else
            {
                await LoadSecretFromPathAsync(vaultClient, _vaultSettings.SecretPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load secrets from Vault: {ex.Message}");
        }
    }

    private async Task LoadSecretFromPathAsync(VaultClient vaultClient, string secretPath)
    {
        await RetryPipeline.ExecuteAsync(async cancellationToken =>
        {
            VaultResponse<KvV2ReadResponse> response = await vaultClient.Secrets.KvV2ReadAsync(
                path: secretPath,
                kvV2MountPath: _vaultSettings.MountPath, cancellationToken: cancellationToken);

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
        });
    }

    private async Task LoadAllSecretsFromMountAsync(VaultClient vaultClient)
    {
        VaultResponse<StandardListResponse> listResponse = await vaultClient.Secrets.KvV2ListAsync(
            path: string.Empty,
            kvV2MountPath: _vaultSettings.MountPath
        );

        if (listResponse?.Data?.Keys == null) return;

        // Process secrets sequentially to avoid overwhelming Vault with concurrent requests
        foreach (string secretPath in listResponse.Data.Keys.Where(secretPath => !secretPath.EndsWith('/')))
        {
            try
            {
                await LoadSecretFromPathAsync(vaultClient, secretPath);
                // Small delay between requests to avoid rate limiting
                await Task.Delay(50);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load secret from path '{secretPath}': {ex.Message}");
            }
        }
    }
}
