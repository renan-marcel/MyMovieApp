# Installation and Configuration Guide for `dotnet ef`

This guide will walk you through the necessary steps for the installation of `dotnet ef` and the creation of migrations and scripts.

## Installing `dotnet ef`

If your machine does not have the `dotnet ef` tool, it will need to be installed. To do this, run the following command:

```bash
dotnet tool install --global dotnet-ef
```

This command will install the `dotnet ef` tool globally on your system.

## Creating a Migration

With the `dotnet ef` tool installed, you can create migrations. To create an initial migration, run the following command:

```bash
dotnet ef migrations add Initial
```

## Generating the Migration Script

After creating the migration, you can generate the corresponding script. To do this, run the following command:

```bash
dotnet ef migrations script -o MyMovieApp.Infrastructure.sql
```

This command will generate an SQL file named `MyMovieApp.Infrastructure.sql` that contains the migration script.

---

Remember that the `dotnet ef` tool is necessary to work with database migrations in the Entity Framework. If you encounter problems during installation or generating migrations, make sure you have the .NET Core SDK installed on your system.

---

Good luck and happy coding! 🚀
