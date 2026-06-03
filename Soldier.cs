namespace TheAdventure;

class Soldier
{
    public float x;
    public float y;
    public int hp;
    public int damage;
    public int type;
    public bool alive=true;
    public Enemy? targetEnemy=null;
    public float attackTimer=0f;
}