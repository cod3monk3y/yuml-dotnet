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
        [Test]
        public void Can_Generate_Single_Class_Diagram()
        {
            var types = new List<Type> { typeof(Animal) };
            Assert.AreEqual("[Animal]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Can_Generate_Inherited_Class_Diagram()
        {
            var types = new List<Type> { typeof(Bird), typeof(Animal) };
            Assert.AreEqual("[Bird],[Animal]^-[Bird],[Animal]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Will_Not_Generate_Inherited_Class_Diagram_If_Not_In_List()
        {
            var types = new List<Type> { typeof(Bird) };
            Assert.AreEqual("[Bird]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Can_Generate_Inherited_Class_Diagram_To_Several_Layers()
        {
            var types = new List<Type> { typeof(Eagle), typeof(Bird), typeof(Animal) };
            Assert.AreEqual("[Eagle],[Bird]^-[Eagle],[Animal]^-[Bird],[Bird],[Animal]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Can_Generate_Class_With_Interfaces()
        {
            var types = new List<Type> { typeof(Eagle), typeof(IBirdOfPrey) };
            Assert.AreEqual("[<<IBirdOfPrey>>;Eagle]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Can_Generate_Class_With_Association()
        {
            var types = new List<Type> { typeof(Eagle), typeof(Claw) };
            Assert.AreEqual("[Eagle],[Eagle]->[Claw],[Claw]", new YumlGenerator(types).Yuml());
        }

        [Test]
        public void Can_Generate_Class_With_A_Many_Association()
        {
            var types = new List<Type> { typeof(Eagle), typeof(Claw), typeof(Wing) };
            var yuml = new YumlGenerator(types).Yuml();
			Assert.AreEqual("[Eagle],[Eagle]1-0..*[Wing],[Eagle]->[Claw],[Claw],[Wing]", yuml);
        }

        [Test]
        public void Does_Not_Duplicate_Base_Classes()
        {
            var types = new List<Type> { typeof(Animal), typeof(Bird), typeof(Eagle), typeof(Swallow) };
            var yuml = new YumlGenerator(types).Yuml();
            Assert.AreEqual("[Animal],[Bird],[Animal]^-[Bird],[Eagle],[Bird]^-[Eagle],[Swallow],[Bird]^-[Swallow]", yuml);
        }

        [Test]
        public void Duplicate_Types_Appear_Only_Once()
        {
            var types = new List<Type> { typeof(Animal), typeof(Animal) };
            var yuml = new YumlGenerator(types).Yuml();
            Assert.AreEqual("[Animal]", yuml);
        }

		[Test]
		public void Interface_Inheritance_FALSE()
		{
			var types = new List<Type> { typeof(Swallow), typeof(IAnimalPrey) };

			// first way is inline
			var yuml = new YumlGenerator(types).UseInterfaceInheritance(false).Yuml();
			Assert.AreEqual("[<<IAnimalPrey>>;Swallow]", yuml);
		}

		[Test]
		public void Interface_Inheritance_TRUE()
		{
			// second way is explicit
			var types = new List<Type> { typeof(IAnimalPrey), typeof(Swallow) };
			var yuml = new YumlGenerator(types).UseInterfaceInheritance(true).Yuml();
			Assert.AreEqual("[<<IAnimalPrey>>],[Swallow],[<<IAnimalPrey>>]^-.-[Swallow]", yuml);
		}

		[Test]
		public void Base_Class_Dependencies_Do_Not_Show_In_Derived_Class()
		{
			var types = new List<Type> { typeof(Mass), typeof(Rock), typeof(Igneous) };
			var yuml = new YumlGenerator(types).Yuml();
			// This is odd, because Rock has a property Mass, a field Mass, and a List<Mass>. The process
 			// outputs the enumerable first, followed by the single field (since we're not counting actual 
			// references here yet)
			Assert.AreEqual("[Mass],[Rock],[Rock]1-0..*[Mass],[Rock]->[Mass],[Igneous],[Rock]^-[Igneous]", yuml);
		}

		[Test]
		public void Public_And_Private_Fields()
		{
			var types = new List<Type> { typeof(Lock), typeof(Key), typeof(Secret) };
			var yuml = new YumlGenerator(types).Yuml();
			Assert.AreEqual("[Lock],[Lock]->[Key],[Key],[Secret]", yuml);

			yuml = new YumlGenerator(types).SearchNonPublicMembers(true).Yuml();
			Assert.AreEqual("[Lock],[Lock]->[Key],[Lock]->[Secret],[Key],[Secret]", yuml);
		}

		[Test]
		public void Field_Is_Interface()
		{
			var types = new List<Type> { typeof(Key), typeof(IShiny) };
			var yuml = new YumlGenerator(types).Yuml();
			Assert.AreEqual("[Key],[Key]->[<<IShiny>>]", yuml);
		}

        //1-0..*
        /*
        [Test]
        public void Random()
        {
            var types = new List<Type>();
            types.AddRange(new AssemblyFilter(typeof(Folder).Assembly).Types);
            var yuml = new YumlGenerator(new AssemblyFilter(typeof(NHibernate.TransientObjectException).Assembly).Types.Where(t => t.Namespace.Contains("Cache")).ToList()).Yuml();
            Console.WriteLine(yuml);
        }
         * */


    }
}
