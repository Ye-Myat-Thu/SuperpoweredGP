public interface ICooldownOverrideable
{
    void SetCooldownOverride(bool enabled, float overrideCooldownSeconds);

    void ForceCast();
}
