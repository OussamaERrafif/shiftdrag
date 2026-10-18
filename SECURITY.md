# Security

## Reporting a vulnerability

Please use [GitHub's private vulnerability reporting](https://github.com/OussamaERrafif/shiftdrag/security/advisories/new) for issues involving unsafe file extraction, unintended file modification, argument handling, or exposure of local data. The repository enables private reports.

Include the affected commit, Windows version, engine versions, reproduction steps, expected impact, and a minimal synthetic sample when possible. Do not put exploit details, private documents, or credentials into a public issue. There is no guaranteed response time.

## Scope and dependencies

ShiftDrag is preview software; fixes target the current default branch. External FFmpeg, LibreOffice, Python, and PDF libraries have their own upstream security policies. Report dependency-specific vulnerabilities upstream as appropriate, and include the exact installed versions in reports affecting ShiftDrag.

ZIP extraction checks paths and expansion limits, and conversions stage outputs before publication. These checks do not replace upstream engine updates. Source builds and assembled packages should retain relevant dependency licenses and notices.
