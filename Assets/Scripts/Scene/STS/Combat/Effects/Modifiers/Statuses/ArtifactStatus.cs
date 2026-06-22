public class ArtifactStatus : StatusEffect
{
    public ArtifactStatus(int value)
    {
        Value = value;
        Duration = -1; // Infinite duration
        Name = "Artefact";
        buff=true;
        generic = true;
    }
    public override void Update(Character target)
    {
        if (Value <= 0)
        {
            mustExpire = true;
        }
    }
    public override bool CanApply(StatusEffect newStatus, Character target)
    {
        // If the new status is a debuff, consume one stack of Artifact and prevent the debuff from being applied
        if (newStatus.debuff && Value > 0&& !newStatus.goldFrame)
        {
            Value--;
            if (newStatus.framed)
            {
                Value--; // Consume one extra stack for framed debuffs
            }
            if (Value<0)
            {
                return true; // Allow the debuff to be applied if Artifact is more than consumed
            }
            return false; // Prevent the debuff from being applied
        }
        return true; // Allow other statuses to be applied
    }
    public override string Desc(bool isPlayer)
    {
        return $"Annule les {Value} prochains debuffs.";
    }
}