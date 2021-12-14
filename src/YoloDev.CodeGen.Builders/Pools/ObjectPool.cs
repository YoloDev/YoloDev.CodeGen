// define TRACE_LEAKS to get additional diagnostics that can lead to the leak sources. note: it will
// make everything about 2-3x slower
// 
// #define TRACE_LEAKS

// define DETECT_LEAKS to detect possible leaks
#if DEBUG
#define DETECT_LEAKS  //for now always enable DETECT_LEAKS in debug.
#endif

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

#if DETECT_LEAKS
using System.Runtime.CompilerServices;
#endif

namespace YoloDev.CodeGen.Builders.Pools;

/// <summary>
/// <para>
/// Generic implementation of object pooling pattern with predefined pool size limit. The main
/// purpose is that limited number of frequently used objects can be kept in the pool for
/// further recycling.
/// </para>
/// <para>
/// Notes:
/// <list type="number">
/// <item><description>it is not the goal to keep all returned objects. Pool is not meant for storage. If there
/// is no space in the pool, extra returned objects will be dropped.</description></item>
/// <item><description>it is implied that if object was obtained from a pool, the caller will return it back in
/// a relatively short time. Keeping checked out objects for long durations is ok, but
/// reduces usefulness of pooling. Just new up your own.</description></item>
/// </list>
/// </para>
/// <para>
/// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice.
/// Rationale:
///    If there is no intent for reusing the object, do not use pool - just use "new".
/// </para>
/// </summary>
/// <typeparam name="T"></typeparam>
class ObjectPool<T> where T : class
{
    [DebuggerDisplay("{Value,nq}")]
    struct Element
    {
        internal T? Value;
    }

    internal delegate T Factory();

    // Storage for the pool objects. The first item is stored in a dedicated field because we
    // expect to be able to satisfy most requests from it.
    T? _firstItem;
    readonly Element[] _items;

    // factory is stored for the lifetime of the pool. We will call this only when pool needs to
    // expand. compared to "new T()", Func gives more flexibility to implementers and faster
    // than "new T()".
    readonly Factory _factory;

#if DETECT_LEAKS
    static readonly ConditionalWeakTable<T, LeakTracker> LeakTrackers = new();

    class LeakTracker : IDisposable
    {
        volatile bool _disposed;

#if TRACE_LEAKS
        internal volatile object Trace = null;
#endif

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        [SuppressMessage("Style", "IDE0022:Use expression body for methods", Justification = "Method has compiler-conditionals inside.")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method has compiler-conditionals inside.")]
        string GetTrace()
        {
#if TRACE_LEAKS
            return Trace == null ? "" : Trace.ToString();
#else
            return "Leak tracing information is disabled. Define TRACE_LEAKS on ObjectPool`1.cs to get more info \n";
#endif
        }

        ~LeakTracker()
        {
            if (!_disposed && !Environment.HasShutdownStarted)
            {
                var trace = GetTrace();

                // If you are seeing this message it means that object has been allocated from the pool 
                // and has not been returned back. This is not critical, but turns pool into rather 
                // inefficient kind of "new".
                Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nPool detected potential leaking of {typeof(T)}. \n Location of the leak: \n {trace} TRACEOBJECTPOOLLEAKS_END");
            }
        }
    }
#endif

    internal ObjectPool(Factory factory)
        : this(factory, Environment.ProcessorCount * 2)
    { }

    internal ObjectPool(Factory factory, int size)
    {
        Debug.Assert(size >= 1);
        _factory = factory;
        _items = new Element[size - 1];
    }

    internal ObjectPool(Func<ObjectPool<T>, T> factory, int size)
    {
        Debug.Assert(size >= 1);
        _factory = () => factory(this);
        _items = new Element[size - 1];
    }

    T CreateInstance()
    {
        var inst = _factory();
        return inst;
    }

    /// <summary>
    /// Produces an instance.
    /// </summary>
    /// <remarks>
    /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
    /// Note that Free will try to store recycled objects close to the start thus statistically
    /// reducing how far we will typically search.
    /// </remarks>
    internal T Allocate()
    {
        // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
        // Note that the initial read is optimistically not synchronized. That is intentional. 
        // We will interlock only when we have a candidate. in a worst case we may miss some
        // recently returned objects. Not a big deal.
        var inst = _firstItem;
        if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
        {
            inst = AllocateSlow();
        }

#if DETECT_LEAKS
        var tracker = new LeakTracker();
        LeakTrackers.Add(inst, tracker);

#if TRACE_LEAKS
        var frame = CaptureStackTrace();
        tracker.Trace = frame;
#endif
#endif
        return inst;
    }

    T AllocateSlow()
    {
        var items = _items;

        for (var i = 0; i < items.Length; i++)
        {
            // Note that the initial read is optimistically not synchronized. That is intentional. 
            // We will interlock only when we have a candidate. in a worst case we may miss some
            // recently returned objects. Not a big deal.
            var inst = items[i].Value;
            if (inst != null)
            {
                if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                {
                    return inst;
                }
            }
        }

        return CreateInstance();
    }

    /// <summary>
    /// Returns objects to the pool.
    /// </summary>
    /// <param name="obj">The object to free.</param>
    /// <remarks>
    /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
    /// Note that Free will try to store recycled objects close to the start thus statistically
    /// reducing how far we will typically search in Allocate.
    /// </remarks>
    internal void Free(T obj)
    {
        Validate(obj);
        ForgetTrackedObject(obj);

        if (_firstItem == null)
        {
            // Intentionally not using interlocked here. 
            // In a worst case scenario two objects may be stored into same slot.
            // It is very unlikely to happen and will only mean that one of the objects will get collected.
            _firstItem = obj;
        }
        else
        {
            FreeSlow(obj);
        }
    }

    void FreeSlow(T obj)
    {
        var items = _items;
        for (var i = 0; i < items.Length; i++)
        {
            if (items[i].Value == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                items[i].Value = obj;
                break;
            }
        }
    }

    /// <summary>
    /// <para>Removes an object from leak tracking.</para>
    /// <para>
    /// This is called when an object is returned to the pool. It may also be explicitly
    /// called if an object allocated from the pool is intentionally not being returned
    /// to the pool.  This can be of use with pooled arrays if the consumer wants to
    /// return a larger array to the pool than was originally allocated.
    /// </para>
    /// </summary>
    /// <param name="old">The old object to forget.</param>
    /// <param name="replacement">An optional replacement object.</param>
    [Conditional("DEBUG")]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Method cannot be statick for all compile targets.")]
    internal void ForgetTrackedObject(T old, T? replacement = null)
    {
#if DETECT_LEAKS
        if (LeakTrackers.TryGetValue(old, out var tracker))
        {
            tracker.Dispose();
            LeakTrackers.Remove(old);
        }
        else
        {
            var trace = CaptureStackTrace();
            Debug.WriteLine($"TRACEOBJECTPOOLLEAKS_BEGIN\nObject of type {typeof(T)} was freed, but was not from pool. \n Callstack: \n {trace} TRACEOBJECTPOOLLEAKS_END");
        }

        if (replacement != null)
        {
            tracker = new LeakTracker();
            LeakTrackers.Add(replacement, tracker);
        }
#endif
    }

#if DETECT_LEAKS
    static readonly Lazy<Type> StackTraceType = new(() => Type.GetType("System.Diagnostics.StackTrace")!);

    static object CaptureStackTrace() => Activator.CreateInstance(StackTraceType.Value)!;
#endif

    [Conditional("DEBUG")]
    void Validate(object obj)
    {
        Debug.Assert(obj != null, "freeing null?");

        Debug.Assert(_firstItem != obj, "freeing twice?");

        var items = _items;
        for (var i = 0; i < items.Length; i++)
        {
            var value = items[i].Value;
            if (value == null)
            {
                return;
            }

            Debug.Assert(value != obj, "freeing twice?");
        }
    }
}
