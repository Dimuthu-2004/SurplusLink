// Each integration fixture creates and migrates a PostgreSQL database. Keep
// fixture-level contention bounded; explicit concurrency tests still run in parallel.
[assembly: Xunit.CollectionBehavior(MaxParallelThreads = 2)]
