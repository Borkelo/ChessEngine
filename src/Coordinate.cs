public readonly struct Coordinate
{
    public int Row { get; }
    public int Col { get; }
    
    public Coordinate(int col, int row)
    {
        Col = col;
        Row = row;      
    }
    
    public Coordinate(string notation)
    {
        if (notation.Length != 2)
            throw new Exception("Invalid coordinate: " + notation);

        Col = notation[0] - 'a';
        Row = notation[1] - '1'; 
    }
    
    public override readonly string ToString() => $"{(char)('a' + Col)}{Row + 1}";
    
    public static bool operator ==(Coordinate a, Coordinate b)
    {
        return a.Col == b.Col && a.Row == b.Row;
    }

    public static bool operator !=(Coordinate a, Coordinate b)
    {
        return !(a == b);
    }
    
    public static Coordinate operator +(Coordinate a, Coordinate b)
    {
        return new(a.Col + b.Col, a.Row + b.Row);
    }
    
    public static Coordinate operator -(Coordinate a, Coordinate b)
    {
        return new(a.Col - b.Col, a.Row - b.Row);
    }
    
    public static Coordinate operator *(Coordinate a, int num)
    {
        return new(a.Col * num, a.Row * num);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Coordinate other && this == other;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Col, Row);
    }
}