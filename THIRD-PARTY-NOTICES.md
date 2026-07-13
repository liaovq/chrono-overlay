# Third-party notices

ChronoOverlay packages the following font files as WPF application resources. Each font is licensed under the SIL Open Font License 1.1. The complete license texts are embedded in every released EXE and can be viewed offline from the system tray menu **第三方许可**. The same verbatim texts remain available in the source tree at `src/ChronoOverlay/Assets/Fonts/Licenses/` for auditability.

| Font | Upstream | Pinned source blob | Included file |
| --- | --- | --- | --- |
| JetBrains Mono | https://github.com/JetBrains/JetBrainsMono | `dff66cc50702c75abd025dcf49f62a4dcc2d72de` | `JetBrainsMono-Regular.ttf` |
| IBM Plex Mono | https://github.com/IBM/plex | `4254c37f20ba57e829a3315fdc91cda4e63589f5` | `IBMPlexMono-Regular.ttf` |
| Space Mono | https://github.com/google/fonts | `1cfa3653dc8ddb7aa9f4ef1c9c581e9516137e6e` | `SpaceMono-Regular.ttf` |

No font file has been modified. System fonts such as Cascadia Mono and Consolas are referenced by name and are not redistributed.

The release build verifies SHA-256 digests for all three font files and all three OFL text files, and verifies that this notice plus every license text is present in the WPF resource manifest embedded into the single-file application.
