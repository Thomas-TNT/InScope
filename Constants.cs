namespace InScope;

/// <summary>
/// Central constants to avoid magic strings across the codebase.
/// </summary>
public static class Constants
{
    /// <summary>Procedure type: Electrical.</summary>
    public const string ProcedureTypeElectrical = "Electrical";

    /// <summary>Procedure type: Hydraulic.</summary>
    public const string ProcedureTypeHydraulic = "Hydraulic";

    /// <summary>Procedure type: Mechanical.</summary>
    public const string ProcedureTypeMechanical = "Mechanical";

    /// <summary>Procedure type: Other (fallback for blocks with unknown section).</summary>
    public const string ProcedureTypeOther = "Other";

    /// <summary>Default procedure types when config has none.</summary>
    public static readonly string[] DefaultProcedureTypes = { ProcedureTypeElectrical, ProcedureTypeHydraulic, ProcedureTypeMechanical, ProcedureTypeOther };

    /// <summary>Question type: boolean (Yes/No).</summary>
    public const string QuestionTypeBoolean = "boolean";

    /// <summary>Block change log action: Modified.</summary>
    public const string BlockChangeActionModified = "Modified";

    /// <summary>Block change log action: Created.</summary>
    public const string BlockChangeActionCreated = "Created";

    /// <summary>Config file name.</summary>
    public const string ConfigFileName = "config.json";

    /// <summary>Blocks subfolder name.</summary>
    public const string BlocksFolder = "Blocks";

    /// <summary>BlockMetadata subfolder name.</summary>
    public const string BlockMetadataFolder = "BlockMetadata";

    /// <summary>RTF file extension.</summary>
    public const string RtfExtension = ".rtf";
}
