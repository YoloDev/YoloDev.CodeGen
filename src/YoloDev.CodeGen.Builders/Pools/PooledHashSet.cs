using System.Diagnostics;

namespace YoloDev.CodeGen.Builders.Pools;

// HashSet that can be recycled via an object pool
// NOTE: these HashSets always have the default comparer.
sealed class PooledHashSet<T> : HashSet<T>
{
    readonly ObjectPool<PooledHashSet<T>> _pool;

    PooledHashSet(ObjectPool<PooledHashSet<T>> pool, IEqualityComparer<T> equalityComparer) :
        base(equalityComparer)
    {
        _pool = pool;
    }

    public void Free()
    {
        Clear();
        _pool?.Free(this);
    }

    // global pool
    static readonly ObjectPool<PooledHashSet<T>> PoolInstance = CreatePool(EqualityComparer<T>.Default);

    // if someone needs to create a pool;
    public static ObjectPool<PooledHashSet<T>> CreatePool(IEqualityComparer<T> equalityComparer)
    {
        ObjectPool<PooledHashSet<T>>? pool = null;
        pool = new ObjectPool<PooledHashSet<T>>(() => new PooledHashSet<T>(pool!, equalityComparer), 128);
        return pool;
    }

    public static PooledHashSet<T> GetInstance()
    {
        var instance = PoolInstance.Allocate();
        Debug.Assert(instance.Count == 0);
        return instance;
    }
}
