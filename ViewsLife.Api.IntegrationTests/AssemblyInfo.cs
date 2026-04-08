using Xunit;

// Disables test parallelization for the integration test assembly.
//
// Context:
// - These tests share a custom web host and a shared SQLite in-memory connection.
// - CI can expose race conditions or host-lifecycle timing issues more easily.
// - Disabling parallelization makes auth/cookie integration tests deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]