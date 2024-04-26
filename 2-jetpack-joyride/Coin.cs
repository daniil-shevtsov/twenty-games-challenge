using Godot;
using System;

public partial class Coin : Area2D
{
	public CollisionShape2D collisionShape = null;
	public RectangleShape2D shape = null;
	public Sprite2D sprite = null;

	private float rotationSpeed = 0f;
	private int rotationDirection = 1;

	public void setConfig(float rotationSpeed, int rotationDirection)
	{
		this.rotationSpeed = rotationSpeed * 2;
		this.rotationDirection = rotationDirection;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
		sprite = GetNode<Sprite2D>("Sprite2D");
	}

	public void RotateBy(float angle)
	{
		sprite.RotationDegrees += angle * rotationSpeed * rotationDirection;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
