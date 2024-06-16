using Godot;
using System;

public partial class CarPart : StaticBody2D
{

	[Signal]
	public delegate void HealthDepletedEventHandler(float amount);


	public delegate void CarMoveEventHandler(float scaledAmount);
	public event CarMoveEventHandler CarMoved;

	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Node2D container;
	public Sprite2D bodySprite;
	public AnimationPlayer animationPlayer;


	public long id;
	public float speedMultiplier;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		shape = (RectangleShape2D)collisionShape.Shape.Duplicate();
		collisionShape.Shape = shape;
		sprite = GetNode<Node2D>("Sprite2D");
		container = sprite.GetNode<Node2D>("Container");
		bodySprite = container.GetNode<Sprite2D>("Body");
		animationPlayer = sprite.GetNode<AnimationPlayer>("AnimationPlayer");
	}

	public void Setup(long id, float speedMultiplier)
	{
		this.id = id;
		this.speedMultiplier = speedMultiplier;

		animationPlayer.Play("car_part_walk");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async void HandleHealthDepletedEventHandler(float amount)
	{
		var scaledAmount = amount * sprite.Scale.X * 4 * speedMultiplier;
		CarMoved.Invoke(scaledAmount);
	}
}
