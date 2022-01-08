namespace SezzUI.Enums
{
    public enum DrawState
    {
        Unknown,
        HiddenNotInGame, // Not logged in or during a loading screens
        HiddenDisabled, // Plugin drawing manually disabled right now for whatever reason
        HiddenCutscene, // During cutscenes (chat could be toggled by user)
        Partially, // No unitframes (propably player castbar), no actionbars - quest interaction or right after a loading screens
        Visible
    }
}
