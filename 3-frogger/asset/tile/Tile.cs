using Godot;
using System.Drawing;
using Color = Godot.Color;

public partial class Tile : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public ColorRect background;
	public TileKey key;
	public TileType tileType;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		background = GetNode<ColorRect>("ColorRect");
	}

	public void Setup(Vector2 newSize, TileKey key)
	{
		this.key = key;
		shape.Size = newSize;
		background.Size = shape.Size;
		background.Position = new Vector2(
			collisionShape.Position.X - background.Size.X / 2f,
			collisionShape.Position.Y - background.Size.Y / 2f
		);
		UpdateType(TileType.Ground);
	}

	public void UpdateType(TileType newType)
	{
		tileType = newType;

		var newColor = background.Color;
		switch (newType)
		{
			case TileType.Ground:
				Color color;
				if (key.X % 2 == key.Y % 2)
				{
					color = Color.FromHtml("#FFFFFF");
				}
				else
				{
					color = Color.FromHtml("#000000");
				}
				UpdateColor(color);
				break;
			case TileType.Water:
				UpdateColor(Godot.Color.FromHtml("#0000FF"));
				break;
		}
	}

	public void UpdateColor(Godot.Color newColor)
	{
		background.Color = newColor;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
