using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToYuml.Test.Objects
{
	// simplest bidirectional interface
	// [A]<->[B]
	class A
	{
		public B b;
	}
	class B
	{
		public A a;
	}
}
