using UnityEngine;

/// <summary>
/// Distorsion temporelle : chaque carte jouée repousse le tour de quelqu'un.
///
/// <para>Le combattant est tiré au sort parmi les vivants, et la fraction dont son tour recule
/// l'est aussi, jusqu'à la moitié de son délai. Le tirage prend tout le monde, le porteur
/// compris — mais ce qui lui retombe dessus ne compte que pour moitié, ce que la description ne
/// dit délibérément pas.</para>
///
/// <para>Les deux tirages sortent du flux du combat, jamais d'un hasard à part : un combat rejoué
/// doit se dérouler deux fois pareil.</para>
/// </summary>
public class TimeDistortionStatus : StatusEffect
{
    public TimeDistortionStatus()
    {
        Name = "Distorsion temporelle";
        Duration = -1;
        buff = true;
        framed = true;
    }

    public override string Desc(bool isPlayer)
    {
        string who = isPlayer ? "vous jouez" : "ce personnage joue";
        return $"Chaque fois que {who} une carte, le prochain tour d'un personnage au hasard est "
            + "retardé de jusqu'à 50%.";
    }
}
