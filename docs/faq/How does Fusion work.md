# How does Fusion work?

## Table of Contents

- [1. The Big Picture](#1-the-big-picture)
- [2. The Moving Parts](#2-the-moving-parts)
  - [A. Basic Components](#a-basic-components-for-just-caching-on-one-server)
  - [B. Client-side components](#b-client-side-components-adding-real-time-updates-to-browsers)
  - [C. Components for distributed scenarios](#c-components-for-distributed-scenarios-working-across-multiple-servers)

## 1. The Big Picture

We said Fusion has two core capabilities:

1. Keeping Data Fresh
2. Making Apps Fast

How does it achieve this?
Fusion works by combining two key concepts:

- Smart caching: It remembers the results of computations
- Dependency tracking: It knows which data depends on other data

Think of it like a spreadsheet:

- When you change/mutate a cell, all dependent cells become invalid (grayed out)
- When you open/look at/get an invalid cell, it recalculates using the latest values
- Only cells you actually open/look at/get get recalculated
- In Excel, you create these relationships by writing formulas

Similarly, with Fusion:

- When data changes, only the affected parts update
- Fusion detects the relationships automaticly during computation, and builds a dependency graph
- Just after data has been changed and written to a store (db, in memory list,..), you call `Invalidate()`
- This invalidates all dependent computations
- Values are only recomputed when they're actually needed

## 2. The Moving Parts

There's a saying that there are only two hard problems in computer science: cache invalidation and naming things. Fusion tries to solve the easier one of the two. But we need to do that perfectly as there's nothing worse than a half-working caching system. That is why the devil is in the details, meaning it is sometimes not just a simple as calling `Invalidate()`.

We can make a distinction between different degrees of complexity:

- [Server-side only](#a-basic-components-for-just-caching-on-one-server): Simplest scenario, just caching on one server
- [Client & server](#b-client-side-components-adding-real-time-updates-to-browsers): Adding real-time updates to browsers
- [Distributed](#c-components-for-distributed-scenarios-working-across-multiple-servers): Working across multiple servers

Fusion supports all of these scenarios, but the simpler the scenario, the less components you need to understand.

### A. Basic Components, for just caching on one server

These components form the core of Fusion. They are essential for every scenario, whether you're building a simple single-server application or a complex distributed solution. If you want to understand Fusion, start here:

- **Compute Services** - The core concept where you define what needs to be cached:
  - Define computed values using C# methods
  - Results are automatically cached
  - Support for async computations
  - Built-in error handling and retry logic
- **Invalidation System** - Control cache invalidation using `using (Invalidation.Begin()) { ... }` blocks:
  - Explicit invalidation control
  - Transactional invalidation support
  - Batching of multiple invalidations
- **Dependency Tracking** - Fusion's system for tracking what depends on what:
  - Automatic dependency detection
  - Fine-grained invalidation
  - Minimal recomputation on changes

### B. Client-side components, adding real-time updates to browsers

(Blazor only atm. Typescript implementation should be feasible)

To enable real-time updates, Fusion extends its core functionality with:

- **ActualLab.Rpc** - Fusion's optimized client-server communication:
  - WebSocket-based RPC protocol
  - 1.5x faster than SignalR
  - Uses MemoryPack or MessagePack for efficient serialization
  - Eliminates chattiness through smart batching
- **Blazor Integration** - Uses `ComputedStateComponent<T>` for reactive UI updates:
  - Client-side caching of server responses
  - Automatic updates when server state changes
  - Smart bundling of multiple server calls
- **Session Management** - Handles user sessions across the application:
  - User session tracking across the application
  - Consistent session state between client and server
  - Integration with authentication system
- **Authentication (IAuth)** - Optional authentication integration:
  - Real-time auth state synchronization
  - Works in both Server and WebAssembly modes

### C. Components for distributed scenarios, working across multiple servers

For multi-server deployments, Fusion adds:

- **Operation Framework (OF)** - Fusion's way of handling data modifications:
  - Transactional operations across servers
  - Command execution and routing via Commander
  - Operation logging and replay
  - Automatic conflict resolution
  - Cluster-wide operation coordination
  - Easy implementation for any database supported by Entity Framework
- **RpcCallRouter** - Manages server-to-server communication:
  - Smart routing of RPC calls
  - Distributed computation coordination
  - Load balancing and failover
  - Cluster-wide state consistency
