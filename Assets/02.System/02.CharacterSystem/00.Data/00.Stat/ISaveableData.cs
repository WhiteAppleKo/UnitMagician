namespace CharacterSystem
{
    public interface ISaveableData
    {
        string SaveToJson();
        void LoadFromJson(string json);
    }
}
