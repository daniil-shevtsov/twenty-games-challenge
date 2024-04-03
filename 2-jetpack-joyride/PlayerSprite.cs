using Godot;
using System;

public partial class PlayerSprite : Node2D
{
	private float drawRadius;

	public void setRadius(float radius)
	{
		drawRadius = radius;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public override void _Process(double delta)
	{
		this.QueueRedraw();
	}

	public override void _Draw()
	{
		var center = Vector2.Zero;
		float radius = drawRadius;
		var color = new Color(1, 1, 1);
		DrawCircle(center, radius, color);
	}
}
