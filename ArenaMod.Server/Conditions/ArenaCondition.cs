namespace ArenaMod.Server.Conditions;

public abstract class ArenaCondition
{
    public int Priority { get; set; }
    public abstract void OnUpdate();
    public abstract bool WasFullfilled();
    public abstract void Reset();
}