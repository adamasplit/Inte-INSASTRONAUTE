public class Player : Character
{
        public Player(string name, int maxHP) : base(name, maxHP)
        {
            this.isPlayer = true;
            this.isAlly = true;
            this.isLocalPlayer = true;
        }
}