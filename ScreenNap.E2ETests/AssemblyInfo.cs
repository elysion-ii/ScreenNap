using Xunit;

// ScreenNap runs as a single instance, so its end-to-end tests cannot run side by side
[assembly: CollectionBehavior(DisableTestParallelization = true)]
