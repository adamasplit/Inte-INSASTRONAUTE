public class AnalyseStatus : StatusEffect
{
    public AnalyseStatus(int value)
    {
        Name = "Analyse";
        Value = value;
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        return $"Quand un adversaire subit un effet négatif, gagnez {Value} d'Armure.";
    }
}