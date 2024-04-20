using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public CollisionShape2D collisionShape = null;
	public CapsuleShape2D shape = null;
	public PlayerSprite sprite = null;
	public Node2D headContainer = null;
	public Node2D legBody = null;
	public Node2D legBodyHead = null;
	public Node2D wheelContainer = null;
	public Sprite2D wheel = null;
	private float drawRadius;

	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("PlayerCollider");
		shape = (CapsuleShape2D)collisionShape.Shape;

		sprite = GetNode<PlayerSprite>("PlayerSprite");
		drawRadius = shape.Radius;
		sprite.setRectSize(new Vector2(shape.Radius, shape.Height));
		var spriteContainer = GetNode<Node2D>("PlayerSpriteContainer");
		legBodyHead = spriteContainer.GetNode<Node2D>("LegBodyHead");
		wheelContainer = spriteContainer.GetNode<Node2D>("WheelContainer");
		wheel = wheelContainer.GetNode<Sprite2D>("Wheel");

		legBody = legBodyHead.GetNode<Node2D>("LegBody");
		headContainer = legBodyHead.GetNode<Node2D>("HeadContainer");
	}

	public override void _PhysicsProcess(double delta)
	{

	}
}
