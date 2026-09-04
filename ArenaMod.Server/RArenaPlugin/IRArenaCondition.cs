namespace ArenaMod.Server.RArenaPlugin
{
    public interface IRArenaCondition
    {
        public int Priority { get; set; }
        public abstract void OnUpdate(float tick);
        public abstract bool WasFullfilled();
        public abstract void Reset();
    }
}
