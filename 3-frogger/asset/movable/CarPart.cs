using Godot;
using System;

public partial class CarPart : StaticBody2D
{

	[Signal]
	public delegate void HealthDepletedEventHandler(float amount);

	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Sprite2D bodySprite;

	public long id;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape.Duplicate();
		collisionShape.Shape = shape;
		sprite = GetNode<Node2D>("Sprite2D");
		bodySprite = sprite.GetNode<Sprite2D>("Body");
	}

	public void Setup(long id)
	{
		this.id = id;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void HandleHealthDepletedEventHandler(float amount)
	{

	}
}
