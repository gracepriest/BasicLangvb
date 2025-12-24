using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneratedCode
{
    public class Person
    {
        private string _name;
        private int _age;

        public Person(string name, int age)
        {
            _name = name;
            _age = age;
        }

        public virtual string Greet()
        {
            return "Hello, I am " + _name;
        }

        public string GetName()
        {
            return _name;
        }

        public static Person CreateDefault()
        {
            return new Person("Unknown", 0);
        }

    }

    public class Employee : Person
    {
        private string _department;

        public Employee(string name, int age, string dept) : base(name, age)
        {
            _department = dept;
        }

        public override string Greet()
        {
            return (base.Greet() + " from ") + _department;
        }

    }

    public class OOPFeatures
    {
        public static void Main()
        {
            Person person = null;
            Employee employee = null;
            Person defaultPerson = null;

            person = new Person("John", 30);
            employee = new Employee("Jane", 25, "Engineering");
            Console.WriteLine(person.GetName());
            Console.WriteLine(employee.Greet());
            defaultPerson = Person.CreateDefault();
        }

    }
}
