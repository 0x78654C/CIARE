# Typing and idle resource comparison

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

Automatic suggestion delay (1 ms), editor refresh delay (200 ms), diagnostic debounce (800 ms), and project polling interval (2 seconds) remain unchanged. Production file loading and rendering are unchanged. Full GC calls are confined to the measurement harness.

The changes skip parsing and copying unchanged documents while idle, reuse unchanged workspace units, cache a bounded set of global-using directive strings, prepare each suggestion's resolver source once, cancel obsolete context/diagnostic work, and omit application-internal dependencies from the legacy framework reflection preload. Explicit project references and Roslyn metadata remain available.

Resource assertions passed: 43 for the baseline, 47 for the updated build. They cover workspace refresh, alias addition/removal, completion compatibility, ordinary typing, large-document suggestions, and cancelled context scans.

Existing Debug regressions also passed: dark (1,292 checks), light (176), restored tabs (214), completion (196), and memory/diagnostics (208). The memory scenario additionally checks that an edit discards queued diagnostics immediately while retaining the displayed results. The production Release build succeeded with the existing dependency/platform warnings.

Artifacts retained locally:

- Baseline: `.tmp/performance-baseline/.tmp/startup-tests-88a7dbcaa73c4d47a46ca92223f08325/`
- Updated: `.tmp/startup-tests-6056dabd636b472ea8dc10835bc40a08/`
- Startup/completion regressions: `.tmp/startup-tests-249bb3cd0f774c0a95fc548c58971656/`
- Memory/diagnostics: `.tmp/startup-tests-d1603c0d41794f778edb75f8d025edae/`

The measurements describe this workload and machine, not a universal CPU or memory limit.
