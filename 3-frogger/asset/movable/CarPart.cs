using Godot;
using System;

public partial class CarPart : StaticBody2D
{

	[Signal]
	public delegate void HealthDepletedEventHandler(float amount);

	private CollisionShape2D collisionShape;
	public RectangleShape2D shape;
	public Node2D sprite;
	public Node2D container;
	public Sprite2D bodySprite;
	public AnimationPlayer animationPlayer;

	private Tween tween;

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
		var scaledAmount = amount * sprite.Scale.X * 2 * speedMultiplier;
		GD.Print($"Car-{id} Move {scaledAmount}");
		tween = CreateTween();
		var newPosition = new Vector2(
			GlobalPosition.X - scaledAmount,
			GlobalPosition.Y
		);
		tween.TweenProperty(this, "global_position", newPosition, 0.2f);
	}
}
