# Flowsy Db Cli

This package includes utility classes to create an interactive command line interface tool to perform
database management tasks, though the only supported task for the moment is running migrations.

Under the hood, this package uses the popular tool named [Evolve](https://www.nuget.org/packages/Evolve), so you should
follow the specifications from [Evolve Concepts](https://evolve-db.netlify.app/concepts/) to create your migration files.


## Example
Let's say you create a Console Application to run certain management tasks and you include the following appsettings.json file:
```json5
{
  // ...
  "Database": { // Root for all database connections
    "Database1": { // Key to identify this connection
      "ProviderInvariantName": "Npgsql",
      "ConnectionString": "Server=pg.example.com;Port=5432;Database=db1;User Id=user1;Password=sup3rS3cr3t;Include Error Detail=True;",
      "Migration": { // Optional section to configure database migrations
        "SourceDirectory": "Some/Path/To/Migrations/Database1", // Path with migration scripts for 'Database1'
        "MetadataSchema": "public", // Schema containing the table for migration metadata
        "MetadataTable": "migration", // Table for migration metadata
        "InitializationStatement": "call public.populate_tables();" // Optional statement to execute after running migrations
      }
    },
    "Database2": { // Key to identify this connection
      "ProviderInvariantName": "MySql.Data.MySqlClient",
      "ConnectionString": "Server=mysql.example.com;Port=3306;Database=db2;User Id=user2;Password=m3gaS3cr3t;",
      "Migration": { // Optional section to configure database migrations
        "SourceDirectory": "Some/Path/To/Migrations/Database2", // Path with migration scripts for 'Database2'
        "MetadataTable": "migration", // Table for migration metadata
        "InitializationStatement": "call populate_tables();" // Optional statement to execute after running migrations
      }
    }
  },
  // ...
}
```

Given the previous configuration, you can use the **DbPrompt** class as shown in the following code snippet:
```csharp
var builder = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var configuration = builder.Build();

var prompt = new DbPrompt(configuration, "Database");
await prompt.RunAsync(CancellationToken.None);
```

That's it! DbPrompt will ask the user all the necessary information to run the database migrations.
