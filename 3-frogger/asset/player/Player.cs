using Godot;
using System;

public partial class Player : StaticBody2D
{
	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Sprite2D body;
	public AnimationPlayer animationPlayer;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Node2D>("Sprite");
		body = sprite.GetNode<Sprite2D>("Body");
		animationPlayer = sprite.GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void Setup(Vector2 newSize)
	{
		shape.Size = newSize;
		//var newScale = shape.Size / sprite.Texture.GetSize();
		//sprite.Scale = newScale;
		sprite.Position = new Vector2(
			collisionShape.Position.X/*  - shape.Size.X / 2f */,
			collisionShape.Position.Y/*  - shape.Size.Y / 2f */
		);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
