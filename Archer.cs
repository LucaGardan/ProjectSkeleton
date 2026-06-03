namespace TheAdventure;

class Archer
{
    public float x;
    public float y;

    public int hp;
    public int damage;
    public int type;

    public float range;
    public float shootDelay;
    public float shootTimer=0f;

    public bool alive=true;

    public Enemy? targetEnemy=null;
}