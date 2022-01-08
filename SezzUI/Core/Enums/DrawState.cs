namespace SezzUI.Enums
{
    public enum DrawState
    {
        HiddenNotInGame, // Not logged in or switching zones aka phasing (loading screens)
        HiddenDisabled, // Plugin drawing manually disabled right now for whatever reason
        HiddenCutscene, // During cutscenes (chat could be toggled by user)
        PartiallyInteraction, // No unitframes (only player castbar), no actionbars
        Visible
    }
}
