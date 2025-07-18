public readonly struct Move(Coordinate from, Coordinate to)
{
    public Coordinate From { get; } = from;
    public Coordinate To { get; } = to;

    public override readonly string ToString() => $"{From} > {To}";
    
    public static bool operator ==(Move a, Move b)
    {
        return a.From == b.From && a.To == b.To;
    }

    public static bool operator !=(Move a, Move b)
    {
        return !(a == b);
    }
    
    public override readonly bool Equals(object? obj)
    {
        return obj is Move other && this == other;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(From, To);
    }
}