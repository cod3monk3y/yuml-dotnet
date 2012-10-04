using System.Collections.Generic;

namespace ToYuml.Test.Objects
{
    public class Bird : Animal
    {
        public string WingSpan { get; set; }
    }

    public class Eagle : Bird, IBirdOfPray, IAnimalPrey
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

    public interface IBirdOfPray
    {
    }

    public interface IAnimalPrey
    {
    }
}
