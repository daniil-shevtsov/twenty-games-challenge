using Godot;
using System;

public partial class PlayerSprite : Node2D
{
	private Vector2 rectSize;

	public void setRectSize(Vector2 size)
	{
		rectSize = size;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.

	public override void _Process(double delta)
	{
		this.QueueRedraw();
	}

	public override void _Draw()
	{
		var center = Vector2.Zero;
		float radius = rectSize.X / 2;
		var color = new Color(1, 1, 1);
		DrawCircle(center, radius, color);

		DrawCircle(center - new Vector2(0, rectSize.Y / 2), radius, color);
		DrawRect(new Rect2(center - rectSize / 2, rectSize), color);
		DrawCircle(center + new Vector2(0, rectSize.Y / 2), radius, color);
	}
}
