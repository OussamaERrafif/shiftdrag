# Contributing

Thanks for helping improve ShiftDrag. Start with the [development guide](docs/DEVELOPMENT.md), [architecture](docs/ARCHITECTURE.md), and [testing notes](docs/TESTING.md).

## Report an issue

Use a [bug report](https://github.com/OussamaERrafif/shiftdrag/issues/new?template=bug_report.yml) and include the Windows version, display scale, input type, exact steps, and expected/actual result. For media/PDF issues, include engine versions and whether the problem occurs on a small synthetic file. Use the feature request template for proposals.

Avoid sharing private documents or unreviewed logs. Security-sensitive issues follow [SECURITY.md](SECURITY.md).

## Make a change

1. Fork the repository and create a focused branch.
2. Build with `build.ps1`. Preserve compatibility with the installed Framework compiler and .NET Framework 4.8.
3. Run the relevant functional checks. A core-only run skips optional-engine groups; state which engines were available.
4. For drag or rendering changes, complete the relevant [manual checks](docs/TESTING.md#manual-checks-for-changes-to-input-or-rendering) and include app screenshots where useful.
5. Update affected guides and open a pull request describing the problem, resulting behavior, and verification.

Keep originals safe during conversions, use collision-safe staged outputs, and retain local processing. Avoid new dependencies unless the change benefits from them. Add meaningful regressions for behavior that could break; documentation-only changes do not need new application tests.

Generated builds, engines, test fixtures, local settings, credentials, and personal paths do not belong in commits. Keep dependency notices when distributing runtime components. Contributions to original project code/docs are submitted under the repository's MIT license; reference captures retain their original owners' rights.
