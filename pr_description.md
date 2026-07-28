🎯 **What:** Created a new test file `Core.Tests/ArpResolverTests.cs` to test edge cases in `ArpResolver` class.
📊 **Coverage:** Covered edge cases such as invalid IPs for `ResolveMacAddress` and `TryResolveNetBiosName` along with the standard fallback mechanism (`GetFullArpTable`). Used a blackhole IP for proper timeout validation.
✨ **Result:** Improved overall project test coverage with robust and deterministic tests that do not rely on local routing or flakiness.
