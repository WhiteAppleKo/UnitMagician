namespace UnitSystem
{
    [System.Flags]
    public enum UnitType
    {
        None = 0,
        Mass = 1 << 0,
        Vector = 1 << 2,
        Temperature = 1 << 3
    }
}
