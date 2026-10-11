# Calculator for Linux

A **native Linux port of [Windows Calculator](https://github.com/microsoft/calculator)**.

This is a fork of `microsoft/calculator`. The default branch of this fork is
`linux-port`, which contains the port; `main` is left exactly as Microsoft
published it.

| | |
|---|---|
| UI | [Avalonia](https://avaloniaui.net) (.NET 10) |
| Engine | Microsoft's own C++ `CalcManager` + `Ratpack`, **compiled unmodified** |
| Packaging | Debian `.deb` — no Wine, no Windows SDK, no Visual Studio |

## Why the numbers are exact

The arithmetic is not reimplemented. `src/CalcManager` — including the `Ratpack`
arbitrary-precision library — is compiled as-is via CMake behind a thin frozen C
ABI, and the managed side reaches it through P/Invoke. So the port inherits the
reference engine's exactness rather than IEEE floating-point residue:

| Input | Result |
|---|---|
| `0.1 + 0.2` | `0.3` |
| `1/3 * 3` | `1` |
| `√(2.25) − 1.5` | `0` |
| `170!` | `7.2574156153079989673967282111293e+306` — beyond `double` range |
| `e` | 30 significant digits |

## Build and install

```bash
./linux/packaging/build-deb.sh 1.0.0
sudo apt install ./linux/publish/calculator-linux_1.0.0_amd64.deb
```

Full instructions: [`linux/README.md`](linux/README.md).

## Modes

Standard · Scientific · Programmer · Date calculation · Currency · thirteen unit
converters · Graphing.

Keyboard shortcuts bind to the **typed character**, not to a physical key
position, so operators work on any layout — QWERTZ, AZERTY, non-Latin — matching
upstream's 71 declared button shortcuts.

## Substitutions

Every proprietary component had to be replaced with something open-source. Each
one, the component it replaced, and why are listed in
[`linux/docs/PORTING-NOTES.md`](linux/docs/PORTING-NOTES.md).

## Audit

This port was audited against the unmodified reference and the findings are
published rather than quietly fixed:

| Path | What |
|---|---|
| [`AUDIT/REPORT.md`](AUDIT/REPORT.md) | verdict, defect list, evidence, what is still open |
| [`AUDIT/DEFECTS.md`](AUDIT/DEFECTS.md) | each defect: repro, root cause, fix, regression test |
| [`AUDIT/PROGRESS.md`](AUDIT/PROGRESS.md) | phase-by-phase state |

Highlights: **8 defects found and fixed**, including a High-severity CVE that was
shipping inside the `.deb` and a mode whose calculations silently never appeared
in History. **367 assertions transcribed verbatim from Microsoft's own test
suites** replay against this port — engine vectors, view-model sequences,
navigation facts, history, date arithmetic and copy/paste.

## Verification

```bash
./AUDIT/run_all.sh
```

A ten-step gate: engine build and `ctest`, managed build, the full test suite,
locale tests in their own process, `.deb` packaging, converter/resource byte
parity against the reference, a transitive licence **and CVE** audit, a live
on-screen drive through every mode, and display-scaling plus live-theme checks.
It exits non-zero on any failure.

Current: **549 + 18 managed tests and the native `ctest`, all green.**

## Not finished

Open items are listed honestly in
[`AUDIT/PROGRESS.md`](AUDIT/PROGRESS.md). The substantive ones: a soak/RSS run,
enumerating the AT-SPI accessibility tree, and a decision on the Scientific
keypad, which scrolls here where upstream uses a shift-toggle.

---

Upstream is **Microsoft Corporation**; this port is not affiliated with or
endorsed by Microsoft. Upstream's `main` branch, `LICENSE`, `SECURITY.md` and
code of conduct are unmodified and remain governed by the
[MIT licence](LICENSE).