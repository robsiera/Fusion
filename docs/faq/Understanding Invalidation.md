# Understanding Invalidation

## Two Ways to Handle Invalidation

Fusion provides two main approaches to handle invalidation:

1. **Simple Invalidation** using `Invalidation.Begin()`:
   ```csharp
   using (Invalidation.Begin()) 
   {
       // Your invalidation logic here
   }
   ```
   - Basic scenario for single-server setups
   - Built into Fusion's core
   - Runs only on the current server

2. **Multi-Host Invalidation** using `Invalidation.IsActive`:
   ```csharp
   if (Invalidation.IsActive)
   {
       // Runs on every server in the cluster
       _ = GetUser();        // Invalidate user data
       _ = GetUserSessions(); // Invalidate sessions
   }
   ```
   - Used in distributed scenarios
   - Typically used with Entity Framework and Operation Framework
   - Runs on all servers in the cluster

## Why Two Approaches?

- **Simple Invalidation** is Fusion's native approach
  - Lightweight
  - Perfect for simple scenarios
  - You control where it runs

- **Multi-Host Invalidation** builds on top of the simple approach
  - Handles complex distributed scenarios
  - Integrates with Entity Framework
  - Automatically runs on all servers

## When to Use What?

Use **Simple Invalidation** when:
- Running on a single server
- Not using Entity Framework
- You have your own way to propagate changes

Use **Multi-Host Invalidation** when:
- Running on multiple servers
- Using Entity Framework
- Need automatic propagation of changes

## Important Notes

1. The seemingly unused method calls in `IsActive` blocks are intentional:
   ```csharp
   _ = GetUser(); // This actually registers invalidation
   ```
   - They register what needs to be invalidated
   - The underscore (_) is used because we only care about registration

2. You can create your own invalidation mechanism:
   - As long as it runs on all necessary servers
   - Must properly invalidate dependent computations
   - The Operation Framework is just one implementation 