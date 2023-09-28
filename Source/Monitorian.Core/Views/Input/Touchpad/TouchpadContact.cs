using System;
using System.Windows;

namespace Monitorian.Core.Views.Input.Touchpad;

internal readonly struct TouchpadContact : IEquatable<TouchpadContact>
{
	public int ContactId { get; }
	public int X { get; }
	public int Y { get; }

	public Point Point => new(X, Y);

	public TouchpadContact(int contactId, int x, int y) =>
		(this.ContactId, this.X, this.Y) = (contactId, x, y);

	public override bool Equals(object obj) => (obj is TouchpadContact other) && Equals(other);

	public bool Equals(TouchpadContact other) =>
		(this.ContactId == other.ContactId) && (this.X == other.X) && (this.Y == other.Y);

	public static bool operator ==(TouchpadContact a, TouchpadContact b) => a.Equals(b);
	public static bool operator !=(TouchpadContact a, TouchpadContact b) => !a.Equals(b);

	public override int GetHashCode() => (this.ContactId, this.X, this.Y).GetHashCode();

	public override string ToString() => $"Contact ID:{ContactId} Point:{X},{Y}";
}

internal class TouchpadContactCreator
{
	public int? ContactId { get; set; }
	public int? X { get; set; }
	public int? Y { get; set; }

	public bool TryCreate(out TouchpadContact contact)
	{
		if (ContactId.HasValue && X.HasValue && Y.HasValue)
		{
			contact = new TouchpadContact(ContactId.Value, X.Value, Y.Value);
			return true;
		}
		contact = default;
		return false;
	}

	public void Clear()
	{
		ContactId = null;
		X = null;
		Y = null;
	}
}