using Microsoft.Extensions.Caching.Memory;
using Remora.Rest.Core;

namespace Silk.Shared.Types;

/// <summary>
///     <para>A helper class for generating keys to store in an <see cref="IMemoryCache" /></para>
///     <para>These keys are formatted in a way that should be compatible with Redis, too.</para>
/// </summary>
public static class SilkKeyHelper
{
    /// <summary>
    ///     Generates a key for a regular guild config.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>The generated key.</returns>
    public static string GenerateGuildKey(Snowflake guildId)
        => $"guild_config:{guildId}";

    /// <summary>
    ///     Generates a key for a moderation guild config.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>The generated key.</returns>
    public static string GenerateGuildModKey(Snowflake guildId)
        => $"guild_mod_config:{guildId}";

    /// <summary>
    ///     Generates a key for a guild prefix.
    /// </summary>
    /// <param name="guildId">The ID of the guild.</param>
    /// <returns>The generated key.</returns>
    public static string GenerateGuildPrefixKey(Snowflake guildId)
        => $"guild_prefix:{guildId}";
    
    public static string GenerateGuildMemberCountKey(Snowflake guildId)
        => $"guild_member_count:{guildId}";
    
    public static string GenerateGuildCountKey()
        => "guild_count";
    
    public static string GenerateCurrentGuildCounterKey()
        => "current_guild_count";
    
    public static string GenerateGuildIdentifierKey(Snowflake guildId)
        => $"guild_identifier:{guildId}";
    

}