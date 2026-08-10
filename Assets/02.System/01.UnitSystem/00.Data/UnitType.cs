namespace UnitSystem
{
    [System.Flags]
    public enum UnitType
    {
        None = 0,
        Mass = 1 << 0,
        Volume = 1 << 1,
        Vector = 1 << 2
    }
}
