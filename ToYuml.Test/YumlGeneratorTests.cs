using System;
using System.Collections.Generic;
using NUnit.Framework;
using ToYuml.Test.Objects;
using System.Linq;

namespace ToYuml.Test
{
	[TestFixture]
	public class YumlGeneratorTests
	{
		// proper comparison does a set compare and does not
		// care about ordering
		void Check(string expected, string actual)
		{
			char[] SEP = new char[] { ',' };

			HashSet<string> expectedSet = new HashSet<string>(expected.Split(','));
			HashSet<string> actualSet = new HashSet<string>(actual.Split(','));

			if (!expectedSet.SetEquals(actualSet)) {
				// items only in the expected
				var exp = new HashSet<string>(expectedSet);  // test
				exp.ExceptWith(actualSet);

				// items only in the actual
				var act = new HashSet<string>(actualSet);
				act.ExceptWith(expectedSet);

				var sexp = string.Join(",", exp.ToArray());
				var sact = string.Join(",", act.ToArray());

				string diff = string.Format("Difference exp:{0}, act:{1}", sexp, sact);
				Assert.True(false, diff);
			}
		}

		[Test]
		public void Can_Generate_Single_Class_Diagram()
		{
			var types = new List<Type> { typeof(Animal) };
			Check("[Animal]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Can_Generate_Inherited_Class_Diagram()
		{
			var types = new List<Type> { typeof(Bird), typeof(Animal) };
			Check("[Bird],[Animal]^-[Bird],[Animal]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Will_Not_Generate_Inherited_Class_Diagram_If_Not_In_List()
		{
			var types = new List<Type> { typeof(Bird) };
			Check("[Bird]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Can_Generate_Inherited_Class_Diagram_To_Several_Layers()
		{
			var types = new List<Type> { typeof(Eagle), typeof(Bird), typeof(Animal) };
			Check("[Eagle],[Bird]^-[Eagle],[Animal]^-[Bird],[Bird],[Animal]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Can_Generate_Class_With_Interfaces()
		{
			var types = new List<Type> { typeof(Eagle), typeof(IBirdOfPrey) };
			Check("[<<IBirdOfPrey>>;Eagle]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Can_Generate_Class_With_Association()
		{
			var types = new List<Type> { typeof(Eagle), typeof(Claw) };
			Check("[Eagle],[Eagle]->[Claw],[Claw]", new YumlGenerator(types).Yuml());
		}

		[Test]
		public void Can_Generate_Class_With_A_Many_Association()
		{
			var types = new List<Type> { typeof(Eagle), typeof(Claw), typeof(Wing) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Eagle],[Eagle]1-0..*[Wing],[Eagle]->[Claw],[Claw],[Wing]", yuml);
		}

		[Test]
		public void Does_Not_Duplicate_Base_Classes()
		{
			var types = new List<Type> { typeof(Animal), typeof(Bird), typeof(Eagle), typeof(Swallow) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Animal],[Bird],[Animal]^-[Bird],[Eagle],[Bird]^-[Eagle],[Swallow],[Bird]^-[Swallow]", yuml);
		}

		[Test]
		public void Duplicate_Types_Appear_Only_Once()
		{
			var types = new List<Type> { typeof(Animal), typeof(Animal) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Animal]", yuml);
		}

		[Test]
		public void Interface_Inheritance_FALSE()
		{
			var types = new List<Type> { typeof(Swallow), typeof(IAnimalPrey) };

			// first way is inline
			var yuml = new YumlGenerator(types).UseInterfaceInheritance(false).Yuml();
			Check("[<<IAnimalPrey>>;Swallow]", yuml);
		}

		[Test]
		public void Interface_Inheritance_TRUE()
		{
			// second way is explicit
			var types = new List<Type> { typeof(IAnimalPrey), typeof(Swallow) };
			var yuml = new YumlGenerator(types).UseInterfaceInheritance(true).Yuml();
			Check("[<<IAnimalPrey>>],[Swallow],[<<IAnimalPrey>>]^-.-[Swallow]", yuml);
		}

		[Test]
		public void Base_Class_Dependencies_Do_Not_Show_In_Derived_Class()
		{
			var types = new List<Type> { typeof(Mass), typeof(Rock), typeof(Igneous) };
			var yuml = new YumlGenerator(types).Yuml();
			// This is odd, because Rock has a property Mass, a field Mass, and a List<Mass>. The process
			// outputs the enumerable first, followed by the single field (since we're not counting actual 
			// references here yet)
			Check("[Mass],[Rock],[Rock]1-0..*[Mass],[Rock]->[Mass],[Igneous],[Rock]^-[Igneous]", yuml);
		}

		[Test]
		public void Public_And_Private_Fields()
		{
			var types = new List<Type> { typeof(Lock), typeof(Key), typeof(Secret) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Lock],[Lock]->[Key],[Key],[Secret]", yuml);

			yuml = new YumlGenerator(types).SearchNonPublicMembers(true).Yuml();
			Check("[Lock],[Lock]->[Key],[Lock]->[Secret],[Key],[Secret]", yuml);
		}

		[Test]
		public void Field_Is_Interface()
		{
			var types = new List<Type> { typeof(Key), typeof(IShiny) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Key],[Key]->[<<IShiny>>]", yuml);
		}

		[Test]
		public void Enumerable_Field_Is_Interface()
		{
			var types = new List<Type> { typeof(Key), typeof(INotch) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Key],[Key]1-0..*[<<INotch>>]", yuml);
		}

		[Test]
		public void Current_A_TO_B()
		{
			var types = new List<Type> { typeof(A), typeof(B) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[A]->[B],[B]->[A],[A],[B]", yuml);
		}

		
		[Test]
		public void Bidirectional_A_TO_B_With_Extraneous_Declaration()
		{
			// this is better than Current_A_TO_B, but [B] and [A] don't
			// need to appear here
			var types = new List<Type> { typeof(A), typeof(B) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[A]<->[B],[B],[A]", yuml);
		}

		[Test]
		public void Ideal_Bidirectional_A_TO_B()
		{
			var types = new List<Type> { typeof(A), typeof(B) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[A]<->[B]", yuml);
		}

		[Test]
		public void Bidirectional_One_To_One_Shows_Double_Ended_Arrow()
		{
			var types = new List<Type> { typeof(Container), typeof(Component) };
			var yuml = new YumlGenerator(types).Yuml();
			Check("[Container]<->[Component]", yuml);
		}
	}
}
