using System;

public readonly struct TileKey
{
	public readonly int X;
	public readonly int Y;

	public TileKey(int x, int y)
	{
		X = x;
		Y = y;
	}

	public TileKey Copy(int newX = -1, int newY = -1)
	{
		if (newX == -1)
		{
			newX = X;
		}
		if (newY == -1)
		{
			newY = Y;
		}

		return new TileKey(newX, newY);
	}

	public override String ToString()
	{
		return $"{(X, Y)}";
	}

	public static bool operator ==(TileKey c1, TileKey c2)
	{
		return c1.Equals(c2);
	}

	public static bool operator !=(TileKey c1, TileKey c2)
	{
		return !c1.Equals(c2);
	}
}