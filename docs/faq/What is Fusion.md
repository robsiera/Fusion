# What is Fusion?

Fusion is a .NET library that makes it easy to build real-time, reactive applications. It has two core capabilities:

## 1. Keeping Data Fresh
Fusion ensures your application always shows up-to-date data:
- Data automatically updates when something changes
- All users see the same, current information
- No need to manually refresh

## 2. Making Apps Fast
Fusion makes your application fast and efficient:
- Data is cached intelligently
- Only changed data is transferred
- Apps remain responsive even with complex data

## What can you build with it?

With these capabilities, Fusion enables you to easily create:
- Real-time dashboards
- Collaborative applications
- Offline-capable apps
- Live data views
- Complex forms with derived values

## Who is it for?

Fusion is perfect for:
- Business applications that need live data
- Teams building data-intensive applications
- Projects where data freshness is critical
- Applications requiring offline support
- Systems with multiple connected users

## What problems does it solve?

Without Fusion, developers need to:
- Write complex code to keep data in sync
- Manage caching manually
- Build their own real-time update system
- Handle offline scenarios themselves

Fusion handles all of this automatically, letting developers focus on business logic instead of infrastructure.


## When might Fusion NOT be the best choice?

- Very simple CRUD applications without real-time requirements
- Applications where caching is not important
- Systems requiring complete control over caching logic
- Projects where you cannot use .NET (Core)


## Common Questions

As you start working with Fusion, you might have questions about:

1. [How does Fusion work?](How%20does%20Fusion%20work.md)
   - Learn about the big picture
   - Understand the moving parts
   - See how components work together

2. [Understanding Invalidation](Understanding%20Invalidation.md)
   - Different invalidation approaches
   - When to use which approach
   - How invalidation works in distributed scenarios

3. [Why are there update delays?](Why%20are%20there%20update%20delays.md)
   - The purpose of update delays
   - How UI Commander helps
   - Customizing update behavior