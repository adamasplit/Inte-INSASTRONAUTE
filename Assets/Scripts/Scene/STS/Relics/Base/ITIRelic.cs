using UnityEngine;
public class ITIRelic:BaseRelic
{
    public ITIRelic():base()
    {
        namesByStage[0] = "Processeur transcendant";
        descriptionsByStage[0] = "Les récompenses de cartes incluent des cartes en lien avec l'ennemi vaincu.";
        namesByStage[1] = "Processeur transcendant - Avancé";
        descriptionsByStage[1] = "Les récompenses de cartes ont une forte chance d'inclure des cartes en lien avec l'ennemi vaincu.";
        namesByStage[2] = "Processeur transcendant - Absorption";
        descriptionsByStage[2] = "Tous les dégâts subis sont reflétés sur un ennemi au hasard.";
        Upgrade(0);
    }
    public int DropRateForEnemyCards()
    {
        switch (stage)
        {
            case 0:
                return 100;
            case 1:
                return 200;
            default:
                return 0;
        }
    }

    public override void OnDamageTaken(Character source, Character target, int amount)
    {
        if (stage == 2)
        {
            var combat = source.combat;
            var enemies = combat.enemies;
            if (enemies.Count == 0)
                return;

            var randomEnemy = enemies[Random.Range(0, enemies.Count)];
            randomEnemy.TakeDamage(amount);
        }
    }
}