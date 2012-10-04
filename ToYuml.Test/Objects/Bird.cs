using System.Collections.Generic;

namespace ToYuml.Test.Objects
{
    public class Bird : Animal
    {
        public string WingSpan { get; set; }
    }

    public class Eagle : Bird, IBirdOfPrey, IAnimalPrey
    {
        public Claw Claw { get; set; }
        public IList<Wing> Wings { get; set; }
    }

    public class Swallow : Bird, IAnimalPrey
    {
    }

    public class Claw
    {
    }

    public class Wing
    {
    }

    public interface IBirdOfPrey
    {
    }

    public interface IAnimalPrey
    {
    }

	// ----
	public class Mass
	{
		public float grams;
	}

	public class Rock
	{
		public Mass mass;
		public List<Mass> othermasses;
		public Mass foo
		{
			get { return mass; }
		}
	}

	public class Igneous : Rock
	{
		// NOTE: diagram should NOT show a dependency from Igneous to Mass
	}

	// ----
	public class Lock
	{
		public Key key;
		Secret secret;
	}
	public class Key
	{
	}
	public class Secret
	{
	}
}
