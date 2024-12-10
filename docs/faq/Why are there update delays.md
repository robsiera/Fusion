# Why are there update delays?

## Understanding Update Delays

By default, Fusion introduces small delays (typically 200ms to 1 second) between receiving an invalidation and updating the data. This might seem counterintuitive for a real-time system, but there are good reasons for this:

- **Preventing Server Stress**: When content changes frequently, you don't always need to see every single update immediately
- **Optimization**: For viewers who don't need real-time updates, seeing changes once per second might be sufficient
- **Coalescing Updates**: Multiple rapid changes can be combined into a single update

## The UI Commander's Role

The UI Commander helps manage these delays intelligently:

1. **Instant Updates for User Actions**: When a user performs an action, the UI Commander temporarily sets update delays to zero
   - You see your own changes immediately
   - Other users' changes still use the normal delay

2. **Error Handling**: The UI Commander also:
   - Tracks failed actions
   - Captures errors
   - Makes them available for UI display (like showing error messages)

## Customizing Update Delays

You can control this behavior in several ways:

1. **Set All Delays to Zero**: If you need immediate updates for everything
   ```csharp
   // In your client startup code
   services.Configure<FusionOptions>(options => {
       options.UpdateDelayMilliseconds = 0;
   });
   ```

2. **Ignore UI Commander**: If you set all delays to zero, you can skip using UI Commander entirely

3. **Default Delays**: Most examples use delays between 200ms to 1000ms
   ```csharp
   // Typical default setting
   options.UpdateDelayMilliseconds = 200; // 200ms delay
   ```

## When to Use What?

- **Zero Delays**: Use for critical real-time data where every update matters
- **Short Delays (200ms)**: Good for interactive data that changes frequently
- **Longer Delays (1000ms)**: Suitable for background information or less critical updates
- **UI Commander**: Keep when you want smart handling of user actions versus external changes 