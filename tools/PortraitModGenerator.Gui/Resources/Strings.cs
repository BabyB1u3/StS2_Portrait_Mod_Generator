using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace PortraitModGenerator.Gui.Resources;

internal static class Strings
{
    private static readonly ResourceManager Manager = new(
        "PortraitModGenerator.Gui.Resources.Strings",
        typeof(Strings).Assembly);

    public static string MainForm_Title => Get();
    public static string BuildModForm_Title => Get();
    public static string ConflictReviewForm_Title => Get();

    public static string Button_ImportPck => Get();
    public static string Button_OpenConflicts => Get();
    public static string Button_BuildMod => Get();
    public static string Button_UpdateMapping => Get();
    public static string Button_NextPending => Get();
    public static string Button_Browse => Get();

    public static string Filter_All => Get();
    public static string Filter_Included => Get();
    public static string Filter_Conflict => Get();
    public static string Filter_Unmatched => Get();
    public static string Filter_Discarded => Get();
    public static string Filter_Pending => Get();
    public static string Filter_Resolved => Get();

    public static string Label_Group => Get();
    public static string Label_Card => Get();
    public static string Label_SourcePreview => Get();
    public static string Label_OfficialPreview => Get();
    public static string Label_ModId => Get();
    public static string Label_ModName => Get();
    public static string Label_Author => Get();
    public static string Label_Description => Get();
    public static string Label_ArtifactDir => Get();
    public static string Label_NoConflicts => Get();
    public static string Label_NoConflictSelected => Get();

    public static string Placeholder_SearchAssets => Get();

    public static string Checkbox_Discard => Get();
    public static string Checkbox_DiscardGroup => Get();
    public static string Checkbox_Choose => Get();

    public static string GroupBox_BuildSettings => Get();

    public static string Status_Included => Get();
    public static string Status_Discarded => Get();
    public static string Status_Unmatched => Get();
    public static string Status_Conflict => Get();
    public static string Status_Pending => Get();
    public static string Status_Resolved => Get();

    public static string Badge_Included => Get();
    public static string Badge_Discarded => Get();
    public static string Badge_Unmatched => Get();
    public static string Badge_Conflict => Get();

    public static string Help_MappingReview => Get();
    public static string Help_BuildModPrompt => Get();
    public static string Help_NoConflicts => Get();
    public static string Help_ImportToBegin => Get();

    public static string Info_GdreCache => Get();
    public static string Info_MainSummary => Get();
    public static string Info_SessionPath => Get();
    public static string Info_CandidateStatus => Get();
    public static string Info_CandidatePath => Get();
    public static string Info_CandidateReason => Get();
    public static string Info_ConflictSummary => Get();
    public static string Info_GroupDetails => Get();
    public static string Info_CandidatePackage => Get();
    public static string Info_CandidateFile => Get();
    public static string Info_CandidateConfidence => Get();
    public static string Info_ConflictCandidateCount => Get();

    public static string Status_ImportingPcks => Get();
    public static string Status_SkippingAlreadyImported => Get();
    public static string Status_Recovering => Get();
    public static string Status_Scanning => Get();
    public static string Status_AnalyzingMappings => Get();
    public static string Status_Merging => Get();
    public static string Status_ImportedCount => Get();
    public static string Status_ImportFailed => Get();
    public static string Status_PreparingBuild => Get();
    public static string Status_GeneratingSource => Get();
    public static string Status_WritingReview => Get();
    public static string Status_Materializing => Get();
    public static string Status_BuildingFinal => Get();
    public static string Status_BuiltTo => Get();
    public static string Status_BuildFailed => Get();
    public static string Status_BuildBlocked => Get();
    public static string Status_GenerationCancelled => Get();

    public static string Dialog_ImportPckFilePicker_Title => Get();
    public static string Dialog_PckFileFilter => Get();
    public static string Dialog_ImportPck_Title => Get();
    public static string Dialog_ImportPckFailed_Title => Get();
    public static string Dialog_Conflicts_Title => Get();
    public static string Dialog_BuildMod_Title => Get();
    public static string Dialog_BuildModFailed_Title => Get();
    public static string Dialog_ResolveConflictsFirst_Title => Get();
    public static string Dialog_UnmatchedAssets_Title => Get();

    public static string Error_GdreNotFound => Get();
    public static string Error_ImportFirst => Get();
    public static string Error_ImportAndReviewFirst => Get();
    public static string Error_PendingConflicts => Get();
    public static string Warn_UnmatchedAssets => Get();
    public static string Error_ModIdRequired => Get();
    public static string Error_ArtifactDirRequired => Get();
    public static string Info_BuildSuccess => Get();
    public static string Info_BuildFailedWithLog => Get();

    public static string Text_Unknown => Get();
    public static string Text_None => Get();

    public static string Reason_DiscardedManual => Get();
    public static string Reason_ManuallyAssigned => Get();
    public static string Reason_DiscardedInConflict => Get();

    public static string Default_UnknownAuthor => Get();
    public static string Default_ModDescription => Get();

    public static string Menu_File => Get();
    public static string Menu_Build => Get();
    public static string Menu_View => Get();
    public static string Menu_File_Open => Get();
    public static string Menu_File_Exit => Get();
    public static string Menu_Build_BuildMod => Get();
    public static string Menu_View_Language => Get();

    public static string Button_AdvancedSettings => Get();
    public static string AdvancedSettingsForm_Title => Get();
    public static string AdvancedSettingsForm_Help => Get();
    public static string AdvancedSettingsForm_OK => Get();
    public static string AdvancedSettingsForm_Cancel => Get();

    public static string GetAdvancedFieldLabel(string key) =>
        Manager.GetString($"AdvField_{key}", CultureInfo.CurrentUICulture) ?? key;

    private static string Get([CallerMemberName] string? key = null) =>
        Manager.GetString(key!, CultureInfo.CurrentUICulture) ?? key!;
}
