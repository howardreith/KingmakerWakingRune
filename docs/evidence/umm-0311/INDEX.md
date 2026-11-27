# UMM 0.31.1 baseline evidence

Machine-generated outputs; the round-11 rows were added for code commit `859674ea58cd9e1982050cca32a080ff6fb55ee6` (first focused run on `c4340df82992959d7d8c9bf2b8be1aa19b53823c`), the round-10 rows for code commit `6869e55d13dd7373a981fb127500964fd4f341c1`, the round-9 rows for code commit `eec7a901d1223598b5e6b147faa965dbb5a1f63f` (the other rows were exported 2026-11-24 16:43 UTC for candidate commit `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd`). Each row names what produced that output (its
source commit, or the reused input it describes); outputs produced at another commit are identified there.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<system-drive>` (the Windows system volume root), `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root (Git Bash or any POSIX shell with sha256sum; the second
command, with Python, works in Windows PowerShell and cmd.exe):

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Produced by | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- | --- |
| `focused-r11.json` | 859674e focused driver (13 of 13): R10-F1 linked ZIP whose target receipt moves to the report destination before and after the read, independent-copy report control; R9-F2 reader through a junction redirected after external classification holding its handle through the publisher's failed receipt placement and rollback; R9-F4 tracked Rules=Base.rsp response file and the per-kind decoding boundary; R10-F2 receipt recording CaseRoot.cs as external; R10-F3 six one-path producer variants with their Git change asserted; disposable clone of 859674e | `3cfb0563fd78f587a123bf8aeeda9b582ec29fbac1e5afd8c3f1bda0c8c15fbf` | `4a1e07afcd908b13638636b907a9a9caa558c02e48f62a931823bb7d70401516` |
| `focused-r11-first-run.json` | c4340df focused driver, first run (12 of 13, kept as a failed result): the R9-F2 reader was refused by an incidental error (the publisher had deleted the held file, whose path then resolved under the volume's $Extend folder) rather than by the identity recheck; corrected in 859674e | `c223baaafc5da4a693a0364a987adf4324f6e47d39e77fddf9a0fe53eab597d0` | `1f672178d93619df27bbf32b58aef8c1a876756067a2efa3a16b378350dc0381` |
| `focused-r11-driver.ps1.txt` | the round-11 focused driver itself, all three modes (fleet tool, not a repository script) | `bc8aec9158e04cad7911c1455ef9026551e5e4cb93171fe63e0249a5b04acff7` | `bc8aec9158e04cad7911c1455ef9026551e5e4cb93171fe63e0249a5b04acff7` |
| `retained-r11.json` | 859674e retained-pair step: the corrected production pair built once with Build.ps1 (tests included) in a retained producer clone and validated there with current provenance | `b3bcb6ed4bd228b5ad351ea92ab014bbfef894f75851addc1740c19087da803b` | `b2144674357507a8ff23f4d19584d478c3856765416458b5bb9a54125ec5c1b3` |
| `retained-build-receipt-r11.json` | Build.ps1 receipt of the retained corrected production pair (producer 859674e), read at its retained location | `d45f9cdf0d027b06c3e62ec675fa7f76da117781a761a543871553c709a695a5` | `bf63bb0dde687f4c6bfec8903dcb2bfc8cfa6210069d30df9d0ae261e207640c` |
| `focused-r10.json` | 6869e55 focused driver: the qualified production pair built once with Build.ps1 (tests included) in a retained producer clone and validated with current provenance before and after the disposable clone was removed; R9-F3, R9-F6, R5-F1 (private name), R9-F4 and R9-F5 checks in a disposable clone of 6869e55 | `51ac2a838d73496173cb4bb15e4e80ab38b8d83955149f0731532eace28877b0` | `ea0962d4ab3435cb4068d5d6a20a4baf1795f4f9b0d12e5a5cfa6d0fce523527` |
| `focused-r10-driver.ps1.txt` | the round-10 focused driver itself (fleet tool, not a repository script) | `785beaa3d67d0de4b80c29856ecc695ec0c0d62c60ef53b07440f78fdcdf4c2e` | `785beaa3d67d0de4b80c29856ecc695ec0c0d62c60ef53b07440f78fdcdf4c2e` |
| `retained-build-receipt-r10.json` | Build.ps1 receipt of the retained production pair (producer 6869e55), read at its retained location | `a79b0d2906e57712a14e5351c6f916d6e8319c5dd5969db0938c108b52a9d349` | `50c1a8c8c2c9a00f366ad2b5271b306eef7419b9c37ccfe9c5f0d1cae296c39e` |
| `focused-r9.json` | eec7a90 focused driver: one disposable clone of eec7a90, the real Build.ps1, Validate-Package.ps1 and KwrPackage.ps1 in fresh Windows PowerShell processes (R1-F1 junction, R8-F2 linked report input, R5-F1 alias receipt, R8-F1 casing; the file symbolic-link case recorded as not run with the host's refusal) | `66f254a1c49e22b67a699a364ce56ef3d059776bbde8513b2ceb91f5139ca986` | `fe0911bf3995359392c1629e119b399091de4e641f2166baf283fb3c06908973` |
| `focused-r9-driver.ps1.txt` | the round-9 focused driver itself (fleet tool, not a repository script) | `4da3a8888d1d988bd3ba16280e897166e4f56e095e2eead0ceb25b750e7469ed` | `4da3a8888d1d988bd3ba16280e897166e4f56e095e2eead0ceb25b750e7469ed` |
| `identity-probe-r9.txt` | lightweight probe of the KwrFileIdentityV5 members (uncommitted batch, identical to eec7a90): physical path through letter case, a junction and a hard link; hard-link names; another hard-link name could be removed while a read handle without delete sharing was open | `44197e719e04251a3000cf7a9b8a8b78fd20721c0a870ec933375d05d6cb895f` | `4b264572ae93d86044cd428726e950336547d78830f16231e213b6a49c7bccb4` |
| `identity-probe-r9.ps1.txt` | the identity probe script (fleet tool) | `f55ff87b9eeaedb9bc95e10c5441eb302f6538233b80e4564f0a4e904b252f39` | `f55ff87b9eeaedb9bc95e10c5441eb302f6538233b80e4564f0a4e904b252f39` |
| `pre-enrollment-check-project.json` | transcript summary of the pre-enrollment check (reused) | `5ae3ce0af8f9fe86cdcfd4d3929a5f7556575d1efbe0fa09273b2520b8b8c687` (original output, from its first export) | `3ebd153477e35fbdbfb6bcb76a51f9af726612b443c16f30cfa05a4f2b8ed9c4` |
| `onboarding-check-project.json` | controller check-project at enrollment (reused) | `62de07c8b77eb26a1d127ac0e2b864237747fb8580eb8afedbe0ccb4277b119f` (original output, from its first export) | `41173a29b581cb70452e4c1ddee0f684b3d0bc022eccb72ed297e5913de3edb3` |
| `onboarding-preflight.json` | controller preflight at enrollment (reused) | `866b925516364db6a5ca660090f7250b8c34a337b1c9d5e1a98fc8cb3d4a7d25` (original output, from its first export) | `48b7b83c7e2321bd53e392898e5e8cb40f360b943d04247929fbb45824c4b9e4` |
| `onboarding-profile-summary.json` | controller profile summary at enrollment (reused) | `bd865902fc3b4229cd2a7fb66530d0a485cd8837e96001b761cb586b42e018e0` (original output, from its first export) | `805c79fe888e0c79367200bc5982cef230023a81228a1e15590a53639424c345` |
| `onboarding-push-policy.json` | controller push policy record (reused) | `5a1dd67c5b6e67de9d4b5eeac06b864b21671a6cd9dd8b6f658c117090963b88` (original output, from its first export) | `ee816756fedb193f0fde385471ed57b234912f6973859b437218be78bdb5adc3` |
| `pr-12-description.json` | PR 12 description record (reused) | `42fca77afb907e719bdef6d512736fb4dfe4a283c45340fbbe3a4ab588fa94bc` (original output, from its first export) | `e29bd0d7d7dc1fea4ad058e56fe6a793017e33d2bc1a65c0c55ca831e5a988a9` |
| `newest-stable-selection.json` | newest-stable loader selection record (reused) | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` (original output, from its first export) | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `onboarding-check-project-20261124.json` | controller check-project 2026-11-24 (reused) | `1364b1bf6e39d313b23c8c4cb9a08303966af9cc6ca1042b767da018db640030` (original output, from its first export) | `2cca8f36d6830372c02c749b6cce5477f3f316652cd8b864f291571b055ccd92` |
| `onboarding-preflight-20261124.json` | controller preflight 2026-11-24 (reused) | `fee0f562e9d28d52bf0b865a45b0ae35271de0daf2dce41699497c860f263da4` (original output, from its first export) | `f5ca5bfc44c46d5515ee5941dea53bfd74fc0989377d7ddcebfabdb52338fbe9` |
| `effective-profile-verification-20261124.json` | profile comparison 2026-11-24 (reused) | `244ea7f66eb2d08943f882dbc74d4c119fdf3a1c3f5b8fcc99ed58f1b9531b54` (original output, from its first export) | `244ea7f66eb2d08943f882dbc74d4c119fdf3a1c3f5b8fcc99ed58f1b9531b54` |
| `build-receipt.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `0f5440b5caff7cf37e251cd96b1c5060f78df1d1c39204ff2a23b35e35a14c7b` | `ebe8685dce1545b2f6b36b031a6c8959c411839616f903836b303c8e10548a09` |
| `package-validation.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `c163a380f049521c16a764932fb37e623be1bfe9686cb8707d821bd3fecdfbb6` | `020100ec1a58f596b8a5363e17e3bb4c6e05d9a42e976fa31628bb61958fecdc` |
| `direct-build-record.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `40d8988be1c360b52f175efb00a6a37f6455aa53571ea7d6fd0f1903df40ae27` | `4d2c0c5d1e0573d5601743a68486ff48a675616c15e16a8edbdc58cf9addbc7b` |
| `loader-baseline.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `e0e44a875d0039de1a87f3924872a575dcfa39f14f061b29d030e7dd3344a6a6` | `825c53a2163c8d1bb9267076c70eb84e0bcd090d3abc9a63bd478ceb39cf3236` |
| `loader-baseline-v99.json` | temporary commit 0d07d71 (3779b0e plus shipping version 0.12.99) | `914764597028ef6ddf265604ad6a3039dca841ea7bde4857be6a16e1b54a484c` | `e05f7e17f5e893391d264c15552f8344a42cdfb14d3fc1da6bd665cf18a4a6d4` |
| `api-audit.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `1c85d73828cdc990ac19aac041f0e28ff2040678f8242f0bc0f85346f9242283` | `aad38b1472d791d61be73fdc726db8c9c10c35e5135facaa36685de3fab80760` |
| `loader-profile-inventory.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `bb5b23f38a0c41e5cf67997233ad742e7c57fc678afe6beb305609c34fa2911a` | `b8592cd5b942d40b3f89722f03019d2ee7cf4928a575ad01cc740ebdd6e7d395` |
| `docs-link-check.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `33c6d5549b2382f54fdbb197ad2859315e9e1f44aa445d935749cd9a129855d7` | `4ea04cf7c5b89e9c7088afc6092e9affeda2817a97341fec4ed9f7374a3ea125` |
| `readme-audit-run.json` | `3779b0ef0d450e52073000a99ffef7fd0ee7b6cd` | `9cc7fad896e9cfb3db84b3762fa0e03acb77f0ede4a4b9195c6a561ff5fc481b` | `f25cd99a2a8b62049ebcf8b5307433418671b9a9cdcf27b914c5ef24d3f110ff` |
| `loader-profiles.md` | fleet loader-profile record (reused) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | audit tool source (reused) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | audit tool project (reused) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |

Added 2026-11-24 after the owner amendment (controller-recorded): `onboarding-check-project-20261124.json` and
`onboarding-preflight-20261124.json` are controller outputs of that day, and `effective-profile-verification-20261124.json`
records that the profile file equals the enrollment snapshot. They verify the current configuration. They are not
pre-enrollment evidence: the pre-enrollment check survives only as the transcript summary in
`pre-enrollment-check-project.json`, which the owner accepted as a documented historical gap.

`loader-baseline-v99.json` is the same boundary suite run from a temporary commit that sets the shipping version to
0.12.99 on top of `3779b0e` (round 5, R3-F4); it is not a candidate.
