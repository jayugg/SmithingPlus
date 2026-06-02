namespace SmithingPlus.Config;

public class ClientConfig
{
    public bool ShowRepairedCount { get; set; } = true;
    public bool ShowBrokenCount { get; set; } = true;
    public bool ShowRepairSmithName { get; set; } = false;
    public bool AnvilShowRecipeVoxels { get; set; } = true;
    public bool RememberHammerToolMode { get; set; } = true;
    public bool ShowWorkableTemperature { get; set; } = true;
    public bool HandbookExtraInfo { get; set; } = true;
    public int AnvilRecipeSelectionColumns { get; set; } = 8;
}
