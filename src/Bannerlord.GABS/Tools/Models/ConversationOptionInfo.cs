namespace Bannerlord.GABS.Tools.Models;

/// <summary>
/// Conversation dialog option.
/// </summary>
public class ConversationOptionInfo
{
    /// <summary>Option index in the list</summary>
    public int Index { get; set; }
    /// <summary>Option identifier</summary>
    public string? Id { get; set; }
    /// <summary>Option display text</summary>
    public string? Text { get; set; }
    /// <summary>Whether the option can be selected</summary>
    public bool IsClickable { get; set; }
    /// <summary>Whether this option triggers a persuasion check</summary>
    public bool HasPersuasion { get; set; }
    /// <summary>Skill used for persuasion check</summary>
    public string? SkillName { get; set; }
    /// <summary>Trait used for persuasion check</summary>
    public string? TraitName { get; set; }
    /// <summary>Hint text shown on hover</summary>
    public string? Hint { get; set; }
    /// <summary>Persuasion success chance percentage</summary>
    public double? SuccessChance { get; set; }
    /// <summary>Critical success chance percentage</summary>
    public double? CritSuccessChance { get; set; }
    /// <summary>Critical failure chance percentage</summary>
    public double? CritFailChance { get; set; }
    /// <summary>Failure chance percentage</summary>
    public double? FailChance { get; set; }
}