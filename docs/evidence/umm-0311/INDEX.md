# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `3c9f79e28acf473c919d21ab4e46deb323ff3902`, exported 2026-11-24 02:16 UTC.
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
| `build-receipt.json` | `3801af97c7074c940cd0d279f06b4fc271c7400b396cb99a961e364d05524e72` | `5423c3c885e13e1b3298650ed1c92c3ee1334279570bed8764401b96e4cae663` |
| `package-validation.json` | `da8bebde1df19c66e86c0d082e918c047364e2dcf15cb7831ba8790528cb7f77` | `40a9c46b92e0a4527d8741d369aea101711c102ef71f87336eb0f8e957e7240d` |
| `direct-build-record.json` | `d466323a2d7dae033b9017f144c62346cdd6dacaf88734004b426255b374306a` | `b67df5aeef3f630b557032e2ccdbc3fdb883ded003a35ea4bb0b9f5a63e87e98` |
| `loader-baseline.json` | `1e0a730547010792bde3f8d6f802498f261e086bbc7374c3951f7fad6e34c7a9` | `faa199bff3292750e4850d7639b0c14ca0688d55d5dd1f6987168f7e151c5bd7` |
| `api-audit.json` | `1c85d73828cdc990ac19aac041f0e28ff2040678f8242f0bc0f85346f9242283` | `aad38b1472d791d61be73fdc726db8c9c10c35e5135facaa36685de3fab80760` |
| `loader-profile-inventory.json` | `0aa37cfcb9f3df1b11a6a1dde1cf4da69e6e65a9c0c5f67f5072ff2deace5533` | `e81a2209054055761cbbfe2150e0b1c326c652f69e8e67c33932a2ec5115ceb0` |
| `docs-link-check.json` | `33c6d5549b2382f54fdbb197ad2859315e9e1f44aa445d935749cd9a129855d7` | `4ea04cf7c5b89e9c7088afc6092e9affeda2817a97341fec4ed9f7374a3ea125` |
| `onboarding-check-project.json` | `62de07c8b77eb26a1d127ac0e2b864237747fb8580eb8afedbe0ccb4277b119f` | `41173a29b581cb70452e4c1ddee0f684b3d0bc022eccb72ed297e5913de3edb3` |
| `onboarding-preflight.json` | `866b925516364db6a5ca660090f7250b8c34a337b1c9d5e1a98fc8cb3d4a7d25` | `48b7b83c7e2321bd53e392898e5e8cb40f360b943d04247929fbb45824c4b9e4` |
| `onboarding-profile-summary.json` | `bd865902fc3b4229cd2a7fb66530d0a485cd8837e96001b761cb586b42e018e0` | `805c79fe888e0c79367200bc5982cef230023a81228a1e15590a53639424c345` |
| `onboarding-push-policy.json` | `9a29e266c4155d1a0529c17f4b6230616f39c593b8be8ef8a9abb90e53f76a01` | `f47df089b28e389e95a249dcccf2151fda2635b95ad006868098d4b2e9eb1ad0` |
| `pr-12-description.json` | `0a6baf886e4d5cb53fa0bc548213cec75287768cc225ad53c022539102e01126` | `84710647cebde6df0d1dc34430346038a82982b71e1db28b4d76758a7b0765e1` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
