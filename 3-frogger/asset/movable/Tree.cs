using Godot;
using System;

public partial class Tree : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Sprite2D sprite;

	public long id;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public void Setup(Vector2 newSize, long id)
	{
		shape.Size = newSize;
		var newScale = shape.Size / sprite.Texture.GetSize();
		sprite.Scale = newScale;
		sprite.Position = new Vector2(
			collisionShape.Position.X,
			collisionShape.Position.Y
		);

		this.id = id;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
