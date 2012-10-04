using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToYuml;
using ToYuml.Test.Objects;

namespace YumlApp
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Requesting ab...");
			YumlRequest.Request("[A]->[B]", true, "a_b.png");

			Console.WriteLine("Requesting abc...");
			YumlRequest.Request("[A]->[B],[A]^-[C]", false, "abc.png");

			// Oroborous
			Console.WriteLine("Requesting ToYuml...");
			var gen = new YumlGenerator()
				.AddTypesForAssembly(typeof(YumlRequest))
				.SearchNonPublicMembers(true)
				.UseInterfaceInheritance(true);

			string yuml = gen.Yuml();
			YumlRequest.Request(yuml, true, "ToYuml.png");

			Console.WriteLine("Requesting ToYuml.Test...");
			gen = new YumlGenerator()
				.AddTypesForAssembly(typeof(Animal))
				.SearchNonPublicMembers(true)
				.UseInterfaceInheritance(true);
			yuml = gen.Yuml();
			YumlRequest.Request(yuml, true, "ToYuml.Test.png");

			Console.WriteLine("Done!");
		}
	}
}
