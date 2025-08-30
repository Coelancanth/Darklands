# SPD Combat System - Direct Code Extraction for Darklands

**Created**: 2025-08-30
**Purpose**: Specific patterns to port directly to Darklands

## 🎯 Priority 1: Time-Unit System

### SPD Pattern (Actor.java)
```java
public abstract class Actor {
    public static final float TICK = 1f;
    private float time;
    
    protected void spend(float time) {
        this.time += time;
    }
    
    protected void postpone(float time) {
        if (this.time < now + time) {
            this.time = now + time;
        }
    }
    
    public float cooldown() {
        return time - now;
    }
}
```

### Darklands Implementation (C#)
```csharp
public abstract class Actor {
    public const float TICK = 1f;
    private float _time;
    
    protected void Spend(float time) {
        _time += time;
        // Round to fix floating point errors
        float ex = Math.Abs(_time % 1f);
        if (ex < .001f) {
            _time = (float)Math.Round(_time);
        }
    }
    
    protected void Postpone(float time) {
        if (_time < TimeScheduler.Now + time) {
            _time = TimeScheduler.Now + time;
        }
    }
    
    public float Cooldown => _time - TimeScheduler.Now;
}
```

## 🎯 Priority 2: Turn Processing Loop

### SPD Pattern (Actor.process())
```java
public static void process() {
    boolean doNext;
    do {
        current = null;
        float earliest = Float.MAX_VALUE;
        
        for (Actor actor : all) {
            if (actor.time < earliest ||
                actor.time == earliest && actor.actPriority > current.actPriority) {
                earliest = actor.time;
                current = actor;
            }
        }
        
        if (current != null) {
            now = current.time;
            doNext = current.act();
        } else {
            doNext = false;
        }
    } while (doNext);
}
```

### Darklands Implementation (C#)
```csharp
public class TimeScheduler {
    private HashSet<Actor> _actors = new();
    public static float Now { get; private set; }
    
    public void Process() {
        while (true) {
            var next = FindNextActor();
            if (next == null) break;
            
            Now = next.Time;
            bool continues = next.Act();
            
            if (!continues) break;
        }
    }
    
    private Actor FindNextActor() {
        Actor earliest = null;
        float earliestTime = float.MaxValue;
        
        foreach (var actor in _actors) {
            if (actor.Time < earliestTime ||
                (actor.Time == earliestTime && 
                 (earliest == null || actor.Priority > earliest.Priority))) {
                earliestTime = actor.Time;
                earliest = actor;
            }
        }
        
        return earliest;
    }
}
```

## 🎯 Priority 3: Attack System

### SPD Pattern (Char.attack())
```java
public boolean attack(Char enemy, float dmgMulti, float dmgBonus, float accMulti) {
    if (enemy == null) return false;
    
    boolean visibleFight = Dungeon.level.heroFOV[pos] || Dungeon.level.heroFOV[enemy.pos];
    
    if (enemy.isInvulnerable(getClass())) {
        spend(attackDelay());
        return false;
    }
    
    if (hit(this, enemy, accMulti, false)) {
        int damage = damageRoll();
        damage = Math.round(damage * dmgMulti);
        damage += dmgBonus;
        
        damage = enemy.defenseProc(this, damage);
        damage = attackProc(enemy, damage);
        
        enemy.damage(damage, this);
        
        spend(attackDelay());
        return true;
    } else {
        enemy.sprite.showStatus(CharSprite.NEUTRAL, enemy.defenseVerb());
        spend(attackDelay());
        return false;
    }
}
```

### Darklands Implementation (C#)
```csharp
public class AttackCommand : ICommand<Fin<Unit>> {
    public ActorId AttackerId { get; init; }
    public ActorId TargetId { get; init; }
    public float DamageMultiplier { get; init; } = 1f;
    public float DamageBonus { get; init; } = 0f;
    public float AccuracyMultiplier { get; init; } = 1f;
}

public class AttackCommandHandler : ICommandHandler<AttackCommand, Fin<Unit>> {
    public async Task<Fin<Unit>> Handle(AttackCommand request, CancellationToken ct) {
        var attacker = GetActor(request.AttackerId);
        var target = GetActor(request.TargetId);
        
        if (target.IsInvulnerable) {
            attacker.Spend(attacker.AttackDelay);
            return Fin<Unit>.Fail("Target is invulnerable");
        }
        
        if (CombatCalculator.Hit(attacker, target, request.AccuracyMultiplier)) {
            int damage = attacker.RollDamage();
            damage = (int)(damage * request.DamageMultiplier);
            damage += (int)request.DamageBonus;
            
            // Apply defense procs
            damage = await ApplyDefenseProcs(target, attacker, damage);
            damage = await ApplyAttackProcs(attacker, target, damage);
            
            // Deal damage
            await _mediator.Send(new DamageCommand {
                TargetId = request.TargetId,
                Damage = damage,
                Source = request.AttackerId
            });
            
            attacker.Spend(attacker.AttackDelay);
            return Fin<Unit>.Succ(Unit.Default);
        } else {
            // Miss
            await _mediator.Publish(new AttackMissedNotification {
                AttackerId = request.AttackerId,
                TargetId = request.TargetId
            });
            
            attacker.Spend(attacker.AttackDelay);
            return Fin<Unit>.Fail("Attack missed");
        }
    }
}
```

## 🎯 Priority 4: AI State Machine

### SPD Pattern (Mob AI States)
```java
public abstract class Mob extends Char {
    public AiState SLEEPING = new Sleeping();
    public AiState HUNTING = new Hunting();
    public AiState WANDERING = new Wandering();
    public AiState FLEEING = new Fleeing();
    public AiState state = SLEEPING;
    
    protected class Hunting implements AiState {
        @Override
        public boolean act(boolean enemyInFOV, boolean justAlerted) {
            if (enemyInFOV) {
                target = enemy.pos;
            }
            
            if (canAttack(enemy)) {
                return doAttack(enemy);
            } else {
                if (enemyInFOV) {
                    target = enemy.pos;
                } else {
                    chooseEnemy();
                    target = enemy != null ? enemy.pos : Dungeon.hero.pos;
                }
                
                return moveSprite(pos, findPath(enemy, target));
            }
        }
    }
}
```

### Darklands Implementation (C#)
```csharp
public abstract class Mob : Actor {
    public IAiState CurrentState { get; set; }
    
    protected override bool Act() {
        var enemyInFov = CanSee(Enemy);
        var justAlerted = WasJustAlerted();
        
        return CurrentState.Act(this, enemyInFov, justAlerted);
    }
}

public interface IAiState {
    bool Act(Mob mob, bool enemyInFov, bool justAlerted);
}

public class HuntingState : IAiState {
    public bool Act(Mob mob, bool enemyInFov, bool justAlerted) {
        if (enemyInFov) {
            mob.Target = mob.Enemy.Position;
        }
        
        if (mob.CanAttack(mob.Enemy)) {
            return mob.DoAttack(mob.Enemy);
        } else {
            if (!enemyInFov) {
                mob.ChooseEnemy();
                mob.Target = mob.Enemy?.Position ?? GameState.Hero.Position;
            }
            
            var path = Pathfinder.FindPath(mob.Position, mob.Target);
            return mob.Move(path.FirstOrDefault());
        }
    }
}

public class WanderingState : IAiState {
    public bool Act(Mob mob, bool enemyInFov, bool justAlerted) {
        if (enemyInFov && (justAlerted || Random.Float() < 1f)) {
            mob.CurrentState = new HuntingState();
            return mob.CurrentState.Act(mob, enemyInFov, false);
        }
        
        // Wander randomly
        var neighbors = Pathfinder.GetNeighbors(mob.Position);
        var validMoves = neighbors.Where(n => mob.CanMove(n)).ToList();
        if (validMoves.Any()) {
            var target = validMoves[Random.Int(validMoves.Count)];
            return mob.Move(target);
        }
        
        mob.Spend(Actor.TICK);
        return true;
    }
}
```

## 🎯 Priority 5: Buff System

### SPD Pattern (Buff.java)
```java
public class Buff extends Actor {
    public Char target;
    
    public boolean attachTo(Char target) {
        if (target.isImmune(getClass())) {
            return false;
        }
        
        this.target = target;
        target.add(this);
        
        return true;
    }
    
    public void detach() {
        target.remove(this);
        Actor.remove(this);
    }
    
    @Override
    protected boolean act() {
        diactivate();
        return true;
    }
    
    public static <T extends Buff> T affect(Char target, Class<T> buffClass) {
        T buff = target.buff(buffClass);
        if (buff != null) {
            return buff;
        } else {
            return append(target, buffClass);
        }
    }
}
```

### Darklands Implementation (C#)
```csharp
public abstract class Buff : Actor {
    public Actor Target { get; private set; }
    
    public virtual bool AttachTo(Actor target) {
        if (target.IsImmune(GetType())) {
            return false;
        }
        
        Target = target;
        target.AddBuff(this);
        TimeScheduler.Add(this);
        
        OnAttach();
        return true;
    }
    
    public virtual void Detach() {
        OnDetach();
        Target?.RemoveBuff(this);
        TimeScheduler.Remove(this);
    }
    
    protected virtual void OnAttach() { }
    protected virtual void OnDetach() { }
    
    public static T Affect<T>(Actor target) where T : Buff, new() {
        var existing = target.GetBuff<T>();
        if (existing != null) {
            existing.Refresh();
            return existing;
        }
        
        var buff = new T();
        buff.AttachTo(target);
        return buff;
    }
}

// Example: Poison Buff
public class Poison : Buff {
    private float _duration = 5f;
    private int _damagePerTick = 2;
    
    protected override bool Act() {
        if (Target == null) {
            Detach();
            return true;
        }
        
        Target.Damage(_damagePerTick, this);
        
        _duration -= TICK;
        if (_duration <= 0) {
            Detach();
        } else {
            Spend(TICK);
        }
        
        return true;
    }
}
```

## 📋 Implementation Order

### Phase 1: Core Time System (2 days)
1. Create Actor base class with time management
2. Implement TimeScheduler with priority queue
3. Add basic tests for turn order

### Phase 2: Combat Foundation (3 days)
1. Port attack/damage calculations
2. Implement hit/miss formulas
3. Create damage pipeline with reduction stages
4. Add combat tests

### Phase 3: AI Framework (2 days)
1. Create IAiState interface
2. Implement basic states (Hunting, Wandering, Fleeing)
3. Add state transition logic
4. Test with simple mobs

### Phase 4: Buff System (2 days)
1. Create Buff base class as Actor
2. Implement attachment/detachment
3. Create 3-5 basic buffs (Poison, Haste, Slow)
4. Add buff UI indicators

### Phase 5: Integration (1 day)
1. Connect to existing Grid system
2. Update Movement commands to use time units
3. Add combat to UI layer
4. Full integration testing

## ⚠️ Key Differences to Handle

### Threading
- SPD is single-threaded
- We need thread safety for Godot
- Use MediatR for command/event dispatch

### Grid System
- SPD uses 1D array (pos as int)
- We use 2D Position records
- Need coordinate conversion

### Save System
- SPD uses Bundle (custom serialization)
- We'll use JSON/Binary serialization
- Design for forward compatibility

## 🎯 Next Actions

1. **Create TD_002**: Migrate to float-based time units
2. **Create VS_009**: Implement TimeScheduler
3. **Create VS_010**: Port attack system
4. **Create VS_011**: Implement buff framework
5. **Update Glossary**: Add SPD terms (Actor, Buff, Tick, etc.)

---

*This extraction focuses on immediately implementable patterns with clear C# translations.*