using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToYuml;

namespace YumlApp
{
	class Program
	{
		static void Main(string[] args)
		{
			YumlRequest.Request("[A]->[B]", true, "a_b.png");
			YumlRequest.Request("[A]->[B],[A]^-[C]", false, "abc.png");
		}
	}
}
