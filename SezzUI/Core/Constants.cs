namespace SezzUI
{
    public static class Constants
    {
        public const byte MAX_PLAYER_LEVEL = 90; // Required for cooldown calculations using GetMaxCharges
        public const int PERMANENT_STATUS_DURATION = -1; // Must be negative, used for status effect that haven't ticked yet (like initial Surging Tempest).
    }
}
