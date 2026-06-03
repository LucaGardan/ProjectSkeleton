namespace TheAdventure;

class Enemy
{
    public float x;
    public float y;
    public float speed;
    public int hp;
    public int damage;
    public int reward;
    public int type;
    public int pathIndex=1;
    public bool alive=true;
    public Soldier? targetSoldier=null;
    public Archer? targetArcher=null;
    public float attackTimer=0f;
}