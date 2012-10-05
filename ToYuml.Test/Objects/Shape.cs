using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToYuml.Test.Objects
{
	public interface IShape
	{
		void Draw();
	}

	public class Rhombus : IShape
	{
		public void Draw() { }
	}

	public class Square : Rhombus
	{
	}

	// interface inheritance
	public interface IRoundShape : IShape
	{
		float Radius();
	}

	public class Ellipse : IRoundShape
	{
		public void Draw() { } // IShape
		public float Radius() { return 1.0f; } // IRoundShape
	}

	public class Circle : Ellipse { }

}
