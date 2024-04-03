using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public CollisionShape2D collisionShape = null;
	public CapsuleShape2D shape = null;
	public PlayerSprite sprite = null;
	private float drawRadius;

	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("PlayerCollider");
		shape = (CapsuleShape2D)collisionShape.Shape;

		sprite = GetNode<PlayerSprite>("PlayerSprite");
		drawRadius = shape.Radius;
		sprite.setRectSize(new Vector2(shape.Radius, shape.Height));
	}

	public override void _PhysicsProcess(double delta)
	{

	}
}
