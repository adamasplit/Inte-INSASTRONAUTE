/// <summary>
/// Processeur transcendant : tout ce qu'on vous lance vous revient une fois.
///
/// <para>Chaque carte jouée par l'autre camp laisse une copie dans votre main — les mêmes effets,
/// sous un nom construit à partir d'eux, éthérée et à usage unique. La copie est une Attaque si
/// ces effets frappent, une Compétence sinon, et ce quelle que soit la carte dont elle vient.</para>
///
/// <para><b>L'effet lui-même vit côté serveur</b>, dans <c>ITIRelicHandler</c> : c'est le moteur
/// autoritatif qui décide ce que la relique produit, et le client reçoit la copie comme n'importe
/// quelle autre carte de sa main. La reproduire ici n'ajouterait rien et casserait quelque chose —
/// la main du client est remplacée en entier à chaque synchronisation d'état, donc une copie
/// ajoutée localement serait soit effacée, soit comptée deux fois.</para>
///
/// <para>Ce qui reste ici est ce que le serveur ne porte pas : le nom et le texte que le joueur
/// lit à la sélection de personnage et dans son inventaire.</para>
/// </summary>
public class ITIRelic : BaseRelic
{
    public ITIRelic() : base()
    {
        namesByStage[0] = "Processeur transcendant";
        descriptionsByStage[0] =
            "Chaque carte jouée contre vous en laisse une copie <color=orange>temporaire</color>" + "et <color=orange>à usage unique</color> dans votre main.";
        Upgrade(0);
    }
}
