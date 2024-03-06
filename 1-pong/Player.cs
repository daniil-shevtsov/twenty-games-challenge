using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// public RigidBody2D body;
	public CollisionShape2D collisionShape = null;
	public RectangleShape2D shape = null;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
