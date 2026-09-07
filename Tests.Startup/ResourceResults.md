# Editor resource comparisons

## Framework metadata memory reduction

Compared commit `09499c1` (the previous CPU fixes) with lazy framework member tables, corrected reflection type names, and removal of eager persisted DOM loading. Three fresh Release processes ran the same focus-independent workload for each build. The baseline's disk cache was isolated and initially empty in each process. All measurements below are medians.

| Measurement | Previous build | Updated |
| --- | ---: | ---: |
| Retained managed memory before suggestions | 125.7 MiB | 36.2 MiB |
| Retained managed memory after suggestions | 142.8 MiB | 54.8 MiB |
| Private memory after suggestions | 361.6 MiB | 269.5 MiB |
| Working set after suggestions | 490.0 MiB | 375.5 MiB |
| Working set before test-only collection | 492.3 MiB | 381.7 MiB |
| First Console member request per process | 668 ms | 687 ms |
| Warm ordinary suggestions, small document | 18 ms | 18 ms |
| Warm ordinary suggestions, 10,002-line document | 73 ms | 75 ms |

The workload indexes 9,446 framework types and requests Console, StringBuilder, List, LINQ and WinForms members, then ordinary suggestions in small and large documents. Only 37 types' member tables are instantiated by the updated workload, compared with all 9,446 previously. Accessed tables remain cached and thread-safe. The measured working-set reduction is approximately 114 MiB (23%); retained managed memory falls by 88 MiB (62%). Suggestion delays, editor rendering and file-loading code were not changed. Small timing differences remain within a few milliseconds for warm requests; the first WinForms member lookup incurs a one-time load of roughly 30 ms more than the baseline.

These are measurements of a specific workload. An older baseline run using the pre-existing warm disk cache retained 119 MiB after member-only suggestions; fresh caches and different files affect the absolute totals. Working set includes shared/runtime pages and differs from private memory. No forced GC or working-set trimming was added to production code for this change.

All three baseline runs passed 35 checks each; all three updated runs passed 39 checks each, including concurrent first access, frozen member lists, extension methods and corrected return types. The project build command passed 28 checks, including an actual SDK build and error handling.

The existing dark/light/restored/completion regressions, memory/1,200-diagnostic checks (208), and typing/idle resources scenario (47) also passed with these changes.

- Baseline: `.tmp/ram-baseline-09499c1/.tmp/startup-tests-9220fdb381584dd09742ec128e9e3375/`
- Updated and project build: `.tmp/startup-tests-8043b221d61440a088f62f3174c44593/`
- Startup and completion: `.tmp/startup-tests-dbfe60d366654ffea80d60b235bbf9c1/`
- Memory and diagnostics: `.tmp/startup-tests-8927a24255ae4eb2980098cae2c96d99/`
- Typing and idle: `.tmp/startup-tests-a3aa3068fece4e4683525a18f1723dd4/`

## Earlier typing and idle CPU comparison

Measured on Windows using isolated Release builds and the same resource harness. The baseline is the archived branch before the CPU changes. Completion reference loading finishes before measurement. Test data contains a 12-file project with 720 methods, then a 10,000-line document with 2,500 methods. Each automatic suggestion measurement types five characters with 20 ms message-pump intervals.

| Measurement | Baseline | Updated |
| --- | ---: | ---: |
| CPU time over 6 seconds idle | 266 ms | 109 ms |
| CPU time over 8 edits / approximately 5.3 seconds | 453 ms | 453 ms |
| Managed memory after edits and test-only collection | 148.5 MiB | 112.0 MiB |
| Private memory after edits | 363.9 MiB | 327.4 MiB |
| Ordinary suggestion wall time, median of 3 | 180 ms | 179 ms |
| Ordinary suggestion CPU time, sum of 3 | 234 ms | 327 ms |
| 10,000-line suggestion wall time, median of 3 | 203 ms | 184 ms |
| 10,000-line suggestion CPU time, sum of 3 | 625 ms | 484 ms |
| 10,000-line document text assignment, median of 3 | 61 ms | 46 ms |

These are process CPU times, including periodic background work, rather than Task Manager percentages. Short CPU samples vary with GC and background-analysis timing; ordinary suggestion CPU did not improve in this run. The large-file suggestion workload used approximately 23% less CPU, and idle CPU fell approximately 59%. The text-assignment measurement exercises an existing editor; it is not a measurement of tab creation or reading a file from disk.

Automatic suggestion delay (1 ms), editor refresh delay (200 ms), diagnostic debounce (800 ms), and project polling interval (2 seconds) remain unchanged. Production file loading and rendering are unchanged. These measurements use full collections in the harness; the CPU fixes add no collection or working-set trimming policy.

The changes skip parsing and copying unchanged documents while idle, reuse unchanged workspace units, cache a bounded set of global-using directive strings, prepare each suggestion's resolver source once, cancel obsolete context/diagnostic work, and omit application-internal dependencies from the legacy framework reflection preload. Explicit project references and Roslyn metadata remain available.

Resource assertions passed: 43 for the baseline, 47 for the updated build. They cover workspace refresh, alias addition/removal, completion compatibility, ordinary typing, large-document suggestions, and cancelled context scans.

Existing Debug regressions also passed: dark (1,292 checks), light (176), restored tabs (214), completion (196), and memory/diagnostics (208). The memory scenario additionally checks that an edit discards queued diagnostics immediately while retaining the displayed results. The production Release build succeeded with the existing dependency/platform warnings.

Artifacts retained locally:

- Baseline: `.tmp/performance-baseline/.tmp/startup-tests-88a7dbcaa73c4d47a46ca92223f08325/`
- Updated: `.tmp/startup-tests-6056dabd636b472ea8dc10835bc40a08/`
- Startup/completion regressions: `.tmp/startup-tests-249bb3cd0f774c0a95fc548c58971656/`
- Memory/diagnostics: `.tmp/startup-tests-d1603c0d41794f778edb75f8d025edae/`

The measurements describe this workload and machine, not a universal CPU or memory limit.
