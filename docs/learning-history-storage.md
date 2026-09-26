# Learning-history storage contract

Tracking: [Issue #55](https://github.com/tsuyoshi-otake/kids-traning/issues/55).

`JsonLearningHistoryStore` owns validation and publication of the host-side learning-history
snapshot. The WebView training adapter supplies snapshots; the tray adapter reads them for
the existing PIN-protected parent export. This change does not alter that authentication flow,
the generated question content, learner progression, or the schema version.

## Accepted data

- A JSON object with integer `schemaVersion: 1`; existing minimal snapshots remain valid.
- At most 500 entries when `history` is present; it must be an array.
- At most 4 MiB of UTF-8 payload bytes, including the exact boundary.
- No `parentPin`, `parentPassword`, or `password` property anywhere in objects or arrays.
  Property-name matching ignores case and handles JSON escapes. Ordinary lesson text may
  contain these words; its contents are not scanned or rewritten.
- No duplicate property name within an object, including names equivalent after JSON escape
  decoding. The same name in separate objects is valid. Duplicate detection is case-sensitive,
  matching JSON property lookup semantics.

Invalid writes throw `InvalidDataException` before touching the committed file. Reads of
missing, invalid, oversized, or inaccessible data return a schema-v1 empty snapshot without
modifying the file. A read checks the opened file's length before allocating its contents and
does not permit concurrent in-place modification while that handle is open.

## Publication and failure ownership

Each write stages a unique file beside the destination, closes it, and replaces the destination.
An exception is observable by the caller; a failed replacement preserves the previous snapshot.
The writer deletes its own staging file on failure. If deletion also fails, it logs that cleanup
failure without replacing the original exception.

Store instances in the same process share exclusion for the normalized, case-insensitive path.
Sixteen fixed lock buckets bound synchronization memory; unrelated paths may share a bucket.
Reads, writes, and clears participate in the same exclusion. Concurrent writes publish whole
snapshots with last-completed-write semantics, without merging records. Readers allow deletion
sharing so an opened snapshot can finish reading when replaced.

This is not a power-loss durability or cross-process transaction guarantee. An abrupt process
termination can leave a staging file. Independent processes, hard links, or filesystem aliases
do not participate in the in-process path lock. The app's existing single-instance lifecycle
remains the process boundary.

## Executable acceptance criteria

Run all six storage checks together with the architecture and generated-runtime regressions:

```powershell
dotnet run --project tests\KidsTraining.ArchitectureTests\KidsTraining.ArchitectureTests.csproj -c Release -- .
```

The checks live in `tests/KidsTraining.ArchitectureTests/LearningHistoryStoreTests.cs`.

| Criterion | Verify | Expect |
| --- | --- | --- |
| Compatibility and clear | `RoundTripAndClear` | Fresh instances recover exact text; clear is idempotent; ordinary lesson text is preserved. |
| Credential and ambiguity rejection | `RejectUnsafeSnapshots` | Root/nested/array credentials, escaped names, duplicate keys, malformed input, and invalid versions are rejected without replacing prior data. |
| Data bounds | `EnforceLimits` | 500 records and exactly 4 MiB UTF-8 succeed; 501 records and one excess byte fail. |
| Read failure recovery | `ReadInvalidDiskData` | Invalid and locked files yield empty snapshots; reads preserve disk data and recover after unlocking; oversized input does not allocate its full contents. |
| Write failure recovery | `WriteFailurePreservesSnapshot` | A locked destination remains intact; failure leaves no staging file; the next write succeeds after unlocking. |
| Concurrent access | `ConcurrentStores` | 32 publications with four workers and a ten-second cancellation bound leave only complete snapshots and no staging files. |

Release build validation:

```powershell
dotnet build src\KidsTraining.App\KidsTraining.App.csproj -c Release --no-restore -warnaserror
git diff --check
```

Run the build and architecture harness sequentially because they share app build outputs.
