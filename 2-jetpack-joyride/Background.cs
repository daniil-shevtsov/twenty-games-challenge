using Godot;
using System;

public partial class Background : Node2D
{

	public Sprite2D main;
	public Sprite2D backup;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		main = GetNode<Sprite2D>("Main");
		backup = GetNode<Sprite2D>("Backup");
	}

	public void MoveBy(float amount)
	{
		main.GlobalPosition = new Vector2(main.GlobalPosition.X + amount, main.GlobalPosition.Y);
		backup.GlobalPosition = new Vector2(backup.GlobalPosition.X + amount, backup.GlobalPosition.Y);
	}

	public void Swap()
	{
		var backupPosition = backup.GlobalPosition;
		backup.GlobalPosition = main.GlobalPosition;
		main.GlobalPosition = backupPosition;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
