using System;
using System.Reflection;
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

    /// <summary>
    /// Gets a configuration sub-section with the specified key and binds it to the specified type.
    /// Throws an exception if the section doesn't exist or is empty.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to</typeparam>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>The bound configuration object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the section is missing or binding fails</exception>
    private static T GetRequiredSection<T>(this IConfiguration configuration, string sectionName) 
        where T : class
    {
        if (string.IsNullOrEmpty(sectionName))
            throw new ArgumentNullException(nameof(sectionName));

        IConfigurationSection section = configuration.GetSection(sectionName);
            
        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{sectionName}' is missing");

        T? result = section.Get<T>();
            
        if (result == null)
            throw new InvalidOperationException($"Unable to bind configuration section '{sectionName}' to type '{typeof(T).Name}'");

        return result;
    }

    /// <summary>
    /// Gets a configuration sub-section using the section name from the type's SectionName constant/property.
    /// Throws an exception if the section doesn't exist or is empty.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to (must have a SectionName constant/property)</typeparam>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The bound configuration object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the section is missing, SectionName is not found, or binding fails</exception>
    public static T GetRequiredSection<T>(this IConfiguration configuration) 
        where T : class
    {
        string sectionName = typeof(T).Name;
        return configuration.GetRequiredSection<T>(sectionName);
    }
}

