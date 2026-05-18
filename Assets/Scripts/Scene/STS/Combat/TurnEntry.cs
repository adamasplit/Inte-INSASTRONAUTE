public class TurnEntry
{
    public Character character;
    public float time;
    public TurnVisualType visualType = TurnVisualType.Normal;
    public int uid;
    public static int nextUID = 0;
    public TurnEntry Clone()
    {
        return new TurnEntry
        {
            character = this.character,
            time = this.time,
            visualType = this.visualType,
            uid = this.uid
        };
    }
}