using System;

namespace SampleInheritance
{
    // Base class
    class Animal
    {
        public string Name { get; set; }

        public Animal(string name)
        {
            Name = name;
        }

        public virtual void Speak()
        {
            Console.WriteLine($"{Name} makes a sound.");
        }
    }

    // Derived class: Dog
    class Dog : Animal
    {
        public Dog(string name) : base(name)
        {
        }

        public override void Speak()
        {
            Console.WriteLine($"{Name} barks.");
        }

        public void Fetch()
        {
            Console.WriteLine($"{Name} is fetching the ball!");
        }
    }

    // Derived class: Cat
    class Cat : Animal
    {
        public string lentak = "sojfosjf";
        public int ole = 2;
        public Cat(string name) : base(name)
        {
        }

        public override void Speak()
        {
            Console.WriteLine($"{Name} meows.");
        }

        public void Scratch()
        {
            Console.WriteLine($"{Name} is scratching!");
        }
    }

    // Another class for demonstrating method calls and loops
    class Zoo
    {
        public Animal[] animals;

        public Zoo(Animal[] animals)
        {
            this.animals = animals;
        }

        public void MakeAllAnimalsSpeak()
        {
            for (int c = 0; c < animals.Length; c++)
            {
                int k = 3;
                while (k > 0)
                {
                    animals[c].Speak();
                    k--;
                }                
            }
        }

        public void PerformAnimalActions()
        {
            for (int i = 0; i < animals.Length; i++)
            {
                if (animals[i] is Dog dog)
                {
                    dog.Fetch();
                }
                else if (animals[i] is Cat cat)
                {
                    cat.Scratch();
                }
                else
                {
                    Console.WriteLine($"{animals[i].Name} has no special action.");
                }
            }                        
        }
    }


    // Main class to run the example
    class Program
    {
        static void Main(string[] args)
        {
            
            // Create some animals
            Animal dog1 = new Dog("Rex");
            Animal cat1 = new Cat("Whiskers");
            Animal genericAnimal = new Animal("Moo");

            // Add animals to the zoo
            Animal[] animals = { dog1, cat1, genericAnimal };
            Zoo myZoo = new Zoo(animals);

            // Make all animals speak
            myZoo.MakeAllAnimalsSpeak();

            // Perform actions specific to animals
            myZoo.PerformAnimalActions();
        }
    }
}
