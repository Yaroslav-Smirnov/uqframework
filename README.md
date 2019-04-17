# UQframework
UQFramework .Net Standard Library which allows linq to any data store similiar to Linq2SQL and Entity Framework.
The library is developed within [LEAPDev](https://leapdev.io/)

# Getting Started
## Creating Model
Any model with string identifier can be used in UQFramework. The identifier property must be marked with [Key] or [Identifier] attribute.
```C#
    public class Entity
    {
        [Key]
        public string Identifier { get; set; }

        public string Name { get; set; }

        public string Property1 { get; set; }
    }
```
