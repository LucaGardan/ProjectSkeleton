    using Silk.NET.SDL;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;
    using SdlRect=Silk.NET.Maths.Rectangle<int>;

    namespace TheAdventure;

    public static class Program
    {
        const int LogicalW=1280;
        const int LogicalH=720;

        struct Area
        {
            public int X;
            public int Y;
            public int W;
            public int H;

            public Area(int x,int y,int w,int h)
            {
                X=x;
                Y=y;
                W=w;
                H=h;
            }
        }

        enum EnemyType
        {
            Normal,
            Runner,
            Heavy
        }

        enum UnitState
        {
            Walk,
            Attack,
            Death
        }
        class MovingUnit
    {
        public float X;
        public float Y;
        public int PathIndex;
        public float Speed;
        public bool FromRight;
        public int Health;
        public int MaxHealth;
        public EnemyType Type;
        public MovingUnit? Target;
        public float AttackTimer;

        public int AnimFrame;
        public float AnimTimer;
        public float AnimSpeed;
        public UnitState State;

        public bool IsDead;
        public float DeathTimer;
    }

       class TowerSlot
{
    public int X;
    public int Y;
    public int R;
    public int ArcherCount;
    public int ArcherType;

    public int[] AnimFrames={0,0,0};
    public float[] AnimTimers={0f,0f,0f};
    public float AnimSpeed=0.08f;

    public bool[] IsAttacking={false,false,false};
    public float[] AttackAnimTimers={0f,0f,0f};
    public IntPtr[] CurrentAttackTextures={IntPtr.Zero,IntPtr.Zero,IntPtr.Zero};

    public bool[] FlipArchers={false,false,false};
    public float[] ArcherCooldowns={0f,0.25f,0.5f};
}
        
        class Arrow
        {
            public float X;
            public float Y;
            public MovingUnit? Target;
            public float Speed;
            public int Damage;
        }

        enum ScreenState
        {
            MainMenu,
            Playing,
            Paused,
            Victory,
            GameOver
        }

        static ScreenState screen=ScreenState.MainMenu;
        static ScreenState previousScreen=ScreenState.MainMenu;

        static IntPtr window=IntPtr.Zero;
        static IntPtr renderer=IntPtr.Zero;

        static IntPtr menuTexture=IntPtr.Zero;
        static IntPtr mapTexture=IntPtr.Zero;
        static IntPtr shopTexture=IntPtr.Zero;
        static IntPtr pauseTexture=IntPtr.Zero;
        static IntPtr victoryTexture=IntPtr.Zero;

        static IntPtr heavyWalkTexture=IntPtr.Zero;
        static IntPtr heavyAttackTexture=IntPtr.Zero;
        static IntPtr heavyDeathTexture=IntPtr.Zero;
        static IntPtr soldierWalkTexture=IntPtr.Zero;
        static IntPtr soldierAttackTexture=IntPtr.Zero;
        static IntPtr soldierDeathTexture=IntPtr.Zero;

        static IntPtr normalWalkTexture=IntPtr.Zero;
        static IntPtr normalAttackTexture=IntPtr.Zero;
        static IntPtr normalDeathTexture=IntPtr.Zero;

        static IntPtr runnerWalkTexture=IntPtr.Zero;
        static IntPtr runnerAttackTexture=IntPtr.Zero;
        static IntPtr runnerDeathTexture=IntPtr.Zero;

        static IntPtr archerIdleTexture=IntPtr.Zero;
        static IntPtr archerAttack1Texture=IntPtr.Zero;
        static IntPtr archerAttack2Texture=IntPtr.Zero;

        static IntPtr gameOverTexture=IntPtr.Zero;

        static int selectedShopItem=1;
        static int gold=100;

        static int kills=0;
        static float survivedTime=0f;

        static int enemySpawnIndex=0;

        static float prepTime=10f;
        static float gameTime=120f;

        static bool inPrep=true;

        static readonly int[] costs={30,45,60,30,50,80,120,200};

        static List<MovingUnit> enemies=new();
        static List<MovingUnit> soldiers=new();
        
        static List<Arrow> arrows=new();

        static float archerTimer=0f;
        static float archerDelay=0.7f;

        static float enemyTimer=0f;
        static float enemyDelay=2f;

        static int currentWave=0;
        static int enemiesLeftInWave=0;
        static float waveTimer=0f;
        static float waveDelay=4f;
        static DateTime lastTime=DateTime.Now;

        static readonly (int x,int y)[] roadPath=
        {
            (75,270),
            (165,370),
            (165,370),
            (200,390),
            (245,390),
            (290,360),

            (335,310),
            (380,290),
            (425,310),
            (470,355),
            (515,380),
            (555,385),

            (595,385),
            (630,385),
            (665,380),

            (705,370),
            (745,320),
            (785,300),
            (825,310),

            (870,345),
            (915,345),
            (960,310),
            (1005,310),
            (1050,350),
            (1095,385),

            (1140,375),
            (1185,330)
        };

        static readonly TowerSlot[] towers=
        {
            new TowerSlot{X=380,Y=150,R=48},
            new TowerSlot{X=780,Y=150,R=48},
            new TowerSlot{X=980,Y=150,R=48},

            new TowerSlot{X=355,Y=500,R=48},
            new TowerSlot{X=785,Y=500,R=48},
            new TowerSlot{X=1005,Y=500,R=48}
        };

        static readonly (int x,int y,int w,int h)[] menuButtons=
        {
            (850,335,360,105),
            (850,465,360,105)
        };

        static readonly (int x,int y,int w,int h)[] pauseButtons=
        {
            (370,470,700,115),
            (370,610,700,115),
            (370,750,700,115)
        };

        static readonly (int x,int y,int w,int h)[] shopCards=
        {
            (45,545,85,140),
            (145,545,85,140),
            (245,545,85,140),
            (345,545,85,140),
            (445,545,85,140),
            (545,545,85,140),
            (645,545,85,140),
            (745,545,85,140)
        };

        static readonly (int x,int y,int w,int h) victoryMenuButton=(380,760,780,130);
        static readonly (int x,int y,int w,int h) gameOverMenuButton=(300,860,820,150);

        public static unsafe void Main()
        {
            var sdl=new Sdl(new SdlContext());

            if(sdl.Init(Sdl.InitVideo|Sdl.InitEvents|Sdl.InitTimer)<0)
                throw new Exception("SDL init failed");

            window=(IntPtr)sdl.CreateWindow(
                "Archers Stand",
                Sdl.WindowposCentered,
                Sdl.WindowposCentered,
                LogicalW,
                LogicalH,
                (uint)WindowFlags.AllowHighdpi
            );

            if(window==IntPtr.Zero)
                throw new Exception("Window creation failed");

            renderer=(IntPtr)sdl.CreateRenderer((Window*)window,-1,(uint)RendererFlags.Accelerated|(uint)RendererFlags.Presentvsync);

            if(renderer==IntPtr.Zero)
                throw new Exception("Renderer creation failed");

            sdl.RenderSetLogicalSize((Renderer*)renderer,LogicalW,LogicalH);

            menuTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/menu.png");
            mapTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/map.png");
            shopTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/shop.png");
            pauseTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/pause.png");
            victoryTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/victory.png");
            gameOverTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/gameover.png");
            heavyWalkTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_hWalk.png");
            heavyAttackTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_hAttack.png");
            heavyDeathTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_hDeath.png");
            soldierWalkTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_kWalk.png");
            soldierAttackTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_kAttack.png");
            soldierDeathTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_kDeath.png");
            normalWalkTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_nWalk.png");
            normalAttackTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_nAttack.png");
            normalDeathTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_nDeath.png");
            runnerWalkTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_rWalk.png");
            runnerAttackTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_rAttack.png");
            runnerDeathTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/S_rDeath.png");
            archerIdleTexture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/A_idle.png");
            archerAttack1Texture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/A_attack1.png");
            archerAttack2Texture=(IntPtr)LoadTexture(sdl,(Renderer*)renderer,"Assets/A_attack2.png");

            bool quit=false;
            var ev=new Event();

            while(!quit)
            {
                DateTime now=DateTime.Now;
                float dt=(float)(now-lastTime).TotalSeconds;
                lastTime=now;

                while(sdl.PollEvent(ref ev)!=0)
                {
                    if(ev.Type==(uint)EventType.Quit)
                    {
                        quit=true;
                        break;
                    }

                    if(ev.Type==(uint)EventType.Keydown)
                        HandleKey(ev,ref quit);

                    if(ev.Type==(uint)EventType.Mousebuttondown && ev.Button.Button==1)
                    {
                        var p=MouseToLogical(sdl,ev.Button.X,ev.Button.Y);
                        HandleClick(p.x,p.y,ref quit);
                    }
                }

                if(screen==ScreenState.Playing)
                    UpdateGame(dt);

                Draw(sdl,(Renderer*)renderer);
                sdl.RenderPresent((Renderer*)renderer);
            }

            DestroyTexture(sdl,menuTexture);
            DestroyTexture(sdl,mapTexture);
            DestroyTexture(sdl,shopTexture);
            DestroyTexture(sdl,pauseTexture);
            DestroyTexture(sdl,victoryTexture);
            DestroyTexture(sdl,gameOverTexture);
            DestroyTexture(sdl,heavyWalkTexture);
            DestroyTexture(sdl,heavyAttackTexture);
            DestroyTexture(sdl,heavyDeathTexture);
            DestroyTexture(sdl,soldierWalkTexture);
            DestroyTexture(sdl,soldierAttackTexture);
            DestroyTexture(sdl,soldierDeathTexture);
            DestroyTexture(sdl,normalWalkTexture);
            DestroyTexture(sdl,normalAttackTexture);
            DestroyTexture(sdl,normalDeathTexture);
            DestroyTexture(sdl,runnerWalkTexture);
            DestroyTexture(sdl,runnerAttackTexture);
            DestroyTexture(sdl,runnerDeathTexture);
            DestroyTexture(sdl,archerIdleTexture);
            DestroyTexture(sdl,archerAttack1Texture);
            DestroyTexture(sdl,archerAttack2Texture);

            sdl.DestroyRenderer((Renderer*)renderer);
            sdl.DestroyWindow((Window*)window);
            sdl.Quit();
        }

        static void HandleKey(Event ev,ref bool quit)
        {
            var key=(KeyCode)ev.Key.Keysym.Scancode;

            if(key==KeyCode.Escape)
            {
                if(screen==ScreenState.Playing)
                    Pause();
                else if(screen==ScreenState.Paused)
                    screen=previousScreen;
                else if(screen==ScreenState.MainMenu)
                    quit=true;
                else
                    screen=ScreenState.MainMenu;
            }

            if(key==KeyCode.P && screen==ScreenState.Playing)
                Pause();

            if(key==KeyCode.V && screen==ScreenState.Playing)
                screen=ScreenState.Victory;

            if(key==KeyCode.G && screen==ScreenState.Playing)
                screen=ScreenState.GameOver;
        }

        static void HandleClick(int x,int y,ref bool quit)
        {
            if(screen==ScreenState.MainMenu)
            {
                Area rect=FitArea(2172,724);
                var src=PointToTextureSource(menuTexture,x,y,2172,724,rect);

                if(IsInside(src.x,src.y,menuButtons[0]))
                    StartGame();
                else if(IsInside(src.x,src.y,menuButtons[1]))
                    quit=true;

                return;
            }

            if(screen==ScreenState.Playing)
            {
                if(IsInside(x,y,(1210,15,55,45)))
                {
                    Pause();
                    return;
                }

                if(y>=520)
                {
                    SelectShopItem(x,y);
                    return;
                }

                if(selectedShopItem<=3)
                {
                    if(IsNearRoad(x,y) && gold>=costs[selectedShopItem-1])
                    {
                        gold-=costs[selectedShopItem-1];
                        SpawnSoldier();
                    }
                }
                else
                {
                    int towerIndex=GetTowerIndex(x,y);

                    if(towerIndex!=-1 && towers[towerIndex].ArcherCount<3 && gold>=costs[selectedShopItem-1])
                    {
                        gold-=costs[selectedShopItem-1];
                        towers[towerIndex].ArcherCount++;
                        towers[towerIndex].ArcherType=selectedShopItem;
                    }
                }

                return;
            }

            if(screen==ScreenState.Paused)
            {
                Area rect=FitArea(1402,1120,80);
                var src=PointToTextureSource(pauseTexture,x,y,1402,1120,rect);

                if(IsInside(src.x,src.y,pauseButtons[0]))
                    screen=previousScreen;
                else if(IsInside(src.x,src.y,pauseButtons[1]))
                    StartGame();
                else if(IsInside(src.x,src.y,pauseButtons[2]))
                    screen=ScreenState.MainMenu;

                return;
            }

            if(screen==ScreenState.Victory)
            {
                Area rect=FitArea(1536,1024,80);
                var src=PointToTextureSource(victoryTexture,x,y,1536,1024,rect);

                if(IsInside(src.x,src.y,victoryMenuButton))
                    screen=ScreenState.MainMenu;

                return;
            }

            if(screen==ScreenState.GameOver)
            {
                Area rect=FitArea(1402,1120,80);
                var src=PointToTextureSource(gameOverTexture,x,y,1402,1120,rect);

                if(IsInside(src.x,src.y,gameOverMenuButton))
                    screen=ScreenState.MainMenu;
            }
        }

        static void StartGame()
        {
            screen=ScreenState.Playing;
            gold=120;
            kills=0;
            survivedTime=0f;
            prepTime=10f;
            gameTime=120f;
            inPrep=true;
            selectedShopItem=1;
            enemySpawnIndex=0;
            enemies.Clear();
            soldiers.Clear();
            arrows.Clear();
            archerTimer=0f;
            currentWave=0;
            enemiesLeftInWave=0;
            waveTimer=0f;
            enemyDelay=1.8f;
            foreach(var t in towers)
            {
                t.ArcherCount=0;
                t.ArcherType=0;
            }

            enemyTimer=0f;
            lastTime=DateTime.Now;
        }

        static void Pause()
        {
            previousScreen=screen;
            screen=ScreenState.Paused;
        }

        static void UpdateGame(float dt)
        {
            if(inPrep)
            {
                prepTime-=dt;

                if(prepTime<=0f)
                {
                    prepTime=0f;
                    inPrep=false;
                }
            }
            else
            {
                enemyTimer+=dt;
                gameTime-=dt;
                survivedTime=120f-gameTime;
                UpdateWaves(dt);
                if(gameTime<=0f)
                {
                    gameTime=0f;
                    screen=ScreenState.Victory;
                    return;
                }
                if(enemyTimer>=enemyDelay)
                {
                    enemyTimer=0f;
                    SpawnEnemy();
                }
                UpdateDuels(dt);
                UpdateArrows(dt);
                RemoveDeadUnits(dt);

                MoveUnits(enemies,dt);
                MoveUnits(soldiers,dt);

                UpdateArchers(dt);
                RemoveDeadUnits(dt);
                foreach(var t in towers)
                    UpdateTowerAnimation(t,dt);
            }
        }

    static void SpawnEnemy()
{
    float t=survivedTime;

    SpawnEnemyOfType(EnemyType.Normal);

    if(t>=35f && t<65f)
    {
        if(Random.Shared.Next(100)<30)
            SpawnEnemyOfType(EnemyType.Runner);
    }
    else if(t>=65f && t<90f)
    {
        if(Random.Shared.Next(100)<65)
            SpawnEnemyOfType(EnemyType.Runner);

        if(Random.Shared.Next(100)<30)
            SpawnEnemyOfType(EnemyType.Heavy);
    }
    else if(t>=90f)
    {
        if(Random.Shared.Next(100)<85)
            SpawnEnemyOfType(EnemyType.Runner);

        if(Random.Shared.Next(100)<55)
            SpawnEnemyOfType(EnemyType.Heavy);

        if(Random.Shared.Next(100)<35)
            SpawnEnemyOfType(EnemyType.Normal);

        if(Random.Shared.Next(100)<20)
            SpawnEnemyOfType(EnemyType.Runner);
    }
}    
    static void SpawnEnemyOfType(EnemyType type)
{
    int hp=type switch
    {
        EnemyType.Normal=>240,
        EnemyType.Runner=>300,
        EnemyType.Heavy=>500,
        _=>240
    };

    float speed=type switch
    {
        EnemyType.Normal=>45f,
        EnemyType.Runner=>65f,
        EnemyType.Heavy=>28f,
        _=>45f
    };

    enemies.Add(new MovingUnit{
        X=roadPath[0].x,
        Y=roadPath[0].y,
        PathIndex=1,
        Speed=speed,
        FromRight=false,
        Health=hp,
        MaxHealth=hp,
        Type=type,
        AnimFrame=0,
        AnimTimer=0f,
        AnimSpeed=type switch
        {
            EnemyType.Normal=>0.10f,
            EnemyType.Runner=>0.08f,
            EnemyType.Heavy=>0.12f,
            _=>0.10f
        },
        State=UnitState.Walk
    });
}
        static EnemyType GetWaveEnemyType()
{
    float t=survivedTime;

    int n;
    int r;
    int h;

    if(t<35f)
    {
        n=85;
        r=15;
        h=0;
    }
    else if(t<65f)
    {
        n=70;
        r=35;
        h=0;
    }
    else if(t<90f)
    {
        n=60;
        r=50;
        h=10;
    }
    else
    {
        n=60;
        r=50;
        h=20;
    }

    if(Random.Shared.Next(100)<h)
        return EnemyType.Heavy;

    if(Random.Shared.Next(100)<r)
        return EnemyType.Runner;

    return EnemyType.Normal;
}
        static void SpawnSoldier()
    {
        int last=roadPath.Length-1;

        int hp=selectedShopItem switch
        {
            1=>360,
            2=>540,
            3=>720,
            _=>360
        };

        soldiers.Add(new MovingUnit{
            X=roadPath[last].x,
            Y=roadPath[last].y,
            PathIndex=last-1,
            Speed=60,
            FromRight=true,
            Health=hp,
            MaxHealth=hp,
            AnimFrame=0,
            AnimTimer=0f,
            AnimSpeed=0.06f,
            State=UnitState.Walk
        });
    }

        static void MoveUnits(List<MovingUnit> list,float dt)
    {
        
        for(int i=list.Count-1;i>=0;i--)
        {
            MovingUnit u=list[i];
            if(u.IsDead)
            {
                UpdateAnimation(u,dt);
                continue;
            }
            if(u.Target!=null)
            {
                u.State=UnitState.Attack;
                UpdateAnimation(u,dt);
                continue;
            }

            u.State=UnitState.Walk;
            UpdateAnimation(u,dt);

            if(u.PathIndex<0 || u.PathIndex>=roadPath.Length)
            {
                if(list==enemies)
                {
                    gold-=15;

                    if(gold<=0)
                    {
                        gold=0;
                        screen=ScreenState.GameOver;
                    }
                }

                list.RemoveAt(i);
                continue;
            }

            float tx=roadPath[u.PathIndex].x;
            float ty=roadPath[u.PathIndex].y;

            float dx=tx-u.X;
            float dy=ty-u.Y;
            float dist=MathF.Sqrt(dx*dx+dy*dy);

            if(dist<5f)
            {
                if(u.FromRight)
                    u.PathIndex--;
                else
                    u.PathIndex++;

                if(u.PathIndex<0 || u.PathIndex>=roadPath.Length)
                {
                    if(list==enemies)
                    {
                        gold-=15;

                        if(gold<=0)
                        {
                            gold=0;
                            screen=ScreenState.GameOver;
                        }
                    }

                    list.RemoveAt(i);
                }

                continue;
            }

            u.X+=dx/dist*u.Speed*dt;
            u.Y+=dy/dist*u.Speed*dt;
        }
    }

        static void UpdateWaves(float dt)
{
    float t=survivedTime;

    enemyDelay=t switch
    {
        <35f=>1.6f,
        <65f=>1.25f,
        <90f=>0.95f,
        _=>0.55f
    };
}
        static void UpdateArchers(float dt)
{
    foreach(var t in towers)
    {
        if(t.ArcherCount<=0)
            continue;

        float range=GetArcherRange(t.ArcherType);

        (int x,int y)[] pos=
        {
            (t.X-18,t.Y-8),   // left
            (t.X+6,t.Y-8),    // right
            (t.X-6,t.Y-28)    // top
        };

        for(int i=0;i<t.ArcherCount;i++)
        {
            t.ArcherCooldowns[i]-=dt;

            if(t.ArcherCooldowns[i]>0f)
                continue;

            MovingUnit? target=FindEnemyInRange(t.X,t.Y,range);

            if(target==null)
                continue;

            t.FlipArchers[i]=target.X<pos[i].x;
            t.ArcherCooldowns[i]=archerDelay;

            t.IsAttacking[i]=true;
            t.AttackAnimTimers[i]=0f;
            t.AnimFrames[i]=0;
            t.AnimTimers[i]=0f;
            t.CurrentAttackTextures[i]=(t.ArcherType%2==0)?archerAttack1Texture:archerAttack2Texture;

            arrows.Add(new Arrow{
                X=pos[i].x+22,
                Y=pos[i].y+22,
                Target=target,
                Speed=420f,
                Damage=GetArcherDamage(t.ArcherType)
            });
        }
    }
}

        static float GetArcherRange(int type)
        {
            return type switch
            {
                4=>180f,
                5=>220f,
                6=>260f,
                7=>300f,
                8=>350f,
                _=>180f
            };
        }

        static int GetArcherDamage(int type)
        {
            return type switch
            {
                4=>20,
                5=>30,
                6=>40,
                7=>60,
                8=>90,
                _=>20
            };
        }

        static MovingUnit? FindEnemyInRange(int x,int y,float range)
        {
            MovingUnit? best=null;
            float bestDist=range;

            foreach(var e in enemies)
            {
                float dx=e.X-x;
                float dy=e.Y-y;
                float dist=MathF.Sqrt(dx*dx+dy*dy);

                if(dist<=bestDist)
                {
                    bestDist=dist;
                    best=e;
                }
            }

            return best;
        }

        static void UpdateArrows(float dt)
    {
        for(int i=arrows.Count-1;i>=0;i--)
        {
            Arrow a=arrows[i];

            if(a.Target==null || !enemies.Contains(a.Target) || a.Target.IsDead)
            {
                arrows.RemoveAt(i);
                continue;
            }

            float dx=a.Target.X-a.X;
            float dy=a.Target.Y-a.Y;
            float dist=MathF.Sqrt(dx*dx+dy*dy);

            if(dist<8f)
            {
                a.Target.Health-=a.Damage;
                arrows.RemoveAt(i);
                continue;
            }

            a.X+=dx/dist*a.Speed*dt;
            a.Y+=dy/dist*a.Speed*dt;
        }
    }

        static int GetEnemyGoldReward(MovingUnit e)
        {
            return e.Type switch
            {
                EnemyType.Normal=>15,
                EnemyType.Runner=>30,
                EnemyType.Heavy=>50,
                _=>10
            };
        }

        static void UpdateDuels(float dt)
    {
        foreach(var s in soldiers)
        {
            if(s.IsDead)
                continue;
            if(s.Target!=null && (!enemies.Contains(s.Target) || s.Target.IsDead))
            {
                s.Target=null;
                s.State=UnitState.Walk;
                s.AnimFrame=0;
            }

            if(s.Target==null)
            {
                foreach(var e in enemies)
                {
                    if(e.IsDead)
                        continue;

                    if(e.Target!=null)
                        continue;

                    float dx=e.X-s.X;
                    float dy=e.Y-s.Y;

                    if(dx*dx+dy*dy<=400)
                    {
                        s.Target=e;
                        e.Target=s;
                        s.AttackTimer=0f;
                        e.AttackTimer=0f;
                        s.State=UnitState.Attack;
                        s.AnimFrame=0;
                        s.AnimTimer=0f;

                        e.State=UnitState.Attack;
                        e.AnimFrame=0;
                        e.AnimTimer=0f;
                        break;
                    }
                }
            }

            if(s.Target==null)
                continue;

            MovingUnit enemy=s.Target;

            if(enemy.IsDead || s.IsDead)
            {   
                s.Target=null;
                enemy.Target=null;

                if(!s.IsDead)
                    s.State=UnitState.Walk;

                if(!enemy.IsDead)
                    enemy.State=UnitState.Walk;

                continue;
            }

            s.AttackTimer+=dt;
            enemy.AttackTimer+=dt;

            if(s.AttackTimer>=0.8f)
            {
                s.AttackTimer=0f;
                enemy.Health-=GetSoldierDamage(s);
            }

            if(enemy.AttackTimer>=1.0f)
            {
                enemy.AttackTimer=0f;
                s.Health-=GetEnemyDamage(enemy);
            }
        }
    }

        static int GetSoldierDamage(MovingUnit s)
    {
        return s.MaxHealth switch
        {
            360=>Random.Shared.Next(75,96),
            540=>Random.Shared.Next(80,101),
            720=>Random.Shared.Next(100,126),
            _=>85
        };
    }

        static int GetEnemyDamage(MovingUnit e)
    {
        return e.Type switch
        {
            EnemyType.Normal=>Random.Shared.Next(65,81),
            EnemyType.Runner=>Random.Shared.Next(90,111),
            EnemyType.Heavy=>Random.Shared.Next(165,196),
            _=>70
        };
    }

        static void RemoveDeadUnits(float dt)
    {
        for(int i=soldiers.Count-1;i>=0;i--)
        {
            MovingUnit s=soldiers[i];

            if(s.Health<=0 && !s.IsDead)
            {
                s.IsDead=true;
                s.State=UnitState.Death;
                s.AnimFrame=0;
                s.AnimTimer=0f;
                s.DeathTimer=0f;

                if(s.Target!=null)
                {
                    s.Target.Target=null;
                    s.Target.State=UnitState.Walk;
                    s.Target.AnimFrame=0;
                }

                s.Target=null;
            }

            if(s.IsDead)
            {
                s.DeathTimer+=dt;

                if(s.DeathTimer>=0.8f)
                    soldiers.RemoveAt(i);
            }
        }

        for(int i=enemies.Count-1;i>=0;i--)
        {
            MovingUnit e=enemies[i];

            if(e.Health<=0 && !e.IsDead)
    {
        e.IsDead=true;
        e.State=UnitState.Death;
        e.Speed=0;
        e.AnimFrame=0;
        e.AnimTimer=0f;
        e.AnimSpeed=0.12f;
        e.DeathTimer=0f;

        if(e.Target!=null)
        {
            e.Target.Target=null;
            e.Target.State=UnitState.Walk;
            e.Target.AnimFrame=0;
            e.Target.AnimTimer=0f;
        }

        e.Target=null;

        gold+=GetEnemyGoldReward(e);
        kills++;
    }

            if(e.IsDead)
            {
                e.DeathTimer+=dt;

                if(e.DeathTimer>=1.5f)
                    enemies.RemoveAt(i);
            }
        }
    }
        static bool IsNearRoad(int x,int y)
        {
            for(int i=0;i<roadPath.Length-1;i++)
            {
                float d=DistanceToSegment(x,y,roadPath[i].x,roadPath[i].y,roadPath[i+1].x,roadPath[i+1].y);

                if(d<=38)
                    return true;
            }

            return false;
        }

        static float DistanceToSegment(float px,float py,float ax,float ay,float bx,float by)
        {
            float dx=bx-ax;
            float dy=by-ay;

            if(dx==0 && dy==0)
                return MathF.Sqrt((px-ax)*(px-ax)+(py-ay)*(py-ay));

            float t=((px-ax)*dx+(py-ay)*dy)/(dx*dx+dy*dy);
            t=Math.Clamp(t,0,1);

            float cx=ax+t*dx;
            float cy=ay+t*dy;

            return MathF.Sqrt((px-cx)*(px-cx)+(py-cy)*(py-cy));
        }

        static int GetTowerIndex(int x,int y)
        {
            for(int i=0;i<towers.Length;i++)
            {
                int dx=x-towers[i].X;
                int dy=y-towers[i].Y;

                if(dx*dx+dy*dy<=towers[i].R*towers[i].R)
                    return i;
            }

            return -1;
        }

        static void SelectShopItem(int x,int y)
        {
            int localX=x;
            int localY=y-520;

            if(localX<0 || localX>1280 || localY<0 || localY>200)
                return;

            int sourceX=(int)(localX*797f/1280f);
            int sourceY=(int)(localY*313f/200f);

            int[] lefts={35,110,175,245,304,375,440,510};

            for(int i=0;i<lefts.Length;i++)
            {
                if(IsInside(sourceX,sourceY,(lefts[i],85,56,190)))
                {
                    selectedShopItem=i+1;
                    return;
                }
            }
        }

        static void UpdateAnimation(MovingUnit u,float dt)
{
    u.AnimTimer+=dt;

    if(u.AnimTimer>=u.AnimSpeed)
    {
        u.AnimTimer=0f;
        u.AnimFrame++;
    }

    int maxFrames=6;

    if(u.AnimFrame>=maxFrames)
    {
        if(u.State==UnitState.Death)
            u.AnimFrame=maxFrames-1;
        else
            u.AnimFrame=0;
    }
}
        static unsafe void Draw(Sdl sdl,Renderer* r)
        {
            sdl.SetRenderDrawColor(r,0,0,0,255);
            sdl.RenderClear(r);

            if(screen==ScreenState.MainMenu)
            {
                DrawTextureFit(sdl,r,menuTexture,2172,724);
                return;
            }

            DrawTextureFill(sdl,r,mapTexture,2048,1151);

            //DrawRoadDebug(sdl,r);
            DrawUnits(sdl,r);
            DrawArrows(sdl,r);
            DrawTowerArchers(sdl,r);
            DrawShop(sdl,r);
            DrawPauseButton(sdl,r);

            if(screen==ScreenState.Paused)
            {
                DrawOverlay(sdl,r);
                DrawTextureFit(sdl,r,pauseTexture,1402,1120,80);
            }
            else if(screen==ScreenState.Victory)
            {
                DrawOverlay(sdl,r);
                DrawTextureFit(sdl,r,victoryTexture,1536,1024,80);

                DrawNumberCentered(sdl,r,kills,775,330,2);
                DrawNumberCentered(sdl,r,(int)survivedTime,775,425,2);
            }
            else if(screen==ScreenState.GameOver)
            {
                DrawOverlay(sdl,r);
                DrawTextureFit(sdl,r,gameOverTexture,1402,1120,80);

                DrawNumberCentered(sdl,r,kills,795,380,2);
                DrawNumberCentered(sdl,r,(int)survivedTime,795,448,2);
            }
        }

        static unsafe void DrawRoadDebug(Sdl sdl,Renderer* r)
        {
            sdl.SetRenderDrawBlendMode(r,BlendMode.Blend);
            sdl.SetRenderDrawColor(r,255,220,120,110);

            for(int i=0;i<roadPath.Length-1;i++)
                DrawThickLine(sdl,r,roadPath[i].x,roadPath[i].y,roadPath[i+1].x,roadPath[i+1].y,3);
        }

        static void UpdateTowerAnimation(TowerSlot t,float dt)
{
    for(int i=0;i<t.ArcherCount;i++)
    {
        if(!t.IsAttacking[i])
        {
            t.AnimFrames[i]=0;
            t.AnimTimers[i]=0f;
            continue;
        }

        t.AnimTimers[i]+=dt;
        t.AttackAnimTimers[i]+=dt;

        if(t.AnimTimers[i]>=t.AnimSpeed)
        {
            t.AnimTimers[i]=0f;
            t.AnimFrames[i]++;
        }

        if(t.AnimFrames[i]>=6)
        {
            t.AnimFrames[i]=0;
            t.IsAttacking[i]=false;
            t.AttackAnimTimers[i]=0f;
        }
    }
}
        static unsafe void DrawHeavySprite(Sdl sdl,Renderer* r,MovingUnit e)
    {
        IntPtr texture=IntPtr.Zero;

        if(e.State==UnitState.Walk)
            texture=heavyWalkTexture;
        else if(e.State==UnitState.Attack)
            texture=heavyAttackTexture;
        else if(e.State==UnitState.Death)
            texture=heavyDeathTexture;

        if(texture==IntPtr.Zero)
            return;

        int frameW=96;
        int frameH=96;
        if(e.State==UnitState.Death && e.AnimFrame>5)
            e.AnimFrame=5;
        var src=new SdlRect(e.AnimFrame*frameW,0,frameW,frameH);
        var dst=new SdlRect((int)e.X-32,(int)e.Y-42,96,96);

        var flip=!e.FromRight?RendererFlip.FlipHorizontal:RendererFlip.None;
        sdl.RenderCopyEx(r,(Texture*)texture,&src,&dst,0,null,flip);
    }
        static unsafe void DrawNormalSprite(Sdl sdl,Renderer* r,MovingUnit e)
{
    DrawEnemySheetSprite(sdl,r,e,normalWalkTexture,normalAttackTexture,normalDeathTexture);
}

static unsafe void DrawRunnerSprite(Sdl sdl,Renderer* r,MovingUnit e)
{
    DrawEnemySheetSprite(sdl,r,e,runnerWalkTexture,runnerAttackTexture,runnerDeathTexture);
}
    static unsafe void DrawEnemySheetSprite(Sdl sdl,Renderer* r,MovingUnit e,IntPtr walkTex,IntPtr attackTex,IntPtr deathTex)
{
    IntPtr texture=IntPtr.Zero;

    if(e.State==UnitState.Walk)
        texture=walkTex;
    else if(e.State==UnitState.Attack)
        texture=attackTex;
    else if(e.State==UnitState.Death)
        texture=deathTex;

    if(texture==IntPtr.Zero)
        return;

    uint format=0;
    int access=0;
    int texW=0;
    int texH=0;

    sdl.QueryTexture((Texture*)texture,&format,&access,&texW,&texH);

    int frameCount=6;
    int frameW=texW/frameCount;
    int frameH=texH;

    if(e.AnimFrame>=frameCount)
        e.AnimFrame=0;

    var src=new SdlRect(e.AnimFrame*frameW,0,frameW,frameH);
    var dst=new SdlRect((int)e.X-32,(int)e.Y-42,64,64);

    var flip=!e.FromRight?RendererFlip.FlipHorizontal:RendererFlip.None;
    sdl.RenderCopyEx(r,(Texture*)texture,&src,&dst,0,null,flip);
}
        static unsafe void DrawSoldierSprite(Sdl sdl,Renderer* r,MovingUnit s)
    {
        IntPtr texture=IntPtr.Zero;

        if(s.State==UnitState.Walk)
            texture=soldierWalkTexture;
        else if(s.State==UnitState.Attack)
            texture=soldierAttackTexture;
        else if(s.State==UnitState.Death)
            texture=soldierDeathTexture;

        if(texture==IntPtr.Zero)
            return;

        int frameW=96;
        int frameH=96;

        var src=new SdlRect(s.AnimFrame*frameW,0,frameW,frameH);

        // Soldiers go LEFT → no flip
        var dst=new SdlRect((int)s.X-32,(int)s.Y-42,96,96);

        sdl.RenderCopy(r,(Texture*)texture,&src,&dst);
    }
        static unsafe void DrawUnits(Sdl sdl,Renderer* r)
        {
            foreach(var e in enemies)
            {
                switch(e.Type)
                {
                    case EnemyType.Normal:
                    DrawNormalSprite(sdl,r,e);
                    break;

                    case EnemyType.Runner:
                    DrawRunnerSprite(sdl,r,e);
                    break;

                    case EnemyType.Heavy:
                    DrawHeavySprite(sdl,r,e);
                    break;
                }
            }

            foreach(var s in soldiers)
                DrawSoldierSprite(sdl,r,s);
        }

        static unsafe void DrawArrows(Sdl sdl,Renderer* r)
        {
            foreach(var a in arrows)
                DrawCircle(sdl,r,(int)a.X,(int)a.Y,4,240,220,80,255);
        }

        static unsafe void DrawTowerArchers(Sdl sdl,Renderer* r)
{
    foreach(var t in towers)
    {
        DrawCircleOutline(sdl,r,t.X,t.Y,t.R,210,170,80,120);

        if(t.ArcherCount<=0)
            continue;

        (int x,int y)[] pos=
        {
            (t.X-18,t.Y-8),   // left
            (t.X+6,t.Y-8),    // right
            (t.X-6,t.Y-28)    // top
        };

        for(int i=0;i<t.ArcherCount;i++)
        {
            IntPtr texture=t.IsAttacking[i]?t.CurrentAttackTextures[i]:archerIdleTexture;

            if(texture==IntPtr.Zero)
                texture=archerIdleTexture;

            if(texture==IntPtr.Zero)
                continue;

            uint format=0;
            int access=0;
            int texW=0;
            int texH=0;

            sdl.QueryTexture((Texture*)texture,&format,&access,&texW,&texH);

            int frameCount=t.IsAttacking[i]?6:1;
            int frameW=texW/frameCount;
            int frameH=texH;

            int frame=t.IsAttacking[i]?t.AnimFrames[i]:0;

            if(frame>=frameCount)
                frame=0;

            var src=new SdlRect(frame*frameW,0,frameW,frameH);

            DrawArcherAtTower(sdl,r,texture,src,pos[i].x,pos[i].y,t.FlipArchers[i]);
        }
    }
}
        static unsafe void DrawArcherAtTower(Sdl sdl,Renderer* r,IntPtr texture,SdlRect src,int x,int y,bool flip)
{
    var dst=new SdlRect(x,y,44,44);
    var spriteFlip=flip?RendererFlip.FlipHorizontal:RendererFlip.None;

    sdl.RenderCopyEx(r,(Texture*)texture,&src,&dst,0,null,spriteFlip);
}
        static unsafe void DrawShop(Sdl sdl,Renderer* r)
        {
            if(shopTexture==IntPtr.Zero)
                return;

            var dst=new SdlRect(0,520,1280,250);
            sdl.RenderCopy(r,(Texture*)shopTexture,null,&dst);

            int[] lefts={35,110,175,245,304,375,440,510};

            for(int i=0;i<lefts.Length;i++)
            {
                int sx=lefts[i]+5;
                int sy=100;
                int sw=46;
                int sh=145;

                int x=(int)(sx*1280f/797f);
                int y=520+(int)(sy*250f/313f);
                int w=(int)(sw*1280f/797f);
                int h=(int)(sh*250f/313f);

                if(gold<costs[i])
                    DrawFilledBox(sdl,r,x,y,w,h,80,80,80,130);
            }

            int sourceX=lefts[selectedShopItem-1]+5;
            int sourceY=100;
            int sourceW=46;
            int sourceH=145;

            int boxX=(int)(sourceX*1280f/797f);
            int boxY=520+(int)(sourceY*250f/313f);
            int boxW=(int)(sourceW*1280f/797f);
            int boxH=(int)(sourceH*250f/313f);

            DrawBorder(sdl,r,boxX,boxY,boxW,boxH,255,210,80);
            DrawBorder(sdl,r,boxX+2,boxY+2,boxW-4,boxH-4,255,210,80);

            DrawNumberCentered(sdl,r,gold,1000,655,2);
            DrawNumberCentered(sdl,r,(int)gameTime,1160,655,2);

            if(inPrep)
                DrawNumberCentered(sdl,r,(int)prepTime,1150,575,1);
            else
                DrawNumberCentered(sdl,r,0,1150,575,1);
        }

        static unsafe void DrawNumberCentered(Sdl sdl,Renderer* r,int value,int centerX,int y,int scale)
        {
            string text=value.ToString();

            int digitWidth=9*scale;
            int totalWidth=text.Length*digitWidth;

            int startX=centerX-totalWidth/2;

            int offset=0;
            foreach(char c in text)
            {
                DrawDigit(sdl,r,c-'0',startX+offset,y,scale);
                offset+=digitWidth;
            }
        }

        static unsafe void DrawDigit(Sdl sdl,Renderer* r,int d,int x,int y,int scale)
        {
            int[,] segments={
                {1,1,1,1,1,1,0},
                {0,1,1,0,0,0,0},
                {1,1,0,1,1,0,1},
                {1,1,1,1,0,0,1},
                {0,1,1,0,0,1,1},
                {1,0,1,1,0,1,1},
                {1,0,1,1,1,1,1},
                {1,1,1,0,0,0,0},
                {1,1,1,1,1,1,1},
                {1,1,1,1,0,1,1}
            };

            int t=scale;
            int w=6*scale;
            int h=8*scale;

            if(segments[d,0]==1) DrawFilledBox(sdl,r,x,y,w,t,255,220,120,255);
            if(segments[d,1]==1) DrawFilledBox(sdl,r,x+w,y,t,h,255,220,120,255);
            if(segments[d,2]==1) DrawFilledBox(sdl,r,x+w,y+h,t,h,255,220,120,255);
            if(segments[d,3]==1) DrawFilledBox(sdl,r,x,y+h*2,w,t,255,220,120,255);
            if(segments[d,4]==1) DrawFilledBox(sdl,r,x-t,y+h,t,h,255,220,120,255);
            if(segments[d,5]==1) DrawFilledBox(sdl,r,x-t,y,t,h,255,220,120,255);
            if(segments[d,6]==1) DrawFilledBox(sdl,r,x,y+h,w,t,255,220,120,255);
        }

        static unsafe void DrawPauseButton(Sdl sdl,Renderer* r)
        {
            DrawFilledBox(sdl,r,1210,15,55,45,30,35,45,220);
            DrawBorder(sdl,r,1210,15,55,45,210,170,80);
            DrawFilledBox(sdl,r,1228,25,7,25,220,180,90,255);
            DrawFilledBox(sdl,r,1242,25,7,25,220,180,90,255);
        }

        static unsafe void DrawOverlay(Sdl sdl,Renderer* r)
        {
            DrawFilledBox(sdl,r,0,0,LogicalW,LogicalH,0,0,0,170);
        }

        static unsafe void DrawTextureFit(Sdl sdl,Renderer* r,IntPtr texture,int texW,int texH,int percent=100)
        {
            if(texture==IntPtr.Zero)
                return;

            Area area=FitArea(texW,texH,percent);
            var dst=new SdlRect(area.X,area.Y,area.W,area.H);
            sdl.RenderCopy(r,(Texture*)texture,null,&dst);
        }

        static unsafe void DrawTextureFill(Sdl sdl,Renderer* r,IntPtr texture,int texW,int texH)
        {
            if(texture==IntPtr.Zero)
                return;

            float scale=MathF.Max(LogicalW/(float)texW,LogicalH/(float)texH);
            int w=(int)(texW*scale);
            int h=(int)(texH*scale);

            var dst=new SdlRect((LogicalW-w)/2,(LogicalH-h)/2,w,h);
            sdl.RenderCopy(r,(Texture*)texture,null,&dst);
        }

        static Area FitArea(int texW,int texH,int percent=100)
        {
            float maxW=LogicalW*percent/100f;
            float maxH=LogicalH*percent/100f;
            float scale=MathF.Min(maxW/texW,maxH/texH);

            int w=(int)(texW*scale);
            int h=(int)(texH*scale);

            return new Area((LogicalW-w)/2,(LogicalH-h)/2,w,h);
        }

        static (int x,int y) PointToTextureSource(IntPtr texture,int x,int y,int texW,int texH,Area dst)
        {
            if(texture==IntPtr.Zero || x<dst.X || x>dst.X+dst.W || y<dst.Y || y>dst.Y+dst.H)
                return (-1,-1);

            int sx=(int)((x-dst.X)*texW/(float)dst.W);
            int sy=(int)((y-dst.Y)*texH/(float)dst.H);

            return (sx,sy);
        }

        static unsafe (int x,int y) MouseToLogical(Sdl sdl,int mouseX,int mouseY)
        {
            int windowW=LogicalW;
            int windowH=LogicalH;

            sdl.GetWindowSize((Window*)window,&windowW,&windowH);

            float scale=MathF.Min(windowW/(float)LogicalW,windowH/(float)LogicalH);
            float viewW=LogicalW*scale;
            float viewH=LogicalH*scale;
            float viewX=(windowW-viewW)/2f;
            float viewY=(windowH-viewH)/2f;

            int x=(int)((mouseX-viewX)/scale);
            int y=(int)((mouseY-viewY)/scale);

            return (x,y);
        }

        static bool IsInside(int x,int y,(int x,int y,int w,int h) b)
        {
            return x>=b.x && x<=b.x+b.w && y>=b.y && y<=b.y+b.h;
        }

        static unsafe void DrawFilledBox(Sdl sdl,Renderer* r,int x,int y,int w,int h,byte red,byte green,byte blue,byte alpha)
        {
            sdl.SetRenderDrawBlendMode(r,BlendMode.Blend);
            sdl.SetRenderDrawColor(r,red,green,blue,alpha);

            var rect=new SdlRect(x,y,w,h);
            sdl.RenderFillRect(r,&rect);
        }

        static unsafe void DrawBorder(Sdl sdl,Renderer* r,int x,int y,int w,int h,byte red,byte green,byte blue)
        {
            sdl.SetRenderDrawColor(r,red,green,blue,255);

            var rect=new SdlRect(x,y,w,h);
            sdl.RenderDrawRect(r,&rect);

            rect=new SdlRect(x+1,y+1,w-2,h-2);
            sdl.RenderDrawRect(r,&rect);
        }

        static unsafe void DrawCircle(Sdl sdl,Renderer* r,int cx,int cy,int radius,byte red,byte green,byte blue,byte alpha)
        {
            sdl.SetRenderDrawBlendMode(r,BlendMode.Blend);
            sdl.SetRenderDrawColor(r,red,green,blue,alpha);

            for(int y=-radius;y<=radius;y++)
            {
                for(int x=-radius;x<=radius;x++)
                {
                    if(x*x+y*y<=radius*radius)
                        sdl.RenderDrawPoint(r,cx+x,cy+y);
                }
            }
        }

        static unsafe void DrawCircleOutline(Sdl sdl,Renderer* r,int cx,int cy,int radius,byte red,byte green,byte blue,byte alpha)
        {
            sdl.SetRenderDrawBlendMode(r,BlendMode.Blend);
            sdl.SetRenderDrawColor(r,red,green,blue,alpha);

            for(int a=0;a<360;a++)
            {
                float rad=a*MathF.PI/180f;
                int x=cx+(int)(MathF.Cos(rad)*radius);
                int y=cy+(int)(MathF.Sin(rad)*radius);
                sdl.RenderDrawPoint(r,x,y);
            }
        }

        static unsafe void DrawThickLine(Sdl sdl,Renderer* r,int x1,int y1,int x2,int y2,int thickness)
        {
            for(int dx=-thickness;dx<=thickness;dx++)
            {
                for(int dy=-thickness;dy<=thickness;dy++)
                {
                    if(dx*dx+dy*dy<=thickness*thickness)
                        sdl.RenderDrawLine(r,x1+dx,y1+dy,x2+dx,y2+dy);
                }
            }
        }

        static unsafe Texture* LoadTexture(Sdl sdl,Renderer* r,string path)
        {
            if(!File.Exists(path))
            {
                Console.WriteLine("Missing file: "+path);
                return null;
            }

            using var image=Image.Load<Rgba32>(path);

            int width=image.Width;
            int height=image.Height;

            Texture* texture=sdl.CreateTexture(r,372645892,1,width,height);

            if(texture==null)
                return null;

            byte[] pixels=new byte[width*height*4];

            for(int y=0;y<height;y++)
            {
                for(int x=0;x<width;x++)
                {
                    Rgba32 p=image[x,y];
                    int i=(y*width+x)*4;

                    pixels[i]=p.B;
                    pixels[i+1]=p.G;
                    pixels[i+2]=p.R;
                    pixels[i+3]=p.A;
                }
            }

            fixed(byte* p=pixels)
                sdl.UpdateTexture(texture,null,p,width*4);

            sdl.SetTextureBlendMode(texture,BlendMode.Blend);

            return texture;
        }

        static unsafe void DestroyTexture(Sdl sdl,IntPtr texture)
        {
            if(texture!=IntPtr.Zero)
                sdl.DestroyTexture((Texture*)texture);
        }
    }