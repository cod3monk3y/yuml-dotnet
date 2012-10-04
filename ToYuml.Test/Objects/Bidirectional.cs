using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToYuml.Test.Objects
{
	// Bidirectional
	class Container
	{
		public Component child;
	}

	class Component
	{
		public Container parent { get; private set; }

		public Component(Container c)
		{
			this.parent = c;
		}
	}
}
