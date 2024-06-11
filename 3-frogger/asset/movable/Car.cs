using Godot;
using System;

public partial class Car : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Sprite2D bodySprite;

	public long id;
	public float speedMultiplier;
	public int lengthInTiles;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape.Duplicate();
		collisionShape.Shape = shape;
		sprite = GetNode<Node2D>("Sprite2D");
		bodySprite = sprite.GetNode<Sprite2D>("Body");
	}

	public void Setup(int lengthInTiles, Vector2 tileSize, long id, float speedMultiplier)
	{
		var newSize = new Vector2(tileSize.X * lengthInTiles, tileSize.Y);
		shape.Size = newSize;
		var newScale = shape.Size / bodySprite.Texture.GetSize();
		sprite.Scale = newScale;
		sprite.Position = new Vector2(
			collisionShape.Position.X,
			collisionShape.Position.Y
		);

		this.id = id;
		this.speedMultiplier = speedMultiplier;
		this.lengthInTiles = lengthInTiles;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
