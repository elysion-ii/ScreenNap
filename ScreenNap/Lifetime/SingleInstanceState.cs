namespace ScreenNap.Lifetime;

// What the single-instance check found (specification LIFE-2)
internal enum SingleInstanceState
{
    Acquired,
    // Another ScreenNap of this Windows session holds the lock, possibly with other
    // privileges that keep this one from opening it
    HeldByAnotherInstance,
    CheckFailed,
}
