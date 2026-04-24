public class Enemy : Character
{
    public Enemy(string name, int maxHP) : base(name, maxHP)
    {
        this.isPlayer = false;
    }
    //public EnemyIntent intent;
}