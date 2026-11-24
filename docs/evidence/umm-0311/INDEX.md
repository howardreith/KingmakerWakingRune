# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `5b91fa18fff8f7a23bd2698807b6a304e43b1408`, exported 2026-11-24 03:02 UTC.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root (Git Bash or any POSIX shell with sha256sum; the second
command, with Python, works in Windows PowerShell and cmd.exe):

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `build-receipt.json` | `1a77fff9a7e763fef4ab7bc2ea5db9698f07e25148de82a3dc93fce8e95e037d` | `0c34ad8785948da1886c23b390ff79716f7fa9d99326f2d75ed9e2c49d75d5ed` |
| `package-validation.json` | `a3be79eb59e68db9e9ddecb4998b8dc397c2bf098856615bcce3850b90c6aac7` | `561a588263d195b751c01706a9817fc915dcdcb34653c9ceff85ad08fa68267d` |
| `direct-build-record.json` | `77c088b86d0f97021330759149d378880dbc316b4949ae0f23722c99f8d387dd` | `3a57874a00c471122659f524ea823d86cad5ace28d90e6626236bb6d2327b8d6` |
| `loader-baseline.json` | `b99c388fc53ad60f55ac0b5d1ea757588c1fefa882e11b6dc0a157c03d48489d` | `613468b448eea08e48a443027360431a1f05b45f1b5f5035a9beb316d55c507e` |
| `api-audit.json` | `1c85d73828cdc990ac19aac041f0e28ff2040678f8242f0bc0f85346f9242283` | `aad38b1472d791d61be73fdc726db8c9c10c35e5135facaa36685de3fab80760` |
| `loader-profile-inventory.json` | `8dba8979955ba6af6724a3a3db7fde068a3fed4b34fcbf2aba1ec775d82ddd57` | `9e7cb585829ac7ddef424af1e745cd358953166f00950e0fa7250af13c877c46` |
| `docs-link-check.json` | `33c6d5549b2382f54fdbb197ad2859315e9e1f44aa445d935749cd9a129855d7` | `4ea04cf7c5b89e9c7088afc6092e9affeda2817a97341fec4ed9f7374a3ea125` |
| `readme-audit-run.json` | `22aa08bdbf2545810609e653c12775c557a3f5832c13ff907d888ad743366404` | `b2fd4dd2d10bdcc0efe550bcdc9aa6244e75042d0563f630416a45e8969705da` |
| `pre-enrollment-check-project.json` | `5ae3ce0af8f9fe86cdcfd4d3929a5f7556575d1efbe0fa09273b2520b8b8c687` | `3ebd153477e35fbdbfb6bcb76a51f9af726612b443c16f30cfa05a4f2b8ed9c4` |
| `onboarding-check-project.json` | `62de07c8b77eb26a1d127ac0e2b864237747fb8580eb8afedbe0ccb4277b119f` | `41173a29b581cb70452e4c1ddee0f684b3d0bc022eccb72ed297e5913de3edb3` |
| `onboarding-preflight.json` | `866b925516364db6a5ca660090f7250b8c34a337b1c9d5e1a98fc8cb3d4a7d25` | `48b7b83c7e2321bd53e392898e5e8cb40f360b943d04247929fbb45824c4b9e4` |
| `onboarding-profile-summary.json` | `bd865902fc3b4229cd2a7fb66530d0a485cd8837e96001b761cb586b42e018e0` | `805c79fe888e0c79367200bc5982cef230023a81228a1e15590a53639424c345` |
| `onboarding-push-policy.json` | `5a1dd67c5b6e67de9d4b5eeac06b864b21671a6cd9dd8b6f658c117090963b88` | `ee816756fedb193f0fde385471ed57b234912f6973859b437218be78bdb5adc3` |
| `pr-12-description.json` | `42fca77afb907e719bdef6d512736fb4dfe4a283c45340fbbe3a4ab588fa94bc` | `e29bd0d7d7dc1fea4ad058e56fe6a793017e33d2bc1a65c0c55ca831e5a988a9` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
