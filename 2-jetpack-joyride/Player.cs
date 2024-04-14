using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public CollisionShape2D collisionShape = null;
	public CapsuleShape2D shape = null;
	public PlayerSprite sprite = null;
	public Node2D headContainer = null;
	private float drawRadius;

	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("PlayerCollider");
		shape = (CapsuleShape2D)collisionShape.Shape;

		sprite = GetNode<PlayerSprite>("PlayerSprite");
		drawRadius = shape.Radius;
		sprite.setRectSize(new Vector2(shape.Radius, shape.Height));
		headContainer = GetNode<Node2D>("PlayerSpriteContainer").GetNode<Node2D>("LegBody").GetNode<Node2D>("HeadContainer");
	}

	public override void _PhysicsProcess(double delta)
	{

	}
}
