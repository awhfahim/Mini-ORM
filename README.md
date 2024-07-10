# Mini ORM - Object-Relational Mapping

Mini ORM is a lightweight Object-Relational Mapper (ORM) developed in C# to simplify data access between applications and SQL Server databases. The ORM leverages C# for core development and ADO.NET for interacting with the SQL Server Database. It utilizes Reflection to dynamically analyze object properties and map them to corresponding database table columns.

## Features

- Simplifies data access between applications and SQL Server databases.
- Uses ADO.NET for database interaction.
- Utilizes Reflection to map object properties to database columns dynamically.
- Lightweight and easy to integrate into your projects.

## Getting Started

### Prerequisites

- .NET Framework
- SQL Server

### Installation

1. Clone the repository:

   ```bash
   git clone https://github.com/awhfahim/Mini-ORM.git
   ```

2. Open the solution in Visual Studio.

3. Build the project to restore the necessary packages.

### Usage

1. Define your data model classes. Ensure that the class and its properties match the corresponding database table and columns.

   ```csharp
   public class Student
   {
       public int Id { get; set; }
       public string Name { get; set; }
       public int Age { get; set; }
   }
   ```

2. Initialize the ORM with your connection string.

   ```csharp
   string connectionString = "your_connection_string";
   MyORM orm = new MyORM(connectionString);
   ```

3. Perform CRUD operations.

   - Insert:

     ```csharp
     Student student = new Student { Name = "John Doe", Age = 20 };
     orm.Insert(student);
     ```

   - Update:

     ```csharp
     student.Name = "Jane Doe";
     orm.Update(student);
     ```

   - Delete:

     ```csharp
     orm.Delete(student);
     ```

   - Select:

     ```csharp
     List<Student> students = orm.Select<Student>();
     ```

## Code Overview

The core of the ORM is in the `MyORM.cs` file. This class handles the connection to the database and performs the necessary operations using ADO.NET.

```csharp
public class MyORM
{
    private string _connectionString;

    public MyORM(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Insert, Update, Delete, Select methods...
}
```

The ORM uses Reflection to dynamically map the properties of the object to the corresponding columns in the database table.

```csharp
var properties = typeof(T).GetProperties();
```

For more details, you can view the complete code [here](https://github.com/awhfahim/Mini-ORM/blob/main/Assignment3/Assignment3/MyORM.cs).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Contact

If you have any questions, feel free to open an issue or contact me on this mail address fahimhasan314@gmail.com.

---

Developed by Abu Wabaidar Hasan Fahim.

---

