Migration notes

Files for migration scaffold were created previously then removed to avoid build errors in the .NET Framework solution.

To fully migrate:
1. Create a separate solution or project for the .NET 7 Core app (keep separate from the .NET Framework solution).
2. Port controllers, views and models into that new project, update package references, and test.

Recommendation: Do migration in a new branch and create a separate solution file for the Core project to avoid conflicts with the existing .NET Framework solution.