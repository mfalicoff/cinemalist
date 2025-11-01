using CinemaList.Api.Configuration;
using CinemaList.Api.Settings;
using Microsoft.Extensions.Configuration;

namespace CinemaList.Api.Extensions;

/// <summary>
/// Extension methods for adding HashiCorp Vault configuration.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds HashiCorp Vault as a configuration source.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="configuration">The existing configuration to read Vault settings from.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddVault(this IConfigurationBuilder builder, IConfiguration configuration)
    {
        VaultSettings vaultSettings = new();
        configuration.GetSection(nameof(VaultSettings)).Bind(vaultSettings);

        if (!string.IsNullOrEmpty(vaultSettings.Address) && !string.IsNullOrEmpty(vaultSettings.Token))
        {
            builder.Add(new VaultConfigurationSource
            {
                VaultSettings = vaultSettings
            });
        }

        return builder;
    }
    
    public static IConfigurationBuilder ConfigureConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        // Add Override configuration source (loads before Vault so Vault can override)
        configurationBuilder.AddJsonFile("appsettings.override.json", optional: true, reloadOnChange: true);

        // Add Vault configuration source (loads secrets into configuration system)
        configurationBuilder.AddVault(configurationBuilder.Build());

        // Add appsettings.override.json as the final configuration source
        configurationBuilder.AddJsonFile("appsettings.override.json", optional: true, reloadOnChange: true);

        return configurationBuilder;
    }
}

