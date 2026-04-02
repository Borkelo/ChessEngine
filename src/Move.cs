public struct Move
{
    public Coordinate From { get; }
    public Coordinate To { get; }
    
    public Move(Coordinate from, Coordinate to)
    {
        From = from;
        To = to;      
    }
    
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